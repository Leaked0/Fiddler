using System;
using System.Net.Sockets;
using System.Threading;

namespace Fiddler
{
	/// <summary>
	/// The GenericTunnel class represents a "blind tunnel" to shuffle bytes between a client and the server.
	/// </summary>
	// Token: 0x0200003E RID: 62
	internal class GenericTunnel : ITunnel
	{
		// Token: 0x17000076 RID: 118
		// (get) Token: 0x06000264 RID: 612 RVA: 0x00016A10 File Offset: 0x00014C10
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
		// Token: 0x17000077 RID: 119
		// (get) Token: 0x06000265 RID: 613 RVA: 0x00016A18 File Offset: 0x00014C18
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
		// Token: 0x17000078 RID: 120
		// (get) Token: 0x06000266 RID: 614 RVA: 0x00016A20 File Offset: 0x00014C20
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
		// Token: 0x06000267 RID: 615 RVA: 0x00016A28 File Offset: 0x00014C28
		internal static void CreateTunnel(Session oSession, bool bStreamResponse)
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
			if (bStreamResponse)
			{
				oSession.oRequest.pipeClient = null;
			}
			ServerPipe oPS = oSession.oResponse.pipeServer;
			if (oPS == null)
			{
				return;
			}
			if (bStreamResponse)
			{
				oSession.oResponse.pipeServer = null;
			}
			GenericTunnel oNewTunnel = new GenericTunnel(oSession, oPC, oPS, bStreamResponse);
			oSession.__oTunnel = oNewTunnel;
			new Thread(new ThreadStart(oNewTunnel.RunTunnel))
			{
				IsBackground = true
			}.Start();
		}

		/// <summary>
		/// Creates a tunnel. External callers instead use the CreateTunnel static method.
		/// </summary>
		/// <param name="oSess">The session for which this tunnel was initially created.</param>
		/// <param name="oFrom">Client Pipe</param>
		/// <param name="oTo">Server Pipe</param>
		// Token: 0x06000268 RID: 616 RVA: 0x00016AD4 File Offset: 0x00014CD4
		private GenericTunnel(Session oSess, ClientPipe oFrom, ServerPipe oTo, bool bStreamResponse)
		{
			this._mySession = oSess;
			this.pipeToClient = oFrom;
			this.pipeToRemote = oTo;
			this.bResponseStreamStarted = bStreamResponse;
			this._mySession.SetBitFlag(SessionFlags.IsBlindTunnel, true);
			FiddlerApplication.DebugSpew("[GenericTunnel] For session #" + this._mySession.id.ToString() + " created...");
		}

		/// <summary>
		/// This function keeps the thread alive until it is signaled that the traffic is complete
		/// </summary>
		// Token: 0x06000269 RID: 617 RVA: 0x00016B44 File Offset: 0x00014D44
		private void WaitForCompletion()
		{
			AutoResetEvent autoResetEvent = this.oKeepTunnelAlive;
			this.oKeepTunnelAlive = new AutoResetEvent(false);
			FiddlerApplication.DebugSpew("[GenericTunnel] Blocking thread...");
			this.oKeepTunnelAlive.WaitOne();
			FiddlerApplication.DebugSpew("[GenericTunnel] Unblocking thread...");
			this.oKeepTunnelAlive.Close();
			this.oKeepTunnelAlive = null;
			this.bIsOpen = false;
			this.arrRequestBytes = (this.arrResponseBytes = null);
			this.pipeToClient = null;
			this.pipeToRemote = null;
			FiddlerApplication.DebugSpew("[GenericTunnel] Thread for session #" + this._mySession.id.ToString() + " has died...");
			if (this._mySession.oResponse != null && this._mySession.oResponse.headers != null)
			{
				this._mySession.oResponse.headers["EndTime"] = DateTime.Now.ToString("HH:mm:ss.fff");
				this._mySession.oResponse.headers["ClientToServerBytes"] = this._lngEgressByteCount.ToString();
				this._mySession.oResponse.headers["ServerToClientBytes"] = this._lngIngressByteCount.ToString();
			}
			this._mySession.Timers.ServerDoneResponse = (this._mySession.Timers.ClientBeginResponse = (this._mySession.Timers.ClientDoneResponse = DateTime.Now));
			this._mySession.state = SessionStates.Done;
			this._mySession = null;
		}

		/// <summary>
		/// Executes the HTTPS tunnel inside an All-it-can-eat exception handler.
		/// Call from a background thread.
		/// </summary>
		// Token: 0x0600026A RID: 618 RVA: 0x00016CCC File Offset: 0x00014ECC
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

		/// <summary>
		/// Executes the WebSocket tunnel on a background thread
		/// </summary>
		// Token: 0x0600026B RID: 619 RVA: 0x00016D40 File Offset: 0x00014F40
		private void DoTunnel()
		{
			if (FiddlerApplication.oProxy == null)
			{
				return;
			}
			this.arrRequestBytes = new byte[16384];
			this.arrResponseBytes = new byte[16384];
			this.bIsOpen = true;
			if (CONFIG.bDebugSpew)
			{
				FiddlerApplication.DebugSpew(string.Format("Generic Tunnel for Session #{0}, Response State: {1}, created between\n\t{2}\nand\n\t{3}", new object[]
				{
					this._mySession.id,
					this.bResponseStreamStarted ? "Streaming" : "Blocked",
					this.pipeToClient,
					this.pipeToRemote
				}));
			}
			try
			{
				this.pipeToClient.BeginReceive(this.arrRequestBytes, 0, this.arrRequestBytes.Length, SocketFlags.None, new AsyncCallback(this.OnClientReceive), null);
				if (this.bResponseStreamStarted)
				{
					this.pipeToRemote.BeginReceive(this.arrResponseBytes, 0, this.arrResponseBytes.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteReceive), null);
				}
				this.WaitForCompletion();
			}
			catch (Exception eX)
			{
			}
			this.CloseTunnel();
		}

		/// <summary>
		/// Instructs the tunnel to take over the server pipe and begin streaming responses to the client
		/// </summary>
		// Token: 0x0600026C RID: 620 RVA: 0x00016E50 File Offset: 0x00015050
		internal void BeginResponseStreaming()
		{
			FiddlerApplication.DebugSpew(">>> Begin response streaming in GenericTunnel for Session #" + this._mySession.id.ToString());
			this.bResponseStreamStarted = true;
			this._mySession.oResponse.pipeServer = null;
			this._mySession.oRequest.pipeClient = null;
			this.pipeToRemote.BeginReceive(this.arrResponseBytes, 0, this.arrResponseBytes.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteReceive), null);
		}

		/// <summary>
		/// Close the HTTPS tunnel and signal the event to let the service thread die.
		/// WARNING: This MUST not be allowed to throw any exceptions, because it will do so on threads that don't catch them, and this will kill the application.
		/// </summary>
		// Token: 0x0600026D RID: 621 RVA: 0x00016ED4 File Offset: 0x000150D4
		public void CloseTunnel()
		{
			FiddlerApplication.DebugSpew("Close Generic Tunnel for Session #" + ((this._mySession != null) ? this._mySession.id.ToString() : "<unassigned>"));
			try
			{
				if (this.pipeToClient != null)
				{
					this.pipeToClient.End();
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.DebugSpew("Error closing gatewayFrom tunnel. " + eX.Message + "\n" + eX.StackTrace);
			}
			try
			{
				if (this.pipeToRemote != null)
				{
					this.pipeToRemote.End();
				}
			}
			catch (Exception eX2)
			{
				FiddlerApplication.DebugSpew("Error closing gatewayTo tunnel. " + eX2.Message + "\n" + eX2.StackTrace);
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
				FiddlerApplication.DebugSpew("Error closing oKeepTunnelAlive. " + eX3.Message + "\n" + eX3.StackTrace);
			}
		}

		/// <summary>
		///  Called when we have received data from the local client.
		///  Incoming data will immediately be forwarded to the remote host.
		/// </summary>
		/// <param name="ar">The result of the asynchronous operation.</param>
		// Token: 0x0600026E RID: 622 RVA: 0x00016FE8 File Offset: 0x000151E8
		protected void OnClientReceive(IAsyncResult ar)
		{
			try
			{
				int Ret = this.pipeToClient.EndReceive(ar);
				if (Ret > 0)
				{
					this._lngEgressByteCount += (long)Ret;
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("[GenericTunnel] Received from client: " + Ret.ToString() + " bytes. Sending to server...");
						FiddlerApplication.DebugSpew(Utilities.ByteArrayToHexView(this.arrRequestBytes, 16, Ret));
					}
					FiddlerApplication.DoReadRequestBuffer(this._mySession, this.arrRequestBytes, Ret);
					this.pipeToRemote.Send(this.arrRequestBytes, 0, Ret);
					this.pipeToClient.BeginReceive(this.arrRequestBytes, 0, this.arrRequestBytes.Length, SocketFlags.None, new AsyncCallback(this.OnClientReceive), null);
				}
				else
				{
					FiddlerApplication.DoReadRequestBuffer(this._mySession, this.arrRequestBytes, 0);
					this.CloseTunnel();
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.DebugSpew("[GenericTunnel] OnClientReceive threw... " + eX.Message);
				this.CloseTunnel();
			}
		}

		/// <summary>Called when we have sent data to the local client.<br>When all the data has been sent, we will start receiving again from the remote host.</br></summary>
		/// <param name="ar">The result of the asynchronous operation.</param>
		// Token: 0x0600026F RID: 623 RVA: 0x000170E8 File Offset: 0x000152E8
		protected void OnClientSent(IAsyncResult ar)
		{
			try
			{
				FiddlerApplication.DebugSpew("OnClientSent...");
				this.pipeToClient.EndSend(ar);
			}
			catch (Exception eX)
			{
				FiddlerApplication.DebugSpew("[GenericTunnel] OnClientSent failed... " + eX.Message);
				this.CloseTunnel();
			}
		}

		/// <summary>Called when we have sent data to the remote host.<br>When all the data has been sent, we will start receiving again from the local client.</br></summary>
		/// <param name="ar">The result of the asynchronous operation.</param>
		// Token: 0x06000270 RID: 624 RVA: 0x0001713C File Offset: 0x0001533C
		protected void OnRemoteSent(IAsyncResult ar)
		{
			try
			{
				FiddlerApplication.DebugSpew("OnRemoteSent...");
				this.pipeToRemote.EndSend(ar);
			}
			catch (Exception eX)
			{
				FiddlerApplication.DebugSpew("[GenericTunnel] OnRemoteSent failed... " + eX.Message);
				this.CloseTunnel();
			}
		}

		/// <summary>Called when we have received data from the remote host.<br>Incoming data will immediately be forwarded to the local client.</br></summary>
		/// <param name="ar">The result of the asynchronous operation.</param>
		// Token: 0x06000271 RID: 625 RVA: 0x00017190 File Offset: 0x00015390
		protected void OnRemoteReceive(IAsyncResult ar)
		{
			try
			{
				int Ret = this.pipeToRemote.EndReceive(ar);
				if (Ret > 0)
				{
					this._lngIngressByteCount += (long)Ret;
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("[GenericTunnel] Received from server: " + Ret.ToString() + " bytes. Sending to client...");
						FiddlerApplication.DebugSpew(Utilities.ByteArrayToHexView(this.arrResponseBytes, 16, Ret));
					}
					FiddlerApplication.DoReadResponseBuffer(this._mySession, this.arrResponseBytes, Ret);
					this.pipeToClient.Send(this.arrResponseBytes, 0, Ret);
					this.pipeToRemote.BeginReceive(this.arrResponseBytes, 0, this.arrResponseBytes.Length, SocketFlags.None, new AsyncCallback(this.OnRemoteReceive), null);
				}
				else
				{
					FiddlerApplication.DebugSpew("[GenericTunnel] ReadFromRemote failed, ret=" + Ret.ToString());
					FiddlerApplication.DoReadResponseBuffer(this._mySession, this.arrResponseBytes, 0);
					this.CloseTunnel();
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.DebugSpew("[GenericTunnel] OnRemoteReceive failed... " + eX.Message);
				this.CloseTunnel();
			}
		}

		// Token: 0x04000111 RID: 273
		private ClientPipe pipeToClient;

		// Token: 0x04000112 RID: 274
		private ServerPipe pipeToRemote;

		/// <summary>
		/// Is streaming started in the downstream direction?
		/// </summary>
		// Token: 0x04000113 RID: 275
		private bool bResponseStreamStarted;

		// Token: 0x04000114 RID: 276
		private Session _mySession;

		// Token: 0x04000115 RID: 277
		private byte[] arrRequestBytes;

		// Token: 0x04000116 RID: 278
		private byte[] arrResponseBytes;

		// Token: 0x04000117 RID: 279
		private AutoResetEvent oKeepTunnelAlive;

		// Token: 0x04000118 RID: 280
		private bool bIsOpen = true;

		/// <summary>
		/// Number of bytes received from the client
		/// </summary>
		// Token: 0x04000119 RID: 281
		private long _lngEgressByteCount;

		/// <summary>
		/// Number of bytes received from the server
		/// </summary>
		// Token: 0x0400011A RID: 282
		private long _lngIngressByteCount;
	}
}
