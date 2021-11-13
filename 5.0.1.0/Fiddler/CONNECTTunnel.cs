using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Fiddler
{
	/// <summary>
	/// The CONNECTTunnel class represents a "blind tunnel" through which a CONNECT request is serviced to shuffle bytes between a client and the server.
	/// </summary>
	/// <remarks>
	/// See pg 206 in HTTP: The Complete Reference for details on how Tunnels work.
	/// When HTTPS Decryption is disabled, Fiddler accepts a CONNECT request from the client. Then, we open a connection to the remote server. 
	/// We shuttle bytes back and forth between the client and the server in this tunnel, keeping Fiddler itself out of the loop
	/// (no tampering, etc). 
	/// </remarks>
	// Token: 0x0200001C RID: 28
	internal class CONNECTTunnel : ITunnel
	{
		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000159 RID: 345 RVA: 0x00011E8F File Offset: 0x0001008F
		public bool IsOpen
		{
			get
			{
				return this.bIsOpen;
			}
		}

		/// <summary>
		/// Returns number of bytes sent from the Server to the Client
		/// </summary>
		// Token: 0x1700004E RID: 78
		// (get) Token: 0x0600015A RID: 346 RVA: 0x00011E97 File Offset: 0x00010097
		public long IngressByteCount
		{
			get
			{
				return this._lngIngressByteCount;
			}
		}

		/// <summary>
		/// Returns number of bytes sent from the Client to the Server
		/// </summary>
		// Token: 0x1700004F RID: 79
		// (get) Token: 0x0600015B RID: 347 RVA: 0x00011E9F File Offset: 0x0001009F
		public long EgressByteCount
		{
			get
			{
				return this._lngEgressByteCount;
			}
		}

		/// <summary>
		/// This "Factory" method creates a new HTTPS Tunnel and executes it on a background (non-pooled) thread.
		/// </summary>
		/// <param name="oSession">The Session containing the HTTP CONNECT request</param>
		// Token: 0x0600015C RID: 348 RVA: 0x00011EA8 File Offset: 0x000100A8
		internal static void CreateTunnel(Session oSession)
		{
			if (oSession == null || oSession.oRequest == null || oSession.oRequest.headers == null || oSession.oRequest.pipeClient == null || oSession.oResponse == null)
			{
				return;
			}
			ClientPipe oPC = oSession.oRequest.pipeClient;
			if (oPC == null)
			{
				return;
			}
			oSession.oRequest.pipeClient = null;
			ServerPipe oPS = oSession.oResponse.pipeServer;
			if (oPS == null)
			{
				return;
			}
			oSession.oResponse.pipeServer = null;
			CONNECTTunnel oNewTunnel = new CONNECTTunnel(oSession, oPC, oPS);
			oSession.__oTunnel = oNewTunnel;
			new Thread(new ThreadStart(oNewTunnel.RunTunnel))
			{
				IsBackground = true
			}.Start();
		}

		/// <summary>
		/// Creates a HTTPS tunnel. External callers instead use the CreateTunnel static method.
		/// </summary>
		/// <param name="oSess">The session for which this tunnel was initially created.</param>
		/// <param name="oFrom">Client Pipe</param>
		/// <param name="oTo">Server Pipe</param>
		// Token: 0x0600015D RID: 349 RVA: 0x00011F4A File Offset: 0x0001014A
		private CONNECTTunnel(Session oSess, ClientPipe oFrom, ServerPipe oTo)
		{
			this._mySession = oSess;
			this.pipeTunnelClient = oFrom;
			this.pipeTunnelRemote = oTo;
			this._mySession.SetBitFlag(SessionFlags.IsBlindTunnel, true);
		}

		/// <summary>
		/// This function keeps the thread alive until it is signaled that the traffic is complete
		/// </summary>
		// Token: 0x0600015E RID: 350 RVA: 0x00011F80 File Offset: 0x00010180
		private void WaitForCompletion()
		{
			AutoResetEvent autoResetEvent = this.oKeepTunnelAlive;
			this.oKeepTunnelAlive = new AutoResetEvent(false);
			this.oKeepTunnelAlive.WaitOne();
			this.oKeepTunnelAlive.Close();
			this.oKeepTunnelAlive = null;
			this.bIsOpen = false;
			this.arrRequestBytes = (this.arrResponseBytes = null);
			this.pipeTunnelClient = null;
			this.pipeTunnelRemote = null;
			this.socketClient = (this.socketRemote = null);
			if (Utilities.HasHeaders(this._mySession.oResponse))
			{
				this._mySession.oResponse.headers["EndTime"] = DateTime.Now.ToString("HH:mm:ss.fff");
				this._mySession.oResponse.headers["ClientToServerBytes"] = this._lngEgressByteCount.ToString();
				this._mySession.oResponse.headers["ServerToClientBytes"] = this._lngIngressByteCount.ToString();
			}
			this._mySession.Timers.ServerDoneResponse = (this._mySession.Timers.ClientBeginResponse = (this._mySession.Timers.ClientDoneResponse = DateTime.Now));
			this._mySession = null;
		}

		/// <summary>
		/// Executes the HTTPS tunnel inside an All-it-can-eat exception handler.
		/// Call from a background thread.
		/// </summary>
		// Token: 0x0600015F RID: 351 RVA: 0x000120C0 File Offset: 0x000102C0
		private void RunTunnel()
		{
			if (FiddlerApplication.oProxy == null)
			{
				return;
			}
			try
			{
				this.DoTunnel();
			}
			catch (Exception eX)
			{
				string title = "Uncaught Exception in Tunnel; Session #" + this._mySession.id.ToString();
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[]
				{
					title,
					eX.ToString()
				});
			}
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00012134 File Offset: 0x00010334
		private void DoTunnel()
		{
			try
			{
				this.bIsBlind = !CONFIG.DecryptHTTPS || this._mySession.oFlags.ContainsKey("x-no-decrypt");
				if (this._mySession.oFlags.ContainsKey("x-instrumented-browser-decrypt"))
				{
					this.bIsBlind = false;
				}
				if (!this.bIsBlind)
				{
					this.bIsBlind = CONFIG.ShouldSkipDecryption(this._mySession.PathAndQuery);
				}
				if (!this.bIsBlind && CONFIG.DecryptWhichProcesses != ProcessFilterCategories.All)
				{
					string sProc = this._mySession.oFlags["x-ProcessInfo"];
					if (CONFIG.DecryptWhichProcesses == ProcessFilterCategories.HideAll)
					{
						if (!string.IsNullOrEmpty(sProc))
						{
							this.bIsBlind = true;
						}
					}
					else if (!string.IsNullOrEmpty(sProc))
					{
						bool bIsBrowser = Utilities.IsBrowserProcessName(sProc);
						if ((CONFIG.DecryptWhichProcesses == ProcessFilterCategories.Browsers && !bIsBrowser) || (CONFIG.DecryptWhichProcesses == ProcessFilterCategories.NonBrowsers && bIsBrowser))
						{
							this.bIsBlind = true;
						}
					}
				}
				bool bServerPipeSecured;
				X509Certificate2 certServer;
				for (;;)
				{
					this._mySession.SetBitFlag(SessionFlags.IsDecryptingTunnel, !this.bIsBlind);
					this._mySession.SetBitFlag(SessionFlags.IsBlindTunnel, this.bIsBlind);
					if (this.bIsBlind)
					{
						break;
					}
					bServerPipeSecured = false;
					if (!this._mySession.oFlags.ContainsKey("x-OverrideCertCN"))
					{
						if (CONFIG.bUseSNIForCN)
						{
							string sSNI = this._mySession.oFlags["https-Client-SNIHostname"];
							if (!string.IsNullOrEmpty(sSNI) && sSNI != this._mySession.hostname)
							{
								this._mySession.oFlags["x-OverrideCertCN"] = this._mySession.oFlags["https-Client-SNIHostname"];
							}
						}
						if (this._mySession.oFlags["x-OverrideCertCN"] == null && this._mySession.oFlags.ContainsKey("x-UseCertCNFromServer"))
						{
							if (!this.pipeTunnelRemote.SecureExistingConnection(this._mySession, this._mySession.hostname, this._mySession.oFlags["https-Client-Certificate"], SslProtocols.None, ref this._mySession.Timers.HTTPSHandshakeTime))
							{
								goto Block_18;
							}
							bServerPipeSecured = true;
							string sServerCN = this.pipeTunnelRemote.GetServerCertCN();
							if (!string.IsNullOrEmpty(sServerCN))
							{
								this._mySession.oFlags["x-OverrideCertCN"] = sServerCN;
							}
						}
					}
					string sCertCN = this._mySession.oFlags["x-OverrideCertCN"] ?? Utilities.StripIPv6LiteralBrackets(this._mySession.hostname);
					try
					{
						certServer = CertMaker.FindCert(sCertCN);
						if (certServer == null)
						{
							throw new Exception("Certificate Maker returned null when asked for a certificate for " + sCertCN);
						}
					}
					catch (Exception eX)
					{
						certServer = null;
						FiddlerApplication.Log.LogFormat("fiddler.https> Failed to obtain certificate for {0} due to {1}", new object[] { sCertCN, eX.Message });
						this._mySession.oFlags["x-HTTPS-Decryption-Error"] = "Could not find or generate interception certificate.";
						if (!bServerPipeSecured && FiddlerApplication.Prefs.GetBoolPref("fiddler.network.https.blindtunnelifcertunobtainable", true))
						{
							this.bIsBlind = true;
							continue;
						}
					}
					goto IL_2F0;
				}
				this.DoBlindTunnel();
				return;
				Block_18:
				throw new Exception("HTTPS Early-Handshaking to server did not succeed.");
				IL_2F0:
				if (!bServerPipeSecured)
				{
					SslProtocols sslprotClient = this._PeekClientHelloVersion();
					string sCertCN;
					if (!this.pipeTunnelRemote.SecureExistingConnection(this._mySession, sCertCN, this._mySession.oFlags["https-Client-Certificate"], sslprotClient, ref this._mySession.Timers.HTTPSHandshakeTime))
					{
						throw new Exception("HTTPS Handshaking to server did not succeed.");
					}
				}
				if (!this.pipeTunnelClient.SecureClientPipeDirect(certServer))
				{
					throw new Exception("HTTPS Handshaking to client did not succeed.");
				}
				this._mySession["https-Client-Version"] = this.pipeTunnelClient.SecureProtocol.ToString();
				string sConnectionDescriptionIntro = "Encrypted HTTPS traffic flows through this CONNECT tunnel. HTTPS Decryption is enabled in Fiddler, so decrypted sessions running in this tunnel will be shown in the Web Sessions list.\n\n";
				string sConnectionDescription = this.pipeTunnelRemote.DescribeConnectionSecurity();
				this._mySession.responseBodyBytes = Encoding.UTF8.GetBytes(sConnectionDescriptionIntro + sConnectionDescription);
				this._mySession["https-Server-Cipher"] = this.pipeTunnelRemote.GetConnectionCipherInfo();
				this._mySession["https-Server-Version"] = this.pipeTunnelRemote.SecureProtocol.ToString();
				Session oSecuredSession = new Session(this.pipeTunnelClient, this.pipeTunnelRemote);
				oSecuredSession.oFlags["x-serversocket"] = this._mySession.oFlags["x-securepipe"];
				if (this.pipeTunnelRemote != null && this.pipeTunnelRemote.Address != null)
				{
					oSecuredSession.m_hostIP = this.pipeTunnelRemote.Address.ToString();
					oSecuredSession.oFlags["x-hostIP"] = oSecuredSession.m_hostIP;
					oSecuredSession.oFlags["x-EgressPort"] = this.pipeTunnelRemote.LocalPort.ToString();
				}
				oSecuredSession.Execute(null);
			}
			catch (Exception eX2)
			{
				try
				{
					this.pipeTunnelClient.End();
					this.pipeTunnelRemote.End();
				}
				catch (Exception eeX)
				{
				}
			}
		}

		// Token: 0x06000161 RID: 353 RVA: 0x00012654 File Offset: 0x00010854
		private SslProtocols _PeekClientHelloVersion()
		{
			SslProtocols sslprotClient = SslProtocols.None;
			if (this.pipeTunnelClient != null)
			{
				byte[] arrSniff = new byte[16];
				int iPeekCount = this.pipeTunnelClient.GetRawSocket().Receive(arrSniff, SocketFlags.Peek);
				if (iPeekCount > 3 && arrSniff[0] == 22)
				{
					if (iPeekCount > 10)
					{
						sslprotClient = this._parseSslProt(arrSniff[9], arrSniff[10]);
					}
					else if (iPeekCount > 3)
					{
						sslprotClient = this._parseSslProt(arrSniff[1], arrSniff[2]);
					}
				}
			}
			return sslprotClient;
		}

		// Token: 0x06000162 RID: 354 RVA: 0x000126B9 File Offset: 0x000108B9
		private SslProtocols _parseSslProt(byte b1, byte b2)
		{
			if (b1 != 3)
			{
				return SslProtocols.None;
			}
			switch (b2)
			{
			case 0:
				return SslProtocols.Ssl3;
			case 1:
				return SslProtocols.Tls;
			case 2:
				return SslProtocols.Tls11;
			case 3:
				return SslProtocols.Tls12;
			default:
				return SslProtocols.None;
			}
		}

		// Token: 0x06000163 RID: 355 RVA: 0x000126F0 File Offset: 0x000108F0
		private void DoBlindTunnel()
		{
			this.arrRequestBytes = new byte[16384];
			this.arrResponseBytes = new byte[16384];
			this.socketClient = this.pipeTunnelClient.GetRawSocket();
			this.socketRemote = this.pipeTunnelRemote.GetRawSocket();
			this.socketClient.BeginReceive(this.arrRequestBytes, 0, this.arrRequestBytes.Length, SocketFlags.None, new AsyncCallback(this.OnClientReceive), this.socketClient);
			this.socketRemote.BeginReceive(this.arrResponseBytes, 0, this.arrResponseBytes.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteReceive), this.socketRemote);
			this.WaitForCompletion();
		}

		/// <summary>
		/// Close the HTTPS tunnel and signal the event to let the service thread die.
		/// WARNING: This MUST not be allowed to throw any exceptions, because it will do so on threads that don't catch them, and this will kill the application.
		/// </summary>
		// Token: 0x06000164 RID: 356 RVA: 0x000127A4 File Offset: 0x000109A4
		public void CloseTunnel()
		{
			try
			{
				if (this.pipeTunnelClient != null)
				{
					this.pipeTunnelClient.End();
				}
			}
			catch (Exception eX)
			{
			}
			try
			{
				if (this.pipeTunnelRemote != null)
				{
					this.pipeTunnelRemote.End();
				}
			}
			catch (Exception eX2)
			{
			}
			try
			{
				if (this.oKeepTunnelAlive != null)
				{
					this.oKeepTunnelAlive.Set();
				}
			}
			catch (Exception eX3)
			{
			}
		}

		/// <summary>
		/// 	Called when we have received data from the local client.
		/// 	Incoming data will immediately be forwarded to the remote host.
		/// </summary>
		/// <param name="ar">The result of the asynchronous operation.</param>
		// Token: 0x06000165 RID: 357 RVA: 0x00012824 File Offset: 0x00010A24
		protected void OnClientReceive(IAsyncResult ar)
		{
			try
			{
				int Ret = this.socketClient.EndReceive(ar);
				if (Ret > 0)
				{
					this._lngEgressByteCount += (long)Ret;
					FiddlerApplication.DoReadRequestBuffer(this._mySession, this.arrRequestBytes, Ret);
					if (this._mySession.requestBodyBytes == null || (long)this._mySession.requestBodyBytes.Length == 0L)
					{
						try
						{
							HTTPSClientHello oHello = new HTTPSClientHello();
							if (oHello.LoadFromStream(new MemoryStream(this.arrRequestBytes, 0, Ret, false)))
							{
								this._mySession.requestBodyBytes = Encoding.UTF8.GetBytes(oHello.ToString() + "\n");
								this._mySession["https-Client-SessionID"] = oHello.SessionID;
								if (!string.IsNullOrEmpty(oHello.ServerNameIndicator))
								{
									this._mySession["https-Client-SNIHostname"] = oHello.ServerNameIndicator;
								}
							}
						}
						catch (Exception eX)
						{
							this._mySession.requestBodyBytes = Encoding.UTF8.GetBytes("Request HTTPSParse failed: " + eX.Message);
						}
					}
					this.socketRemote.BeginSend(this.arrRequestBytes, 0, Ret, SocketFlags.None, new AsyncCallback(this.OnRemoteSent), this.socketRemote);
				}
				else
				{
					FiddlerApplication.DoReadRequestBuffer(this._mySession, this.arrRequestBytes, 0);
					this.CloseTunnel();
				}
			}
			catch (Exception eX2)
			{
				this.CloseTunnel();
			}
		}

		/// <summary>Called when we have sent data to the local client.<br>When all the data has been sent, we will start receiving again from the remote host.</br></summary>
		/// <param name="ar">The result of the asynchronous operation.</param>
		// Token: 0x06000166 RID: 358 RVA: 0x000129AC File Offset: 0x00010BAC
		protected void OnClientSent(IAsyncResult ar)
		{
			try
			{
				int Ret = this.socketClient.EndSend(ar);
				if (Ret > 0)
				{
					this.socketRemote.BeginReceive(this.arrResponseBytes, 0, this.arrResponseBytes.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteReceive), this.socketRemote);
				}
			}
			catch (Exception eX)
			{
			}
		}

		/// <summary>Called when we have sent data to the remote host.<br>When all the data has been sent, we will start receiving again from the local client.</br></summary>
		/// <param name="ar">The result of the asynchronous operation.</param>
		// Token: 0x06000167 RID: 359 RVA: 0x00012A10 File Offset: 0x00010C10
		protected void OnRemoteSent(IAsyncResult ar)
		{
			try
			{
				int Ret = this.socketRemote.EndSend(ar);
				if (Ret > 0)
				{
					this.socketClient.BeginReceive(this.arrRequestBytes, 0, this.arrRequestBytes.Length, SocketFlags.None, new AsyncCallback(this.OnClientReceive), this.socketClient);
				}
			}
			catch (Exception eX)
			{
			}
		}

		/// <summary>Called when we have received data from the remote host.<br>Incoming data will immediately be forwarded to the local client.</br></summary>
		/// <param name="ar">The result of the asynchronous operation.</param>
		// Token: 0x06000168 RID: 360 RVA: 0x00012A74 File Offset: 0x00010C74
		protected void OnRemoteReceive(IAsyncResult ar)
		{
			try
			{
				int Ret = this.socketRemote.EndReceive(ar);
				if (Ret > 0)
				{
					this._lngIngressByteCount += (long)Ret;
					FiddlerApplication.DoReadResponseBuffer(this._mySession, this.arrResponseBytes, Ret);
					if (Utilities.IsNullOrEmpty(this._mySession.responseBodyBytes))
					{
						try
						{
							HTTPSServerHello oHello = new HTTPSServerHello();
							if (oHello.LoadFromStream(new MemoryStream(this.arrResponseBytes, 0, Ret, false)))
							{
								string sDecryptTip = (CONFIG.DecryptHTTPS ? string.Format("Fiddler's HTTPS Decryption feature is enabled, but this specific tunnel was configured not to be decrypted. {0}", this._mySession.oFlags.ContainsKey("X-No-Decrypt") ? (" Session Flag 'X-No-Decrypt' was set to: '" + this._mySession.oFlags["X-No-Decrypt"] + "'.") : "Settings can be found inside Settings > HTTPS.") : "To view the encrypted sessions inside this tunnel, enable the Settings > HTTPS > Capture HTTPS traffic option.");
								string sMessage = string.Format("This is a CONNECT tunnel, through which encrypted HTTPS traffic flows.\n{0}\n\n{1}\n", sDecryptTip, oHello.ToString());
								this._mySession.responseBodyBytes = Encoding.UTF8.GetBytes(sMessage);
								this._mySession["https-Server-SessionID"] = oHello.SessionID;
								this._mySession["https-Server-Cipher"] = oHello.CipherSuite;
								this._mySession["https-Server-ProtocolVersion"] = oHello.ProtocolVersion();
							}
						}
						catch (Exception eX)
						{
							this._mySession.requestBodyBytes = Encoding.UTF8.GetBytes("Response HTTPSParse failed: " + eX.Message);
						}
					}
					this.socketClient.BeginSend(this.arrResponseBytes, 0, Ret, SocketFlags.None, new AsyncCallback(this.OnClientSent), this.socketClient);
				}
				else
				{
					FiddlerApplication.DoReadResponseBuffer(this._mySession, this.arrResponseBytes, 0);
					this.CloseTunnel();
				}
			}
			catch (Exception eX2)
			{
				this.CloseTunnel();
			}
		}

		// Token: 0x040000A9 RID: 169
		private Socket socketRemote;

		// Token: 0x040000AA RID: 170
		private Socket socketClient;

		// Token: 0x040000AB RID: 171
		private ClientPipe pipeTunnelClient;

		// Token: 0x040000AC RID: 172
		private ServerPipe pipeTunnelRemote;

		// Token: 0x040000AD RID: 173
		private Session _mySession;

		// Token: 0x040000AE RID: 174
		private byte[] arrRequestBytes;

		// Token: 0x040000AF RID: 175
		private byte[] arrResponseBytes;

		// Token: 0x040000B0 RID: 176
		private AutoResetEvent oKeepTunnelAlive;

		// Token: 0x040000B1 RID: 177
		private bool bIsOpen = true;

		/// <summary>
		/// Number of bytes received from the client
		/// </summary>
		// Token: 0x040000B2 RID: 178
		private long _lngEgressByteCount;

		/// <summary>
		/// Number of bytes received from the server
		/// </summary>
		// Token: 0x040000B3 RID: 179
		private long _lngIngressByteCount;

		/// <summary>
		/// TRUE if this is a Blind tunnel, FALSE if decrypting
		/// </summary>
		// Token: 0x040000B4 RID: 180
		private bool bIsBlind;
	}
}
