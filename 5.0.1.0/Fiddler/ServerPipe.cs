using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using FiddlerCore.Utilities;

namespace Fiddler
{
	/// <summary>
	/// A ServerPipe wraps a socket connection to a server.
	/// </summary>
	// Token: 0x02000055 RID: 85
	public class ServerPipe : BasePipe
	{
		/// <summary>
		/// Policy for reuse of this pipe
		/// </summary>
		// Token: 0x17000098 RID: 152
		// (get) Token: 0x0600033A RID: 826 RVA: 0x0001E8B6 File Offset: 0x0001CAB6
		// (set) Token: 0x0600033B RID: 827 RVA: 0x0001E8BE File Offset: 0x0001CABE
		public PipeReusePolicy ReusePolicy
		{
			get
			{
				return this._reusePolicy;
			}
			set
			{
				this._reusePolicy = value;
			}
		}

		/// <summary>
		/// Wraps a socket in a Pipe
		/// </summary>
		/// <param name="oSocket">The Socket</param>
		/// <param name="sName">Pipe's human-readable name</param>
		/// <param name="bConnectedToGateway">True if the Pipe is attached to a gateway</param>
		/// <param name="sPoolingKey">The Pooling key used for socket reuse</param>
		// Token: 0x0600033C RID: 828 RVA: 0x0001E8C7 File Offset: 0x0001CAC7
		internal ServerPipe(Socket oSocket, string sName, bool bConnectedToGateway, string sPoolingKey)
			: base(oSocket, sName)
		{
			this.dtConnected = DateTime.Now;
			this._bIsConnectedToGateway = bConnectedToGateway;
			this.sPoolKey = sPoolingKey;
		}

		/// <summary>
		/// Returns TRUE if there is an underlying, mutually-authenticated HTTPS stream.
		///
		/// WARNING: Results are a bit of a lie. System.NET IsMutuallyAuthenticated == true if a client certificate is AVAILABLE even
		/// if that certificate was never SENT to the server.
		/// </summary>
		// Token: 0x17000099 RID: 153
		// (get) Token: 0x0600033D RID: 829 RVA: 0x0001E8EB File Offset: 0x0001CAEB
		internal bool isClientCertAttached
		{
			get
			{
				return this._httpsStream != null && this._httpsStream.IsMutuallyAuthenticated;
			}
		}

		/// <summary>
		/// Returns TRUE if this PIPE is marked as having been authenticated using a Connection-Oriented Auth protocol:
		/// NTLM, Kerberos, or HTTPS Client Certificate.
		/// </summary>
		// Token: 0x1700009A RID: 154
		// (get) Token: 0x0600033E RID: 830 RVA: 0x0001E902 File Offset: 0x0001CB02
		internal bool isAuthenticated
		{
			get
			{
				return this._isAuthenticated;
			}
		}

		/// <summary>
		/// Marks this Pipe as having been authenticated. Depending on the preference "fiddler.network.auth.reusemode" this may impact the reuse policy for this pipe
		/// </summary>
		/// <param name="clientPID">The client's process ID, if known.</param>
		// Token: 0x0600033F RID: 831 RVA: 0x0001E90C File Offset: 0x0001CB0C
		internal void MarkAsAuthenticated(int clientPID)
		{
			this._isAuthenticated = true;
			int iMode = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.auth.reusemode", 0);
			if (iMode == 0 && clientPID == 0)
			{
				iMode = 1;
			}
			if (iMode == 0)
			{
				this.ReusePolicy = PipeReusePolicy.MarriedToClientProcess;
				this._iMarriedToPID = clientPID;
				this.sPoolKey = string.Format("pid{0}*{1}", clientPID, this.sPoolKey);
				return;
			}
			if (iMode != 1)
			{
				return;
			}
			this.ReusePolicy = PipeReusePolicy.MarriedToClientPipe;
		}

		/// <summary>
		/// Indicates if this pipe is connected to an upstream (non-SOCKS) Proxy.
		/// </summary>
		// Token: 0x1700009B RID: 155
		// (get) Token: 0x06000340 RID: 832 RVA: 0x0001E973 File Offset: 0x0001CB73
		public bool isConnectedToGateway
		{
			get
			{
				return this._bIsConnectedToGateway;
			}
		}

		/// <summary>
		/// Indicates if this pipe is connected to a SOCKS gateway
		/// </summary>
		// Token: 0x1700009C RID: 156
		// (get) Token: 0x06000341 RID: 833 RVA: 0x0001E97B File Offset: 0x0001CB7B
		// (set) Token: 0x06000342 RID: 834 RVA: 0x0001E983 File Offset: 0x0001CB83
		public bool isConnectedViaSOCKS
		{
			get
			{
				return this._bIsConnectedViaSOCKS;
			}
			set
			{
				this._bIsConnectedViaSOCKS = value;
			}
		}

		/// <summary>
		/// Sets the receiveTimeout based on whether this is a freshly opened server socket or a reused one.
		/// </summary>
		// Token: 0x06000343 RID: 835 RVA: 0x0001E98C File Offset: 0x0001CB8C
		internal void setTimeouts()
		{
			try
			{
				int iReceiveTimeout = ((this.iUseCount < 2U) ? ServerPipe._timeoutReceiveInitial : ServerPipe._timeoutReceiveReused);
				int iSendTimeout = ((this.iUseCount < 2U) ? ServerPipe._timeoutSendInitial : ServerPipe._timeoutSendReused);
				if (iReceiveTimeout > 0)
				{
					this._baseSocket.ReceiveTimeout = iReceiveTimeout;
				}
				if (iSendTimeout > 0)
				{
					this._baseSocket.SendTimeout = iSendTimeout;
				}
			}
			catch
			{
			}
		}

		/// <summary>
		/// Returns a semicolon-delimited string describing this ServerPipe
		/// </summary>
		/// <returns>A semicolon-delimited string</returns>
		// Token: 0x06000344 RID: 836 RVA: 0x0001E9FC File Offset: 0x0001CBFC
		public override string ToString()
		{
			return string.Format("{0}[Key: {1}; UseCnt: {2} [{3}]; {4}; {5} (:{6} to {7}:{8} {9}) {10}]", new object[]
			{
				this._sPipeName,
				this._sPoolKey,
				this.iUseCount,
				string.Empty,
				base.bIsSecured ? "Secure" : "PlainText",
				this._isAuthenticated ? "Authenticated" : "Anonymous",
				base.LocalPort,
				base.Address,
				base.Port,
				this.isConnectedToGateway ? "Gateway" : "Direct",
				this._reusePolicy
			});
		}

		/// <summary>
		/// Gets and sets the pooling key for this server pipe.
		/// </summary>
		/// <example>
		///   direct-&gt;{http|https}/{serverhostname}:{serverport}
		///   gw:{gatewayaddr:port}-&gt;*
		///   gw:{gatewayaddr:port}-&gt;{http|https}/{serverhostname}:{serverport}
		///   socks:{gatewayaddr:port}-&gt;{http|https}/{serverhostname}:{serverport}
		/// </example>
		// Token: 0x1700009D RID: 157
		// (get) Token: 0x06000345 RID: 837 RVA: 0x0001EABC File Offset: 0x0001CCBC
		// (set) Token: 0x06000346 RID: 838 RVA: 0x0001EAC4 File Offset: 0x0001CCC4
		public string sPoolKey
		{
			get
			{
				return this._sPoolKey;
			}
			private set
			{
				if (CONFIG.bDebugSpew && !string.IsNullOrEmpty(this._sPoolKey) && this._sPoolKey != value)
				{
					FiddlerApplication.Log.LogFormat("fiddler.pipes>{0} pooling key changing from '{1}' to '{2}'", new object[] { this._sPipeName, this._sPoolKey, value });
				}
				this._sPoolKey = value.ToLower();
			}
		}

		// Token: 0x06000347 RID: 839 RVA: 0x0001EB2C File Offset: 0x0001CD2C
		private static string SummarizeCert(X509Certificate2 oCert)
		{
			if (!string.IsNullOrEmpty(oCert.FriendlyName))
			{
				return oCert.FriendlyName;
			}
			string sSubject = oCert.Subject;
			if (string.IsNullOrEmpty(sSubject))
			{
				return string.Empty;
			}
			if (sSubject.Contains("CN="))
			{
				return Utilities.TrimAfter(Utilities.TrimBefore(sSubject, "CN="), ",");
			}
			if (sSubject.Contains("O="))
			{
				return Utilities.TrimAfter(Utilities.TrimBefore(sSubject, "O="), ",");
			}
			return sSubject;
		}

		/// <summary>
		/// Returns the Server's certificate Subject CN (used by "x-UseCertCNFromServer")
		/// </summary>
		/// <returns>The *FIRST* CN field from the Subject of the certificate used to secure this HTTPS connection, or null if the connection is unsecure</returns>
		// Token: 0x06000348 RID: 840 RVA: 0x0001EBAC File Offset: 0x0001CDAC
		internal string GetServerCertCN()
		{
			if (this._httpsStream == null)
			{
				return null;
			}
			if (this._httpsStream.RemoteCertificate == null)
			{
				return null;
			}
			string sSubject = this._httpsStream.RemoteCertificate.Subject;
			if (sSubject.Contains("CN="))
			{
				return Utilities.TrimAfter(Utilities.TrimBefore(sSubject, "CN="), ",");
			}
			return sSubject;
		}

		// Token: 0x06000349 RID: 841 RVA: 0x0001EC08 File Offset: 0x0001CE08
		internal string GetServerCertChain()
		{
			if (this._ServerCertChain != null)
			{
				return this._ServerCertChain;
			}
			if (this._httpsStream == null)
			{
				return string.Empty;
			}
			string result;
			try
			{
				X509Certificate2 oEECert = new X509Certificate2(this._httpsStream.RemoteCertificate);
				if (oEECert == null)
				{
					result = string.Empty;
				}
				else
				{
					StringBuilder oSB = new StringBuilder();
					X509Chain oChain = new X509Chain();
					oChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
					oChain.Build(oEECert);
					for (int i = oChain.ChainElements.Count - 1; i >= 1; i--)
					{
						oSB.Append(ServerPipe.SummarizeCert(oChain.ChainElements[i].Certificate));
						oSB.Append(" > ");
					}
					if (oChain.ChainElements.Count > 0)
					{
						oSB.AppendFormat("{0} [{1}]", ServerPipe.SummarizeCert(oChain.ChainElements[0].Certificate), oChain.ChainElements[0].Certificate.SerialNumber);
					}
					this._ServerCertChain = oSB.ToString();
					result = oSB.ToString();
				}
			}
			catch (Exception eX)
			{
				result = eX.Message;
			}
			return result;
		}

		// Token: 0x1700009E RID: 158
		// (get) Token: 0x0600034A RID: 842 RVA: 0x0001ED30 File Offset: 0x0001CF30
		public X509Certificate2 ServerCertificate
		{
			get
			{
				return this._certServer;
			}
		}

		/// <summary>
		/// Return a string describing the HTTPS connection security, if this socket is secured
		/// </summary>
		/// <returns>A string describing the HTTPS connection's security.</returns>
		// Token: 0x0600034B RID: 843 RVA: 0x0001ED38 File Offset: 0x0001CF38
		public string DescribeConnectionSecurity()
		{
			if (this._httpsStream != null)
			{
				string sClientCertificate = string.Empty;
				if (this._httpsStream.IsMutuallyAuthenticated)
				{
					sClientCertificate = "== Client Certificate ==========\nUnknown.\n";
				}
				if (this._httpsStream.LocalCertificate != null)
				{
					sClientCertificate = "\n== Client Certificate ==========\n" + this._httpsStream.LocalCertificate.ToString(true) + "\n";
				}
				StringBuilder oSB = new StringBuilder(2048);
				oSB.AppendFormat("Secure Protocol: {0}\n", this._httpsStream.SslProtocol.ToString());
				oSB.AppendFormat("Cipher: {0}\n", this.GetConnectionCipherInfo());
				oSB.AppendFormat("Hash Algorithm: {0}\n", this.GetConnectionHashInfo());
				oSB.AppendFormat("Key Exchange: {0}\n", this.GetConnectionKeyExchangeInfo());
				oSB.Append(sClientCertificate);
				oSB.AppendLine("\n== Server Certificate ==========");
				try
				{
					oSB.AppendLine(this._httpsStream.RemoteCertificate.ToString(true));
					X509Certificate2 cert = new X509Certificate2(this._httpsStream.RemoteCertificate);
					string subjectAltNamesHeader = "[SubjectAltNames]\n";
					string altNames = CertInfo.GetSubjectAltNames(cert);
					if (!string.IsNullOrEmpty(altNames))
					{
						oSB.AppendLine(subjectAltNamesHeader + altNames);
					}
				}
				catch
				{
				}
				if (FiddlerApplication.Prefs.GetBoolPref("fiddler.network.https.storeservercertchain", false))
				{
					oSB.AppendFormat("[Chain]\n {0}\n", this.GetServerCertChain());
				}
				return oSB.ToString();
			}
			return "No connection security";
		}

		/// <summary>
		/// Returns a string describing how this connection is secured.
		/// </summary>
		/// <returns></returns>
		// Token: 0x0600034C RID: 844 RVA: 0x0001EEA8 File Offset: 0x0001D0A8
		internal string GetConnectionCipherInfo()
		{
			return this.GetConnectionInfo(() => this._httpsStream.CipherAlgorithm.ToString(), () => this._httpsStream.CipherStrength.ToString());
		}

		// Token: 0x0600034D RID: 845 RVA: 0x0001EEC8 File Offset: 0x0001D0C8
		private string GetConnectionHashInfo()
		{
			return this.GetConnectionInfo(delegate
			{
				string sHash = this._httpsStream.HashAlgorithm.ToString();
				if (sHash == "32780")
				{
					sHash = "Sha256";
				}
				else if (sHash == "32781")
				{
					sHash = "Sha384";
				}
				return sHash;
			}, delegate
			{
				string sHashBits = this._httpsStream.HashStrength.ToString();
				if ("0" == sHashBits)
				{
					sHashBits = "?";
				}
				return sHashBits;
			});
		}

		// Token: 0x0600034E RID: 846 RVA: 0x0001EEE8 File Offset: 0x0001D0E8
		private string GetConnectionKeyExchangeInfo()
		{
			return this.GetConnectionInfo(delegate
			{
				string sKeyExchange = this._httpsStream.KeyExchangeAlgorithm.ToString();
				if (sKeyExchange == "44550")
				{
					sKeyExchange = "ECDHE_RSA (0xae06)";
				}
				return sKeyExchange;
			}, () => this._httpsStream.KeyExchangeStrength.ToString());
		}

		// Token: 0x0600034F RID: 847 RVA: 0x0001EF08 File Offset: 0x0001D108
		private string GetConnectionInfo(Func<string> getAlgorithm, Func<string> getStrength)
		{
			if (this._httpsStream == null)
			{
				return "<none>";
			}
			string algorithm;
			try
			{
				algorithm = getAlgorithm();
			}
			catch (NotImplementedException ex)
			{
				return "Your tls implementation does not provide this information";
			}
			catch (Exception ex2)
			{
				if (ex2 == null)
				{
					return "Error";
				}
				return ex2.ToString();
			}
			string strength;
			try
			{
				strength = getStrength();
			}
			catch (Exception ex3)
			{
				strength = "?";
			}
			return string.Format("{0} {1}bits", algorithm, strength);
		}

		/// <summary>
		/// Get the Transport Context for the underlying HTTPS connection so that Channel-Binding Tokens work correctly
		/// </summary>
		/// <returns></returns>
		// Token: 0x06000350 RID: 848 RVA: 0x0001EF98 File Offset: 0x0001D198
		internal TransportContext _GetTransportContext()
		{
			if (this._httpsStream != null)
			{
				return this._httpsStream.TransportContext;
			}
			return null;
		}

		/// <summary>
		/// Returns the IPEndPoint to which this socket is connected, or null
		/// </summary>
		// Token: 0x1700009F RID: 159
		// (get) Token: 0x06000351 RID: 849 RVA: 0x0001EFB0 File Offset: 0x0001D1B0
		public IPEndPoint RemoteEndPoint
		{
			get
			{
				if (this._baseSocket == null)
				{
					return null;
				}
				IPEndPoint result;
				try
				{
					result = this._baseSocket.RemoteEndPoint as IPEndPoint;
				}
				catch (Exception eX)
				{
					result = null;
				}
				return result;
			}
		}

		// Token: 0x06000352 RID: 850 RVA: 0x0001EFF4 File Offset: 0x0001D1F4
		private static bool ConfirmServerCertificate(Session oS, string sExpectedCN, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			CertificateValidity oCV = CertificateValidity.Default;
			FiddlerApplication.CheckOverrideCertificatePolicy(oS, sExpectedCN, certificate, chain, sslPolicyErrors, ref oCV);
			if (oCV == CertificateValidity.ForceInvalid)
			{
				return false;
			}
			if (oCV == CertificateValidity.ForceValid)
			{
				return true;
			}
			if ((oCV != CertificateValidity.ConfirmWithUser && (sslPolicyErrors == SslPolicyErrors.None || CONFIG.IgnoreServerCertErrors)) || oS.oFlags.ContainsKey("X-IgnoreCertErrors"))
			{
				return true;
			}
			if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch && oS.oFlags.ContainsKey("X-IgnoreCertCNMismatch"))
			{
				sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNameMismatch;
				if (sslPolicyErrors == SslPolicyErrors.None)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Get the user's default client cert for authentication; caching if if possible and permitted.
		/// </summary>
		/// <returns></returns>
		// Token: 0x06000353 RID: 851 RVA: 0x0001F068 File Offset: 0x0001D268
		private static X509Certificate _GetDefaultCertificate()
		{
			if (FiddlerApplication.oDefaultClientCertificate != null)
			{
				return FiddlerApplication.oDefaultClientCertificate;
			}
			X509Certificate oCert = null;
			if (File.Exists(CONFIG.GetPath("DefaultClientCertificate")))
			{
				oCert = X509Certificate.CreateFromCertFile(CONFIG.GetPath("DefaultClientCertificate"));
				if (oCert == null)
				{
					return null;
				}
				if (FiddlerApplication.Prefs.GetBoolPref("fiddler.network.https.cacheclientcert", true))
				{
					FiddlerApplication.oDefaultClientCertificate = oCert;
				}
			}
			return oCert;
		}

		/// <summary>
		/// This method is called by the HTTPS Connection establishment to optionally attach a client certificate to the request.
		/// Test Page: https://tower.dartmouth.edu/doip/OracleDatabases.jspx or ClientCertificate.ms in Test folder should request on initial connection
		/// In contrast, this one: https://roaming.officeapps.live.com/rs/roamingsoapservice.svc appears to try twice (renego)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="targetHost"></param>
		/// <param name="localCertificates"></param>
		/// <param name="remoteCertificate"></param>
		/// <param name="acceptableIssuers"></param>
		/// <returns></returns>
		// Token: 0x06000354 RID: 852 RVA: 0x0001F0C4 File Offset: 0x0001D2C4
		private X509Certificate AttachClientCertificate(Session oS, object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
		{
			if (CONFIG.bDebugSpew)
			{
				FiddlerApplication.Log.LogFormat("fiddler.network.https.clientcertificate>AttachClientCertificate {0} - {1}, {2} local certs, {3} acceptable issuers.", new object[]
				{
					targetHost,
					(remoteCertificate != null) ? remoteCertificate.Subject.ToString() : "NoRemoteCert",
					(localCertificates != null) ? localCertificates.Count.ToString() : "(null)",
					(acceptableIssuers != null) ? acceptableIssuers.Length.ToString() : "(null)"
				});
			}
			if (localCertificates.Count > 0)
			{
				this.MarkAsAuthenticated(oS.LocalProcessID);
				oS.oFlags["x-client-cert"] = localCertificates[0].Subject + " Serial#" + localCertificates[0].GetSerialNumberString();
				return localCertificates[0];
			}
			if (FiddlerApplication.ClientCertificateProvider != null)
			{
				X509Certificate oCert = FiddlerApplication.ClientCertificateProvider(oS, targetHost, localCertificates, remoteCertificate, acceptableIssuers);
				if (oCert == null)
				{
					return null;
				}
				if (CONFIG.bDebugSpew)
				{
					FiddlerApplication.DebugSpew("Session #{0} Attaching client certificate '{1}' when connecting to host '{2}'", new object[] { oS.id, oCert.Subject, targetHost });
				}
				this.MarkAsAuthenticated(oS.LocalProcessID);
				oS.oFlags["x-client-cert"] = oCert.Subject + " Serial#" + oCert.GetSerialNumberString();
				return oCert;
			}
			else
			{
				bool bSawHintsServerSentCertRequest = remoteCertificate != null || acceptableIssuers.Length != 0;
				X509Certificate oDefaultCert = ServerPipe._GetDefaultCertificate();
				if (oDefaultCert != null)
				{
					if (bSawHintsServerSentCertRequest)
					{
						this.MarkAsAuthenticated(oS.LocalProcessID);
					}
					oS.oFlags["x-client-cert"] = oDefaultCert.Subject + " Serial#" + oDefaultCert.GetSerialNumberString();
					return oDefaultCert;
				}
				if (bSawHintsServerSentCertRequest)
				{
					FiddlerApplication.Log.LogFormat("The server [{0}] requested a client certificate, but no client certificate was available.", new object[] { targetHost });
					if (CONFIG.bShowDefaultClientCertificateNeededPrompt && FiddlerApplication.Prefs.GetBoolPref("fiddler.network.https.clientcertificate.ephemeral.prompt-for-missing", true))
					{
						FiddlerApplication.Prefs.SetBoolPref("fiddler.network.https.clientcertificate.ephemeral.prompt-for-missing", false);
						string messageTitle = "Client Certificate Requested";
						string messageContent = "The server [" + targetHost + "] requests a client certificate.\nPlease save a client certificate using the filename:\n\n" + CONFIG.GetPath("DefaultClientCertificate");
						FiddlerApplication.Log.LogFormat("{0}: {1}", new object[] { messageTitle, messageContent });
					}
				}
				return null;
			}
		}

		/// <summary>
		/// This function secures an existing connection and authenticates as client. This is primarily useful when
		/// the socket is connected to a Gateway/Proxy and we had to send a CONNECT and get a HTTP/200 Connected back before
		/// we actually secure the socket.
		///  http://msdn.microsoft.com/en-us/library/system.net.security.sslstream.aspx
		/// </summary>
		/// <param name="oS">The Session (a CONNECT) this tunnel wraps</param>
		/// <param name="sCertCN">The CN to use in the certificate</param>
		/// <param name="sClientCertificateFilename">Path to client certificate file</param>
		/// <param name="sslprotClient">The HTTPS protocol version of the Client Pipe; can influence which SslProtocols we offer the server</param>
		/// <param name="iHandshakeTime">Reference-passed integer which returns the time spent securing the connection</param>
		/// <returns>TRUE if the connection can be secued</returns>
		// Token: 0x06000355 RID: 853 RVA: 0x0001F2F4 File Offset: 0x0001D4F4
		internal bool SecureExistingConnection(Session oS, string sCertCN, string sClientCertificateFilename, SslProtocols sslprotClient, ref int iHandshakeTime)
		{
			this.sPoolKey = this.sPoolKey.Replace("->http/", "->https/");
			if (this.sPoolKey.EndsWith("->*"))
			{
				this.sPoolKey = this.sPoolKey.Replace("->*", string.Format("->https/{0}:{1}", oS.hostname, oS.port));
			}
			X509CertificateCollection oClientCerts = ServerPipe.GetCertificateCollectionFromFile(sClientCertificateFilename);
			Stopwatch oSW = Stopwatch.StartNew();
			try
			{
				Stream strmNet = new NetworkStream(this._baseSocket, false);
				if (ServerPipe._bEatTLSAlerts || oS.oFlags.ContainsKey("https-DropSNIAlerts"))
				{
					strmNet = new ServerPipe.TLSAlertEatingStream(strmNet, oS.host);
				}
				this._httpsStream = new SslStream(strmNet, false, delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
				{
					try
					{
						this._certServer = new X509Certificate2(certificate);
					}
					catch (Exception eX2)
					{
					}
					return ServerPipe.ConfirmServerCertificate(oS, sCertCN, certificate, chain, sslPolicyErrors);
				}, (object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers) => this.AttachClientCertificate(oS, sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers));
				SslProtocols oAcceptedProtocols = CONFIG.oAcceptedServerHTTPSProtocols;
				if (oS.oFlags.ContainsKey("x-OverrideSslProtocols"))
				{
					oAcceptedProtocols = Utilities.ParseSSLProtocolString(oS.oFlags["x-OverrideSslProtocols"]);
				}
				else if (CONFIG.bMimicClientHTTPSProtocols && sslprotClient != SslProtocols.None)
				{
					oAcceptedProtocols |= sslprotClient;
				}
				oAcceptedProtocols = SslProtocolsFilter.EnsureConsecutiveProtocols(oAcceptedProtocols);
				SslClientAuthenticationOptions opt = new SslClientAuthenticationOptions
				{
					TargetHost = sCertCN,
					ClientCertificates = oClientCerts,
					EnabledSslProtocols = oAcceptedProtocols,
					CertificateRevocationCheckMode = (FiddlerApplication.Prefs.GetBoolPref("fiddler.network.https.checkcertificaterevocation", false) ? X509RevocationMode.Offline : X509RevocationMode.NoCheck),
					ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 }
				};
				CancellationToken ct = new CancellationToken(false);
				this._httpsStream.AuthenticateAsClientAsync(opt, ct).Wait();
				iHandshakeTime = (int)oSW.ElapsedMilliseconds;
			}
			catch (Exception eX)
			{
				iHandshakeTime = (int)oSW.ElapsedMilliseconds;
				FiddlerApplication.DebugSpew("SecureExistingConnection failed: {0}\n{1}", new object[]
				{
					Utilities.DescribeException(eX),
					eX.StackTrace
				});
				string sError = string.Format("fiddler.network.https> HTTPS handshake to {0} (for #{1}) failed. {2}\n\n", sCertCN, oS.id, Utilities.DescribeException(eX));
				if (eX is CryptographicException && FiddlerApplication.oDefaultClientCertificate != null)
				{
					sError += "NOTE: A ClientCertificate was supplied. Make certain that the certificate is valid and its public key is accessible in the current user account.\n";
				}
				if (eX is AuthenticationException && eX.InnerException != null && eX.InnerException is Win32Exception)
				{
					Win32Exception exWin32 = (Win32Exception)eX.InnerException;
					if (exWin32.NativeErrorCode == -2146893007)
					{
						sError = sError + "HTTPS handshake returned error SEC_E_ALGORITHM_MISMATCH.\nFiddler's Enabled HTTPS Protocols: [" + CONFIG.oAcceptedServerHTTPSProtocols.ToString() + "] are controlled inside Tools > Options > HTTPS.";
						if (oS.oFlags.ContainsKey("x-OverrideSslProtocols"))
						{
							sError = sError + "\nThis connection specified X-OverrideSslProtocols: " + oS.oFlags["x-OverrideSslProtocols"];
						}
					}
					else
					{
						sError = sError + "Win32 (SChannel) Native Error Code: 0x" + exWin32.NativeErrorCode.ToString("x");
					}
				}
				if (Utilities.IsNullOrEmpty(oS.responseBodyBytes))
				{
					oS.responseBodyBytes = Encoding.UTF8.GetBytes(sError);
				}
				FiddlerApplication.Log.LogString(sError);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Return a Certificate Collection containing certificate from the specified file. 
		/// </summary>
		/// <param name="sClientCertificateFilename">Path to the certificate. Relative Paths will be absolutified automatically</param>
		/// <returns>The Certificate collection, or null</returns>
		// Token: 0x06000356 RID: 854 RVA: 0x0001F664 File Offset: 0x0001D864
		private static X509CertificateCollection GetCertificateCollectionFromFile(string sClientCertificateFilename)
		{
			if (string.IsNullOrEmpty(sClientCertificateFilename))
			{
				return null;
			}
			X509CertificateCollection oReturnCollection = null;
			try
			{
				sClientCertificateFilename = Utilities.EnsurePathIsAbsolute(CONFIG.GetPath("Root"), sClientCertificateFilename);
				if (File.Exists(sClientCertificateFilename))
				{
					oReturnCollection = new X509CertificateCollection();
					oReturnCollection.Add(X509Certificate.CreateFromCertFile(sClientCertificateFilename));
				}
				else
				{
					FiddlerApplication.Log.LogFormat("!! ERROR: Specified client certificate file '{0}' does not exist.", new object[] { sClientCertificateFilename });
				}
			}
			catch (Exception eX)
			{
				string title = "Failed to GetCertificateCollection from " + sClientCertificateFilename;
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[]
				{
					title,
					eX.ToString()
				});
			}
			return oReturnCollection;
		}

		// Token: 0x04000187 RID: 391
		private static object thisLock = new object();

		// Token: 0x04000188 RID: 392
		internal static int _timeoutSendInitial = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.serverpipe.send.initial", -1);

		// Token: 0x04000189 RID: 393
		internal static int _timeoutSendReused = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.serverpipe.send.reuse", -1);

		// Token: 0x0400018A RID: 394
		internal static int _timeoutReceiveInitial = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.serverpipe.receive.initial", -1);

		// Token: 0x0400018B RID: 395
		internal static int _timeoutReceiveReused = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.serverpipe.receive.reuse", -1);

		// Token: 0x0400018C RID: 396
		internal static bool _bEatTLSAlerts = FiddlerApplication.Prefs.GetBoolPref("fiddler.network.https.DropSNIAlerts", false);

		// Token: 0x0400018D RID: 397
		private PipeReusePolicy _reusePolicy;

		/// <summary>
		/// DateTime of the completion of the TCP/IP Connection
		/// </summary>
		// Token: 0x0400018E RID: 398
		internal DateTime dtConnected;

		/// <summary>
		/// TickCount when this Pipe was last placed in a PipePool
		/// </summary>
		// Token: 0x0400018F RID: 399
		internal ulong ulLastPooled;

		/// <summary>
		/// Returns TRUE if this ServerPipe is connected to a Gateway
		/// </summary>
		// Token: 0x04000190 RID: 400
		protected bool _bIsConnectedToGateway;

		/// <summary>
		/// Returns TRUE if this ServerPipe is connected to a SOCKS gateway
		/// </summary>
		// Token: 0x04000191 RID: 401
		private bool _bIsConnectedViaSOCKS;

		/// <summary>
		/// The Pooling key used for reusing a previously pooled ServerPipe. See sPoolKey property.
		/// </summary>
		// Token: 0x04000192 RID: 402
		protected string _sPoolKey;

		/// <summary>
		/// This field, if set, tracks the process ID to which this Pipe is permanently bound; set by MarkAsAuthenticated.
		/// NOTE: This isn't actually checked by anyone; instead the PID is added to the POOL Key
		/// </summary>
		// Token: 0x04000193 RID: 403
		private int _iMarriedToPID;

		/// <summary>
		/// Backing field for the isAuthenticated property
		/// </summary>
		// Token: 0x04000194 RID: 404
		private bool _isAuthenticated;

		/// <summary>
		/// String containing representation of the server's certificate chain
		/// </summary>
		// Token: 0x04000195 RID: 405
		private string _ServerCertChain;

		/// <summary>
		/// Server's certificate
		/// </summary>
		// Token: 0x04000196 RID: 406
		private X509Certificate2 _certServer;

		// Token: 0x020000D0 RID: 208
		internal class TLSAlertEatingStream : Stream
		{
			// Token: 0x0600071A RID: 1818 RVA: 0x00038E2B File Offset: 0x0003702B
			public TLSAlertEatingStream(Stream baseStream, string sHostname)
			{
				this._innerStream = baseStream;
				this._toHost = sHostname;
			}

			// Token: 0x1700011B RID: 283
			// (get) Token: 0x0600071B RID: 1819 RVA: 0x00038E48 File Offset: 0x00037048
			public override bool CanRead
			{
				get
				{
					return this._innerStream.CanRead;
				}
			}

			// Token: 0x1700011C RID: 284
			// (get) Token: 0x0600071C RID: 1820 RVA: 0x00038E55 File Offset: 0x00037055
			public override bool CanSeek
			{
				get
				{
					return false;
				}
			}

			// Token: 0x1700011D RID: 285
			// (get) Token: 0x0600071D RID: 1821 RVA: 0x00038E58 File Offset: 0x00037058
			public override bool CanWrite
			{
				get
				{
					return this._innerStream.CanWrite;
				}
			}

			// Token: 0x0600071E RID: 1822 RVA: 0x00038E68 File Offset: 0x00037068
			public override int Read(byte[] buffer, int offset, int count)
			{
				int iThisRead = this._innerStream.Read(buffer, offset, count);
				if (this.bFirstRead && iThisRead > 1)
				{
					if (buffer[offset] == 21 && iThisRead == 5 && buffer[offset + 3] == 0 && 2 == buffer[offset + 4])
					{
						iThisRead = this._innerStream.Read(buffer, offset, 2);
						if (iThisRead == 2 && 1 == buffer[offset] && 112 == buffer[offset + 1])
						{
							FiddlerApplication.Log.LogString("! Eating a TLS unrecognized_name alert (level: Warning) when connecting to '" + this._toHost + "'");
							iThisRead = this._innerStream.Read(buffer, offset, count);
						}
					}
					this.bFirstRead = false;
					this._toHost = null;
				}
				return iThisRead;
			}

			// Token: 0x0600071F RID: 1823 RVA: 0x00038F09 File Offset: 0x00037109
			public override void Write(byte[] buffer, int offset, int count)
			{
				this._innerStream.Write(buffer, offset, count);
			}

			// Token: 0x06000720 RID: 1824 RVA: 0x00038F19 File Offset: 0x00037119
			protected override void Dispose(bool disposing)
			{
				this._innerStream.Close();
			}

			// Token: 0x06000721 RID: 1825 RVA: 0x00038F26 File Offset: 0x00037126
			public override void Flush()
			{
				this._innerStream.Flush();
			}

			// Token: 0x06000722 RID: 1826 RVA: 0x00038F33 File Offset: 0x00037133
			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotSupportedException();
			}

			// Token: 0x06000723 RID: 1827 RVA: 0x00038F3A File Offset: 0x0003713A
			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}

			// Token: 0x1700011E RID: 286
			// (get) Token: 0x06000724 RID: 1828 RVA: 0x00038F41 File Offset: 0x00037141
			public override long Length
			{
				get
				{
					throw new NotSupportedException();
				}
			}

			// Token: 0x1700011F RID: 287
			// (get) Token: 0x06000725 RID: 1829 RVA: 0x00038F48 File Offset: 0x00037148
			// (set) Token: 0x06000726 RID: 1830 RVA: 0x00038F4F File Offset: 0x0003714F
			public override long Position
			{
				get
				{
					throw new NotSupportedException();
				}
				set
				{
					throw new NotSupportedException();
				}
			}

			// Token: 0x17000120 RID: 288
			// (get) Token: 0x06000727 RID: 1831 RVA: 0x00038F56 File Offset: 0x00037156
			// (set) Token: 0x06000728 RID: 1832 RVA: 0x00038F63 File Offset: 0x00037163
			public override int ReadTimeout
			{
				get
				{
					return this._innerStream.ReadTimeout;
				}
				set
				{
					this._innerStream.ReadTimeout = value;
				}
			}

			// Token: 0x17000121 RID: 289
			// (get) Token: 0x06000729 RID: 1833 RVA: 0x00038F71 File Offset: 0x00037171
			// (set) Token: 0x0600072A RID: 1834 RVA: 0x00038F7E File Offset: 0x0003717E
			public override int WriteTimeout
			{
				get
				{
					return this._innerStream.WriteTimeout;
				}
				set
				{
					this._innerStream.WriteTimeout = value;
				}
			}

			// Token: 0x0400036A RID: 874
			private bool bFirstRead = true;

			// Token: 0x0400036B RID: 875
			private Stream _innerStream;

			// Token: 0x0400036C RID: 876
			private string _toHost;
		}
	}
}
