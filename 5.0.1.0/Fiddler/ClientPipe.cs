using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using FiddlerCore.Utilities;

namespace Fiddler
{
	/// <summary>
	/// A ClientPipe wraps a socket connection to a client application.
	/// </summary>
	// Token: 0x02000051 RID: 81
	public class ClientPipe : BasePipe
	{
		/// <summary>
		/// ID of the process that opened this socket, assuming that Port Mapping is enabled, and the connection is from the local machine
		/// </summary>
		// Token: 0x17000095 RID: 149
		// (get) Token: 0x06000320 RID: 800 RVA: 0x0001D9CC File Offset: 0x0001BBCC
		public int LocalProcessID
		{
			get
			{
				return this._iProcessID;
			}
		}

		/// <summary>
		/// Does this Pipe have data (or closure/errors) to read?
		/// </summary>
		/// <returns>TRUE if this Pipe requires attention</returns>
		// Token: 0x06000321 RID: 801 RVA: 0x0001D9D4 File Offset: 0x0001BBD4
		public override bool HasDataAvailable()
		{
			bool result;
			try
			{
				if (this._arrReceivedAndPutBack != null)
				{
					result = true;
				}
				else
				{
					result = base.HasDataAvailable();
				}
			}
			catch
			{
				result = true;
			}
			return result;
		}

		/// <summary>
		/// If you previously read more bytes than you needed from this client socket, you can put some back.
		/// </summary>
		/// <param name="toPutback">Array of bytes to put back; now owned by this object</param>
		// Token: 0x06000322 RID: 802 RVA: 0x0001DA0C File Offset: 0x0001BC0C
		internal void putBackSomeBytes(byte[] toPutback)
		{
			this._arrReceivedAndPutBack = toPutback;
		}

		// Token: 0x06000323 RID: 803 RVA: 0x0001DA18 File Offset: 0x0001BC18
		internal new int Receive(byte[] arrBuffer)
		{
			if (this._arrReceivedAndPutBack == null)
			{
				return base.Receive(arrBuffer);
			}
			int iRecoveredBufferLength = this._arrReceivedAndPutBack.Length;
			Buffer.BlockCopy(this._arrReceivedAndPutBack, 0, arrBuffer, 0, iRecoveredBufferLength);
			this._arrReceivedAndPutBack = null;
			return iRecoveredBufferLength;
		}

		/// <summary>
		/// Name of the Process referred to by LocalProcessID, or String.Empty if unknown
		/// </summary>
		// Token: 0x17000096 RID: 150
		// (get) Token: 0x06000324 RID: 804 RVA: 0x0001DA55 File Offset: 0x0001BC55
		public string LocalProcessName
		{
			get
			{
				return this._sProcessName ?? string.Empty;
			}
		}

		// Token: 0x06000325 RID: 805 RVA: 0x0001DA68 File Offset: 0x0001BC68
		internal ClientPipe(Socket oSocket, DateTime dtCreationTime)
			: base(oSocket, "C")
		{
			try
			{
				this.dtAccepted = dtCreationTime;
				oSocket.NoDelay = true;
				if (ClientChatter.s_SO_RCVBUF_Option >= 0)
				{
					oSocket.ReceiveBufferSize = ClientChatter.s_SO_RCVBUF_Option;
				}
				if (ClientChatter.s_SO_SNDBUF_Option >= 0)
				{
					oSocket.SendBufferSize = ClientChatter.s_SO_SNDBUF_Option;
				}
				if (CONFIG.bDebugSpew)
				{
					FiddlerApplication.DebugSpew("[ClientPipe]\n SendBufferSize:\t{0}\n ReceiveBufferSize:\t{1}\n SendTimeout:\t{2}\n ReceiveTimeOut:\t{3}\n NoDelay:\t{4}", new object[] { oSocket.SendBufferSize, oSocket.ReceiveBufferSize, oSocket.SendTimeout, oSocket.ReceiveTimeout, oSocket.NoDelay });
				}
				this._setProcessName();
			}
			catch
			{
			}
		}

		// Token: 0x06000326 RID: 806 RVA: 0x0001DB30 File Offset: 0x0001BD30
		private void _setProcessName()
		{
			if (!CONFIG.bMapSocketToProcess)
			{
				return;
			}
			if (CONFIG.bAllowRemoteConnections && !ClientPipe._ProcessLookupSkipsLoopbackCheck && !(this._baseSocket.LocalEndPoint as IPEndPoint).Address.Equals((this._baseSocket.RemoteEndPoint as IPEndPoint).Address))
			{
				return;
			}
			this._iProcessID = FiddlerSock.MapLocalPortToProcessId(base.Port);
			if (this._iProcessID > 0)
			{
				this._sProcessName = ProcessHelper.GetProcessName(this._iProcessID);
			}
		}

		/// <summary>
		/// Sets the socket's timeout based on whether we're waiting for our first read or for an ongoing read-loop
		/// </summary>
		// Token: 0x06000327 RID: 807 RVA: 0x0001DBB0 File Offset: 0x0001BDB0
		internal void setReceiveTimeout(bool bFirstRead)
		{
			try
			{
				this._baseSocket.ReceiveTimeout = (bFirstRead ? ClientPipe._timeoutFirstReceive : ClientPipe._timeoutReceiveLoop);
			}
			catch
			{
			}
		}

		/// <summary>
		/// Returns a semicolon-delimited string describing this ClientPipe
		/// </summary>
		/// <returns>A semicolon-delimited string</returns>
		// Token: 0x06000328 RID: 808 RVA: 0x0001DBEC File Offset: 0x0001BDEC
		public override string ToString()
		{
			return string.Format("[ClientPipe: {0}:{1}; UseCnt: {2}[{3}]; Port: {4}; {5} established {6}]", new object[]
			{
				this._sProcessName,
				this._iProcessID,
				this.iUseCount,
				string.Empty,
				base.Port,
				base.bIsSecured ? "SECURE" : "PLAINTTEXT",
				this.dtAccepted
			});
		}

		/// <summary>
		/// Perform a HTTPS Server handshake to the client. Swallows exception and returns false on failure.
		/// </summary>
		/// <param name="certServer"></param>
		/// <returns></returns>
		// Token: 0x06000329 RID: 809 RVA: 0x0001DC6C File Offset: 0x0001BE6C
		internal bool SecureClientPipeDirect(X509Certificate2 certServer)
		{
			try
			{
				FiddlerApplication.DebugSpew("SecureClientPipeDirect({0})", new object[] { certServer.Subject });
				SslStream httpsStream = this._httpsStream;
				this._httpsStream = new SslStream(new NetworkStream(this._baseSocket, false), false);
				SslProtocols sslProtocols = CONFIG.oAcceptedClientHTTPSProtocols;
				sslProtocols = SslProtocolsFilter.EnsureConsecutiveProtocols(sslProtocols);
				SslServerAuthenticationOptions opt = new SslServerAuthenticationOptions
				{
					ServerCertificate = certServer,
					ClientCertificateRequired = ClientPipe._bWantClientCert,
					EnabledSslProtocols = sslProtocols,
					CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
					ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 }
				};
				CancellationToken ct = new CancellationToken(false);
				this._httpsStream.AuthenticateAsServerAsync(opt, ct).Wait();
				return true;
			}
			catch (AuthenticationException aEX)
			{
				FiddlerApplication.Log.LogFormat("!SecureClientPipeDirect failed: {1} for pipe ({0}).", new object[]
				{
					certServer.Subject,
					Utilities.DescribeException(aEX)
				});
				base.End();
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("!SecureClientPipeDirect failed: {1} for pipe ({0})", new object[]
				{
					certServer.Subject,
					Utilities.DescribeException(eX)
				});
				base.End();
			}
			return false;
		}

		/// <summary>
		/// This function sends the client socket a CONNECT ESTABLISHED, and then performs a HTTPS authentication
		/// handshake, with Fiddler acting as the server.
		/// </summary>
		/// <param name="sHostname">Hostname Fiddler is pretending to be (NO PORT!)</param>
		/// <param name="oHeaders">The set of headers to be returned to the client in response to the client's CONNECT tunneling request</param>
		/// <returns>true if the handshake succeeds</returns>
		// Token: 0x0600032A RID: 810 RVA: 0x0001DD9C File Offset: 0x0001BF9C
		internal bool SecureClientPipe(string sHostname, HTTPResponseHeaders oHeaders)
		{
			X509Certificate2 certServer;
			try
			{
				certServer = CertMaker.FindCert(sHostname);
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("fiddler.https> Failed to obtain certificate for {0} due to {1}", new object[] { sHostname, eX.Message });
				certServer = null;
			}
			try
			{
				if (certServer == null)
				{
					FiddlerApplication.Log.LogFormat("!WARNING: Unable to find or create Certificate for {0}", new object[] { sHostname });
					oHeaders.SetStatus(502, "Fiddler unable to find or create certificate");
				}
				if (CONFIG.bDebugSpew)
				{
					FiddlerApplication.DebugSpew("SecureClientPipe for: " + this.ToString() + " sending data to client:\n" + Utilities.ByteArrayToHexView(oHeaders.ToByteArray(true, true), 32));
				}
				base.Send(oHeaders.ToByteArray(true, true));
				if (oHeaders.HTTPResponseCode != 200)
				{
					FiddlerApplication.DebugSpew("SecureClientPipe returning FALSE because HTTPResponseCode != 200");
					return false;
				}
				this._httpsStream = new SslStream(new NetworkStream(this._baseSocket, false), false);
				SslProtocols sslProtocols = CONFIG.oAcceptedClientHTTPSProtocols;
				sslProtocols = SslProtocolsFilter.EnsureConsecutiveProtocols(sslProtocols);
				SslServerAuthenticationOptions opt = new SslServerAuthenticationOptions
				{
					ServerCertificate = certServer,
					ClientCertificateRequired = ClientPipe._bWantClientCert,
					EnabledSslProtocols = sslProtocols,
					CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
					ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11 }
				};
				CancellationToken ct = new CancellationToken(false);
				this._httpsStream.AuthenticateAsServerAsync(opt, ct).Wait();
				return true;
			}
			catch (Exception eX2)
			{
				FiddlerApplication.Log.LogFormat("SecureClientPipe ({0} failed: {1}.", new object[]
				{
					sHostname,
					Utilities.DescribeException(eX2)
				});
				try
				{
					base.End();
				}
				catch (Exception eeX)
				{
				}
			}
			return false;
		}

		/// <summary>
		/// Timestamp of either 1&gt; The underlying socket's creation from a .Accept() call, or 2&gt; when this ClientPipe was created.
		/// </summary>
		// Token: 0x17000097 RID: 151
		// (get) Token: 0x0600032B RID: 811 RVA: 0x0001DF68 File Offset: 0x0001C168
		// (set) Token: 0x0600032C RID: 812 RVA: 0x0001DF70 File Offset: 0x0001C170
		internal DateTime dtAccepted { get; set; }

		/// <summary>
		/// By default, we now test for loopbackness before lookup of PID
		/// https://github.com/telerik/fiddler/issues/83
		/// </summary>
		// Token: 0x0400016F RID: 367
		internal static bool _ProcessLookupSkipsLoopbackCheck = FiddlerApplication.Prefs.GetBoolPref("fiddler.proxy.ProcessLookupSkipsLoopbackCheck", false);

		/// <summary>
		/// Timeout to wait for the *first* data from the client
		/// </summary>
		// Token: 0x04000170 RID: 368
		internal static int _timeoutFirstReceive = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.clientpipe.receive.initial", 45000);

		/// <summary>
		/// Timeout to wait for the ongoing reads from the client (as headers and body are read)
		/// </summary>
		// Token: 0x04000171 RID: 369
		internal static int _timeoutReceiveLoop = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.clientpipe.receive.loop", 60000);

		/// <summary>
		/// Timeout before which an idle connection is closed (e.g. for HTTP Keep-Alive)
		/// </summary>
		// Token: 0x04000172 RID: 370
		internal static int _timeoutIdle = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.clientpipe.idle", 115000);

		// Token: 0x04000173 RID: 371
		internal static int _cbLimitRequestHeaders = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.limit.maxrequestheaders", 1048576);

		// Token: 0x04000174 RID: 372
		private static bool _bWantClientCert = FiddlerApplication.Prefs.GetBoolPref("fiddler.network.https.requestclientcertificate", false);

		/// <summary>
		/// Client process name (e.g. "iexplore")
		/// </summary>
		// Token: 0x04000175 RID: 373
		private string _sProcessName;

		/// <summary>
		/// Client process ProcessID
		/// </summary>
		// Token: 0x04000176 RID: 374
		private int _iProcessID;

		/// <summary>
		/// Data which was previously "over-read" from the client. Populated when HTTP-pipelining is attempted
		/// </summary>
		// Token: 0x04000177 RID: 375
		private byte[] _arrReceivedAndPutBack;
	}
}
