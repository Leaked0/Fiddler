using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Fiddler
{
	/// <summary>
	/// The WebSocket class represents a "tunnel" through which WebSocket messages flow.
	/// The class' messages may be deserialized from a SAZ file.
	/// </summary>
	// Token: 0x0200006E RID: 110
	public class WebSocket : ITunnel
	{
		// Token: 0x170000E0 RID: 224
		// (get) Token: 0x0600056B RID: 1387 RVA: 0x00032023 File Offset: 0x00030223
		public int MessageCount
		{
			get
			{
				if (this.listMessages == null)
				{
					return 0;
				}
				return this.listMessages.Count;
			}
		}

		// Token: 0x0600056C RID: 1388 RVA: 0x0003203C File Offset: 0x0003023C
		internal void UnfragmentMessages()
		{
			if (this.listMessages == null)
			{
				return;
			}
			List<WebSocketMessage> listFinal = new List<WebSocketMessage>();
			WebSocketMessage wsmPriorInbound = null;
			WebSocketMessage wsmPriorOutbound = null;
			List<WebSocketMessage> obj = this.listMessages;
			lock (obj)
			{
				foreach (WebSocketMessage oWSM in this.listMessages)
				{
					if (oWSM.FrameType != WebSocketFrameTypes.Continuation)
					{
						if (oWSM.IsOutbound)
						{
							wsmPriorOutbound = oWSM;
						}
						else
						{
							wsmPriorInbound = oWSM;
						}
						listFinal.Add(oWSM);
					}
					else if (oWSM.IsOutbound)
					{
						if (wsmPriorOutbound == null)
						{
							listFinal.Add(oWSM);
							wsmPriorOutbound = oWSM;
						}
						else
						{
							wsmPriorOutbound.Assemble(oWSM);
						}
					}
					else if (wsmPriorInbound == null)
					{
						listFinal.Add(oWSM);
						wsmPriorInbound = oWSM;
					}
					else
					{
						wsmPriorInbound.Assemble(oWSM);
					}
				}
			}
			this.listMessages = listFinal;
		}

		/// <summary>
		/// Is this WebSocket open/connected?
		/// </summary>
		// Token: 0x170000E1 RID: 225
		// (get) Token: 0x0600056D RID: 1389 RVA: 0x0003212C File Offset: 0x0003032C
		public bool IsOpen
		{
			get
			{
				return this.bIsOpen;
			}
		}

		/// <summary>
		/// Writes all of the messages stored in this WebSocket to a stream.
		/// </summary>
		/// <param name="oFS"></param>
		/// <returns></returns>
		// Token: 0x0600056E RID: 1390 RVA: 0x00032134 File Offset: 0x00030334
		internal bool WriteWebSocketMessageListToStream(Stream oFS)
		{
			oFS.WriteByte(13);
			oFS.WriteByte(10);
			if (this.listMessages != null)
			{
				List<WebSocketMessage> obj = this.listMessages;
				lock (obj)
				{
					foreach (WebSocketMessage oWSM in this.listMessages)
					{
						oWSM.SerializeToStream(oFS);
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Approximate size of the data of the stored messages, used for memory tracking
		/// </summary>
		/// <returns></returns>
		// Token: 0x0600056F RID: 1391 RVA: 0x000321C8 File Offset: 0x000303C8
		internal int MemoryUsage()
		{
			int i = 0;
			if (this.listMessages != null)
			{
				List<WebSocketMessage> obj = this.listMessages;
				lock (obj)
				{
					foreach (WebSocketMessage oWSM in this.listMessages)
					{
						i += 12 + oWSM.PayloadLength;
					}
				}
			}
			return i;
		}

		/// <summary>
		/// Read headers from the stream.
		/// </summary>
		/// <param name="oFS">The Stream from which WebSocketSerializationHeaders should be read</param>
		/// <returns>The Array of headers, or String[0]</returns>
		// Token: 0x06000570 RID: 1392 RVA: 0x00032254 File Offset: 0x00030454
		private static string[] _ReadHeadersFromStream(Stream oFS)
		{
			List<byte> oHeaderBytes = new List<byte>();
			bool bAtCR = false;
			bool bAtCRLF = true;
			int iByte = oFS.ReadByte();
			while (-1 != iByte)
			{
				if (iByte == 13)
				{
					bAtCR = true;
				}
				else if (bAtCR && iByte == 10)
				{
					if (bAtCRLF)
					{
						break;
					}
					bAtCRLF = true;
					oHeaderBytes.Add(13);
					oHeaderBytes.Add(10);
				}
				else
				{
					bAtCRLF = (bAtCR = false);
					oHeaderBytes.Add((byte)iByte);
				}
				iByte = oFS.ReadByte();
			}
			string sHeaders = Encoding.ASCII.GetString(oHeaderBytes.ToArray());
			return sHeaders.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
		}

		// Token: 0x06000571 RID: 1393 RVA: 0x000322DC File Offset: 0x000304DC
		private static DateTime _GetDateTime(string sDateTimeStr)
		{
			DateTime dtResult;
			if (!DateTime.TryParseExact(sDateTimeStr, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dtResult))
			{
				dtResult = new DateTime(0L);
			}
			return dtResult;
		}

		// Token: 0x06000572 RID: 1394 RVA: 0x0003230C File Offset: 0x0003050C
		internal bool ReadWebSocketMessageListFromStream(Stream oFS)
		{
			bool result;
			try
			{
				string[] slHeaders = WebSocket._ReadHeadersFromStream(oFS);
				List<WebSocketMessage> oMsgs = new List<WebSocketMessage>();
				slHeaders = WebSocket._ReadHeadersFromStream(oFS);
				while (slHeaders != null && slHeaders.Length != 0)
				{
					int iSize = 0;
					bool bIsRequest = false;
					DateTime dtDoneRead = new DateTime(0L);
					DateTime dtBeginSend = new DateTime(0L);
					DateTime dtDoneSend = new DateTime(0L);
					WSMFlags wsmfBitFlags = WSMFlags.None;
					foreach (string sHeader in slHeaders)
					{
						if (sHeader.StartsWith("Request-Length:"))
						{
							bIsRequest = true;
							iSize = int.Parse(sHeader.Substring(16));
						}
						else if (sHeader.StartsWith("Response-Length:"))
						{
							bIsRequest = false;
							iSize = int.Parse(sHeader.Substring(17));
						}
						else if (sHeader.StartsWith("DoneRead:"))
						{
							dtDoneRead = WebSocket._GetDateTime(sHeader.Substring(10));
						}
						else if (sHeader.StartsWith("BeginSend:"))
						{
							dtBeginSend = WebSocket._GetDateTime(sHeader.Substring(11));
						}
						else if (sHeader.StartsWith("DoneSend:"))
						{
							dtDoneSend = WebSocket._GetDateTime(sHeader.Substring(10));
						}
						else if (sHeader.StartsWith("BitFlags:"))
						{
							wsmfBitFlags = (WSMFlags)int.Parse(sHeader.Substring(9));
						}
					}
					if (iSize < 1)
					{
						throw new InvalidDataException("Missing size indication.");
					}
					byte[] arrData = new byte[iSize];
					oFS.Read(arrData, 0, iSize);
					MemoryStream oMS = new MemoryStream(arrData);
					WebSocketMessage[] arrWSM = WebSocket._ParseMessagesFromStream(this, ref oMS, bIsRequest, false);
					if (arrWSM.Length == 1)
					{
						if (dtDoneRead.Ticks > 0L)
						{
							arrWSM[0].Timers.dtDoneRead = dtDoneRead;
						}
						if (dtBeginSend.Ticks > 0L)
						{
							arrWSM[0].Timers.dtBeginSend = dtBeginSend;
						}
						if (dtDoneSend.Ticks > 0L)
						{
							arrWSM[0].Timers.dtDoneSend = dtDoneSend;
						}
						arrWSM[0].SetBitFlags(wsmfBitFlags);
						oMsgs.Add(arrWSM[0]);
					}
					if (-1 == oFS.ReadByte() || -1 == oFS.ReadByte())
					{
						slHeaders = null;
					}
					else
					{
						slHeaders = WebSocket._ReadHeadersFromStream(oFS);
					}
				}
				this.listMessages = oMsgs;
				result = true;
			}
			catch (Exception eX)
			{
				result = false;
			}
			return result;
		}

		// Token: 0x06000573 RID: 1395 RVA: 0x00032538 File Offset: 0x00030738
		public override string ToString()
		{
			return string.Format("Session{0}.WebSocket'{1}'", (this._mySession == null) ? (-1) : this._mySession.id, this.sName);
		}

		/// <summary>
		/// Boolean that determines whether the WebSocket tunnel tracks messages.
		/// </summary>
		// Token: 0x170000E2 RID: 226
		// (get) Token: 0x06000574 RID: 1396 RVA: 0x00032565 File Offset: 0x00030765
		// (set) Token: 0x06000575 RID: 1397 RVA: 0x00032570 File Offset: 0x00030770
		internal bool IsBlind
		{
			get
			{
				return !this.bParseMessages;
			}
			set
			{
				this.bParseMessages = !value;
			}
		}

		/// <summary>
		/// Returns number of bytes sent from the Server to the Client on this WebSocket
		/// </summary>
		// Token: 0x170000E3 RID: 227
		// (get) Token: 0x06000576 RID: 1398 RVA: 0x0003257C File Offset: 0x0003077C
		public long IngressByteCount
		{
			get
			{
				return this._lngIngressByteCount;
			}
		}

		/// <summary>
		/// Returns number of bytes sent from the Client to the Server on this WebSocket
		/// </summary>
		// Token: 0x170000E4 RID: 228
		// (get) Token: 0x06000577 RID: 1399 RVA: 0x00032584 File Offset: 0x00030784
		public long EgressByteCount
		{
			get
			{
				return this._lngEgressByteCount;
			}
		}

		/// <summary>
		/// Creates a "detached" WebSocket which contains messages loaded from the specified stream
		/// </summary>
		/// <param name="oS">Session to which the WebSocket messages belong</param>
		/// <param name="strmWSMessages">The Stream containing messages, which will be closed upon completion</param>
		// Token: 0x06000578 RID: 1400 RVA: 0x0003258C File Offset: 0x0003078C
		internal static void LoadWebSocketMessagesFromStream(Session oS, Stream strmWSMessages)
		{
			try
			{
				WebSocket oNewTunnel = new WebSocket(oS, null, null);
				oNewTunnel.sName = string.Format("SAZ-Session#{0}", oS.id);
				oS.__oTunnel = oNewTunnel;
				oNewTunnel.ReadWebSocketMessageListFromStream(strmWSMessages);
			}
			finally
			{
				strmWSMessages.Dispose();
			}
		}

		/// <summary>
		/// This factory method creates a new WebSocket Tunnel and executes it on a background (non-pooled) thread.
		/// </summary>
		/// <param name="oSession">The Session containing the HTTP CONNECT request</param>
		// Token: 0x06000579 RID: 1401 RVA: 0x000325E8 File Offset: 0x000307E8
		internal static void CreateTunnel(Session oSession)
		{
			if (oSession == null || oSession.oRequest == null || oSession.oRequest.headers == null || oSession.oRequest.pipeClient == null)
			{
				return;
			}
			if (oSession.oResponse == null || oSession.oResponse.pipeServer == null)
			{
				return;
			}
			ClientPipe oFrom = oSession.oRequest.pipeClient;
			oSession.oRequest.pipeClient = null;
			ServerPipe oTo = oSession.oResponse.pipeServer;
			oSession.oResponse.pipeServer = null;
			WebSocket oNewTunnel = new WebSocket(oSession, oFrom, oTo);
			oSession.__oTunnel = oNewTunnel;
			new Thread(new ThreadStart(oNewTunnel.RunTunnel))
			{
				IsBackground = true
			}.Start();
		}

		/// <summary>
		/// Creates a WebSocket tunnel. External callers instead use the CreateTunnel static method.
		/// </summary>
		/// <param name="oSess">The session for which this tunnel was initially created.</param>
		/// <param name="oFrom">The client pipe</param>
		/// <param name="oTo">The server pipe</param>
		// Token: 0x0600057A RID: 1402 RVA: 0x00032690 File Offset: 0x00030890
		private WebSocket(Session oSess, ClientPipe oFrom, ServerPipe oTo)
		{
			this.sName = "WebSocket #" + oSess.id.ToString();
			this._mySession = oSess;
			this.oCP = oFrom;
			this.oSP = oTo;
			this._mySession.SetBitFlag(SessionFlags.IsWebSocketTunnel, true);
			if (this._mySession.isAnyFlagSet(SessionFlags.Ignored) || oSess.oFlags.ContainsKey("x-no-parse"))
			{
				this.bParseMessages = false;
				return;
			}
			if (oSess.oFlags.ContainsKey("x-Parse-WebSocketMessages"))
			{
				this.bParseMessages = true;
			}
		}

		/// <summary>
		/// This function keeps the Tunnel/Thread alive until it is signaled that the traffic is complete
		/// </summary>
		// Token: 0x0600057B RID: 1403 RVA: 0x00032749 File Offset: 0x00030949
		private void WaitForCompletion()
		{
			AutoResetEvent autoResetEvent = this.oKeepTunnelAlive;
			this.oKeepTunnelAlive = new AutoResetEvent(false);
			this.oKeepTunnelAlive.WaitOne();
			this.oKeepTunnelAlive.Close();
			this.oKeepTunnelAlive = null;
		}

		/// <summary>
		/// Performs cleanup of the WebSocket instance. Call this after the WebSocket closes normally or after abort/exceptions.
		/// </summary>
		// Token: 0x0600057C RID: 1404 RVA: 0x0003277C File Offset: 0x0003097C
		private void _CleanupWebSocket()
		{
			this.bIsOpen = false;
			this.arrRequestBytes = (this.arrResponseBytes = null);
			this.strmServerBytes = null;
			this.strmClientBytes = null;
			if (this.oCP != null)
			{
				this.oCP.End();
			}
			if (this.oSP != null)
			{
				this.oSP.End();
			}
			this.oCP = null;
			this.oSP = null;
			if (this._mySession != null)
			{
				if (Utilities.HasHeaders(this._mySession.oResponse))
				{
					this._mySession.oResponse.headers["EndTime"] = DateTime.Now.ToString("HH:mm:ss.fff");
					this._mySession.oResponse.headers["ReceivedBytes"] = this._lngIngressByteCount.ToString();
					this._mySession.oResponse.headers["SentBytes"] = this._lngEgressByteCount.ToString();
				}
				this._mySession.Timers.ServerDoneResponse = (this._mySession.Timers.ClientBeginResponse = (this._mySession.Timers.ClientDoneResponse = DateTime.Now));
				this._mySession = null;
			}
		}

		/// <summary>
		/// Executes the WebSocket tunnel on a background thread
		/// </summary>
		// Token: 0x0600057D RID: 1405 RVA: 0x000328B8 File Offset: 0x00030AB8
		private void RunTunnel()
		{
			if (FiddlerApplication.oProxy == null)
			{
				return;
			}
			this.arrRequestBytes = new byte[16384];
			this.arrResponseBytes = new byte[16384];
			if (this.bParseMessages)
			{
				this.strmClientBytes = new MemoryStream();
				this.strmServerBytes = new MemoryStream();
				this.listMessages = new List<WebSocketMessage>();
			}
			this.bIsOpen = true;
			try
			{
				this.oCP.BeginReceive(this.arrRequestBytes, 0, this.arrRequestBytes.Length, SocketFlags.None, new AsyncCallback(this.OnReceiveFromClient), this.oCP);
				this.oSP.BeginReceive(this.arrResponseBytes, 0, this.arrResponseBytes.Length, SocketFlags.None, new AsyncCallback(this.OnReceiveFromServer), this.oSP);
				this.WaitForCompletion();
			}
			catch (Exception eX)
			{
			}
			this.CloseTunnel();
		}

		/// <summary>
		/// Interface Method
		/// Close the WebSocket and signal the event to let its service thread die. Also called by oSession.Abort()
		/// WARNING: This should not be allowed to throw any exceptions, because it will do so on threads that don't 
		/// catch them, and this will kill the application.
		/// </summary>
		// Token: 0x0600057E RID: 1406 RVA: 0x0003299C File Offset: 0x00030B9C
		public void CloseTunnel()
		{
			if (CONFIG.bDebugSpew)
			{
				FiddlerApplication.Log.LogString("Close WebSocket Tunnel: " + Environment.StackTrace);
			}
			try
			{
				if (this.oKeepTunnelAlive != null)
				{
					this.oKeepTunnelAlive.Set();
				}
				else
				{
					this._CleanupWebSocket();
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogString("Error closing oKeepTunnelAlive. " + eX.Message + "\n" + eX.StackTrace);
			}
		}

		/// <summary>
		/// When we get a buffer from the client, we push it into the memory stream
		/// </summary>
		// Token: 0x0600057F RID: 1407 RVA: 0x00032A20 File Offset: 0x00030C20
		private void _PushClientBuffer(int iReadCount)
		{
			this.strmClientBytes.Write(this.arrRequestBytes, 0, iReadCount);
			this._ParseAndSendClientMessages();
		}

		/// <summary>
		/// When we get a buffer from the server, we push it into the memory stream
		/// </summary>
		// Token: 0x06000580 RID: 1408 RVA: 0x00032A3B File Offset: 0x00030C3B
		private void _PushServerBuffer(int iReadCount)
		{
			this.strmServerBytes.Write(this.arrResponseBytes, 0, iReadCount);
			this._ParseAndSendServerMessages();
		}

		// Token: 0x06000581 RID: 1409 RVA: 0x00032A58 File Offset: 0x00030C58
		private static WebSocketMessage[] _ParseMessagesFromStream(WebSocket wsOwner, ref MemoryStream strmData, bool bIsOutbound, bool bTrimAfterParsing)
		{
			List<WebSocketMessage> oMsgList = new List<WebSocketMessage>();
			strmData.Position = 0L;
			long iEndOfLastFullMessage = 0L;
			while (strmData.Length - strmData.Position >= 2L)
			{
				byte[] arrHeader = new byte[2];
				strmData.Read(arrHeader, 0, arrHeader.Length);
				ulong iSize = (ulong)((long)(arrHeader[1] & 127));
				if (iSize == 126UL)
				{
					if (strmData.Length < strmData.Position + 2L)
					{
						break;
					}
					byte[] arrSize = new byte[2];
					strmData.Read(arrSize, 0, arrSize.Length);
					iSize = (ulong)((long)((long)arrSize[0] << 8) + (long)((ulong)arrSize[1]));
				}
				else if (iSize == 127UL)
				{
					if (strmData.Length < strmData.Position + 8L)
					{
						break;
					}
					byte[] arrSize2 = new byte[8];
					strmData.Read(arrSize2, 0, arrSize2.Length);
					iSize = (ulong)((long)(((int)arrSize2[0] << 24) + ((int)arrSize2[1] << 16) + ((int)arrSize2[2] << 8) + (int)arrSize2[3] + ((int)arrSize2[4] << 24) + ((int)arrSize2[5] << 16) + ((int)arrSize2[6] << 8) + (int)arrSize2[7]));
				}
				bool bMasked = 128 == (arrHeader[1] & 128);
				if (strmData.Length < strmData.Position + (long)iSize + (bMasked ? 4L : 0L))
				{
					break;
				}
				WebSocketMessage oMessage = new WebSocketMessage(wsOwner, Interlocked.Increment(ref wsOwner._iMsgCount), bIsOutbound);
				oMessage.AssignHeader(arrHeader[0]);
				if (bMasked)
				{
					byte[] arrKey = new byte[4];
					strmData.Read(arrKey, 0, arrKey.Length);
					oMessage.MaskingKey = arrKey;
				}
				byte[] arrPayload = new byte[iSize];
				strmData.Read(arrPayload, 0, arrPayload.Length);
				oMessage.PayloadData = arrPayload;
				oMsgList.Add(oMessage);
				iEndOfLastFullMessage = strmData.Position;
			}
			strmData.Position = iEndOfLastFullMessage;
			if (bTrimAfterParsing)
			{
				byte[] arrLeftovers = new byte[strmData.Length - iEndOfLastFullMessage];
				strmData.Read(arrLeftovers, 0, arrLeftovers.Length);
				strmData.Dispose();
				strmData = new MemoryStream();
				strmData.Write(arrLeftovers, 0, arrLeftovers.Length);
			}
			return oMsgList.ToArray();
		}

		/// <summary>
		/// This method parses the data in strmClientBytes to extact one or more WebSocket messages. It then sends each message
		/// through the pipeline.
		/// </summary>
		// Token: 0x06000582 RID: 1410 RVA: 0x00032C50 File Offset: 0x00030E50
		private void _ParseAndSendClientMessages()
		{
			WebSocketMessage[] arrMessages = WebSocket._ParseMessagesFromStream(this, ref this.strmClientBytes, true, true);
			foreach (WebSocketMessage oWSM in arrMessages)
			{
				oWSM.Timers.dtDoneRead = DateTime.Now;
				List<WebSocketMessage> obj = this.listMessages;
				lock (obj)
				{
					this.listMessages.Add(oWSM);
				}
				FiddlerApplication.DoOnWebSocketMessage(this._mySession, oWSM);
				if (!oWSM.WasAborted)
				{
					oWSM.Timers.dtBeginSend = DateTime.Now;
					this.oSP.Send(oWSM.ToByteArray());
					oWSM.Timers.dtDoneSend = DateTime.Now;
				}
			}
		}

		/// This method parses the data in strmServerBytes to extact one or more WebSocket messages. It then sends each message
		/// through the pipeline to the client.
		// Token: 0x06000583 RID: 1411 RVA: 0x00032D1C File Offset: 0x00030F1C
		private void _ParseAndSendServerMessages()
		{
			WebSocketMessage[] arrMessages = WebSocket._ParseMessagesFromStream(this, ref this.strmServerBytes, false, true);
			foreach (WebSocketMessage oWSM in arrMessages)
			{
				oWSM.Timers.dtDoneRead = DateTime.Now;
				List<WebSocketMessage> obj = this.listMessages;
				lock (obj)
				{
					this.listMessages.Add(oWSM);
				}
				FiddlerApplication.DoOnWebSocketMessage(this._mySession, oWSM);
				if (!oWSM.WasAborted)
				{
					oWSM.Timers.dtBeginSend = DateTime.Now;
					this.oCP.Send(oWSM.ToByteArray());
					oWSM.Timers.dtDoneSend = DateTime.Now;
				}
			}
		}

		/// <summary>
		///  Called when we have received data from the local client.
		/// </summary>
		/// <param name="ar">The result of the asynchronous operation.</param>
		// Token: 0x06000584 RID: 1412 RVA: 0x00032DE8 File Offset: 0x00030FE8
		protected void OnReceiveFromClient(IAsyncResult ar)
		{
			try
			{
				int iReadCount = this.oCP.EndReceive(ar);
				if (iReadCount > 0)
				{
					this._lngEgressByteCount += (long)iReadCount;
					if (this.bParseMessages)
					{
						this._PushClientBuffer(iReadCount);
					}
					else
					{
						this.oSP.Send(this.arrRequestBytes, 0, iReadCount);
					}
					this.oCP.BeginReceive(this.arrRequestBytes, 0, this.arrRequestBytes.Length, SocketFlags.None, new AsyncCallback(this.OnReceiveFromClient), this.oCP);
				}
				else
				{
					if (this.bParseMessages)
					{
						FiddlerApplication.Log.LogFormat("[{0}] Read from Client returned error: {1}", new object[] { this.sName, iReadCount });
					}
					this.CloseTunnel();
				}
			}
			catch (Exception eX)
			{
				if (this.bParseMessages)
				{
					FiddlerApplication.Log.LogFormat("[{0}] Read from Client failed... {1}", new object[] { this.sName, eX.Message });
				}
				this.CloseTunnel();
			}
		}

		/// <summary>Called when we have received data from the remote host. Incoming data will immediately be forwarded to the local client.</summary>
		/// <param name="ar">The result of the asynchronous operation.</param>
		// Token: 0x06000585 RID: 1413 RVA: 0x00032EE8 File Offset: 0x000310E8
		protected void OnReceiveFromServer(IAsyncResult ar)
		{
			try
			{
				int iReadCount = this.oSP.EndReceive(ar);
				if (iReadCount > 0)
				{
					this._lngIngressByteCount += (long)iReadCount;
					if (this.bParseMessages)
					{
						this._PushServerBuffer(iReadCount);
					}
					else
					{
						this.oCP.Send(this.arrResponseBytes, 0, iReadCount);
					}
					this.oSP.BeginReceive(this.arrResponseBytes, 0, this.arrResponseBytes.Length, SocketFlags.None, new AsyncCallback(this.OnReceiveFromServer), this.oSP);
				}
				else
				{
					if (this.bParseMessages)
					{
						FiddlerApplication.Log.LogFormat("[{0}] Read from Server returned error: {1}", new object[] { this.sName, iReadCount });
					}
					this.CloseTunnel();
				}
			}
			catch (Exception eX)
			{
				if (this.bParseMessages)
				{
					FiddlerApplication.Log.LogFormat("[{0}] Read from Server failed... {1}", new object[] { this.sName, eX.Message });
				}
				this.CloseTunnel();
			}
		}

		// Token: 0x0400025C RID: 604
		private ClientPipe oCP;

		// Token: 0x0400025D RID: 605
		private ServerPipe oSP;

		// Token: 0x0400025E RID: 606
		private Session _mySession;

		// Token: 0x0400025F RID: 607
		private string sName = "Unknown";

		// Token: 0x04000260 RID: 608
		private byte[] arrRequestBytes;

		// Token: 0x04000261 RID: 609
		private byte[] arrResponseBytes;

		// Token: 0x04000262 RID: 610
		private int _iMsgCount;

		// Token: 0x04000263 RID: 611
		private MemoryStream strmClientBytes;

		// Token: 0x04000264 RID: 612
		private MemoryStream strmServerBytes;

		// Token: 0x04000265 RID: 613
		public List<WebSocketMessage> listMessages;

		// Token: 0x04000266 RID: 614
		private AutoResetEvent oKeepTunnelAlive;

		/// <summary>
		/// Should this WebSocket Tunnel parse the WS traffic within into individual messages?
		/// </summary>
		// Token: 0x04000267 RID: 615
		private bool bParseMessages = FiddlerApplication.Prefs.GetBoolPref("fiddler.websocket.ParseMessages", true);

		// Token: 0x04000268 RID: 616
		private bool bIsOpen;

		/// <summary>
		/// Number of bytes received from the client
		/// </summary>
		// Token: 0x04000269 RID: 617
		private long _lngEgressByteCount;

		/// <summary>
		/// Number of bytes received from the server
		/// </summary>
		// Token: 0x0400026A RID: 618
		private long _lngIngressByteCount;
	}
}
