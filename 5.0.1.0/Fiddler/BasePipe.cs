using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;

namespace Fiddler
{
	/// <summary>
	/// Abstract base class for the ClientPipe and ServerPipe classes. A Pipe represents a connection to either the client or the server, optionally encrypted using SSL/TLS.
	/// </summary>
	// Token: 0x02000050 RID: 80
	public abstract class BasePipe
	{
		/// <summary>
		/// Create a new pipe, an enhanced wrapper around a socket
		/// </summary>
		/// <param name="oSocket">Socket which this pipe wraps</param>
		/// <param name="sName">Identification string used for debugging purposes</param>
		// Token: 0x0600030B RID: 779 RVA: 0x0001D594 File Offset: 0x0001B794
		public BasePipe(Socket oSocket, string sName)
		{
			this._sPipeName = sName;
			this._baseSocket = oSocket;
		}

		/// <summary>
		/// Return the Connected status of the base socket. 
		/// WARNING: This doesn't work as you might expect; you can see Connected == false when a READ timed out but a WRITE will succeed.
		/// </summary>
		// Token: 0x1700008E RID: 142
		// (get) Token: 0x0600030C RID: 780 RVA: 0x0001D5AA File Offset: 0x0001B7AA
		public bool Connected
		{
			get
			{
				return this._baseSocket != null && this._baseSocket.Connected;
			}
		}

		/// <summary>
		/// Poll the underlying socket for readable data (or closure/errors)
		/// </summary>
		/// <returns>TRUE if this Pipe requires attention</returns>
		// Token: 0x0600030D RID: 781 RVA: 0x0001D5C1 File Offset: 0x0001B7C1
		public virtual bool HasDataAvailable()
		{
			return this.Connected && this._baseSocket.Poll(0, SelectMode.SelectRead);
		}

		/// <summary>
		/// Returns a bool indicating if the socket in this Pipe is CURRENTLY connected and wrapped in a SecureStream
		/// </summary>
		// Token: 0x1700008F RID: 143
		// (get) Token: 0x0600030E RID: 782 RVA: 0x0001D5DA File Offset: 0x0001B7DA
		public bool bIsSecured
		{
			get
			{
				return this._httpsStream != null;
			}
		}

		/// <summary>
		/// Returns the SSL/TLS protocol securing this connection
		/// </summary>
		// Token: 0x17000090 RID: 144
		// (get) Token: 0x0600030F RID: 783 RVA: 0x0001D5E5 File Offset: 0x0001B7E5
		public SslProtocols SecureProtocol
		{
			get
			{
				if (this._httpsStream == null)
				{
					return SslProtocols.None;
				}
				return this._httpsStream.SslProtocol;
			}
		}

		/// <summary>
		/// Return the Remote Port to which this socket is attached.
		/// </summary>
		// Token: 0x17000091 RID: 145
		// (get) Token: 0x06000310 RID: 784 RVA: 0x0001D5FC File Offset: 0x0001B7FC
		public int Port
		{
			get
			{
				int result;
				try
				{
					if (this._baseSocket != null && this._baseSocket.RemoteEndPoint != null)
					{
						result = (this._baseSocket.RemoteEndPoint as IPEndPoint).Port;
					}
					else
					{
						result = 0;
					}
				}
				catch
				{
					result = 0;
				}
				return result;
			}
		}

		/// <summary>
		/// Return the Local Port to which the base socket is attached. Note: May return a misleading port if the ISA Firewall Client is in use.
		/// </summary>
		// Token: 0x17000092 RID: 146
		// (get) Token: 0x06000311 RID: 785 RVA: 0x0001D650 File Offset: 0x0001B850
		public int LocalPort
		{
			get
			{
				int result;
				try
				{
					if (this._baseSocket != null && this._baseSocket.LocalEndPoint != null)
					{
						result = (this._baseSocket.LocalEndPoint as IPEndPoint).Port;
					}
					else
					{
						result = 0;
					}
				}
				catch
				{
					result = 0;
				}
				return result;
			}
		}

		/// <summary>
		/// Returns the remote address to which this Pipe is connected, or 0.0.0.0 on error.
		/// </summary>
		// Token: 0x17000093 RID: 147
		// (get) Token: 0x06000312 RID: 786 RVA: 0x0001D6A4 File Offset: 0x0001B8A4
		public IPAddress Address
		{
			get
			{
				IPAddress result;
				try
				{
					if (this._baseSocket == null || this._baseSocket.RemoteEndPoint == null)
					{
						result = new IPAddress(0L);
					}
					else
					{
						result = (this._baseSocket.RemoteEndPoint as IPEndPoint).Address;
					}
				}
				catch
				{
					result = new IPAddress(0L);
				}
				return result;
			}
		}

		/// <summary>
		/// Gets or sets the transmission delay on this Pipe, used for performance simulation purposes.
		/// </summary>
		// Token: 0x17000094 RID: 148
		// (get) Token: 0x06000313 RID: 787 RVA: 0x0001D704 File Offset: 0x0001B904
		// (set) Token: 0x06000314 RID: 788 RVA: 0x0001D70C File Offset: 0x0001B90C
		public int TransmitDelay
		{
			get
			{
				return this._iTransmitDelayMS;
			}
			set
			{
				this._iTransmitDelayMS = value;
			}
		}

		/// <summary>
		/// Call this method when about to reuse a socket. Currently, increments the socket's UseCount and resets its transmit delay to 0.
		/// </summary>
		/// <param name="iSession">The session identifier of the new session, or zero</param>
		// Token: 0x06000315 RID: 789 RVA: 0x0001D715 File Offset: 0x0001B915
		internal void IncrementUse(int iSession)
		{
			this._iTransmitDelayMS = 0;
			this.iUseCount += 1U;
		}

		/// <summary>
		/// Sends a byte array through this pipe
		/// </summary>
		/// <param name="oBytes">The bytes</param>
		// Token: 0x06000316 RID: 790 RVA: 0x0001D72C File Offset: 0x0001B92C
		public void Send(byte[] oBytes)
		{
			this.Send(oBytes, 0, oBytes.Length);
		}

		/// <summary>
		/// Sends the data specified in oBytes (between iOffset and iOffset+iCount-1 inclusive) down the pipe.
		/// </summary>
		/// <param name="oBytes"></param>
		/// <param name="iOffset"></param>
		/// <param name="iCount"></param>
		// Token: 0x06000317 RID: 791 RVA: 0x0001D73C File Offset: 0x0001B93C
		internal void Send(byte[] oBytes, int iOffset, int iCount)
		{
			if (oBytes == null)
			{
				return;
			}
			if ((long)(iOffset + iCount) > (long)oBytes.Length)
			{
				iCount = oBytes.Length - iOffset;
			}
			if (iCount < 1)
			{
				return;
			}
			if (this._iTransmitDelayMS >= 1)
			{
				int iBlockSize = 1024;
				for (int iWroteSoFar = iOffset; iWroteSoFar < iOffset + iCount; iWroteSoFar += iBlockSize)
				{
					if (iWroteSoFar + iBlockSize > iOffset + iCount)
					{
						iBlockSize = iOffset + iCount - iWroteSoFar;
					}
					Thread.Sleep(this._iTransmitDelayMS / 2);
					if (this.bIsSecured)
					{
						this._httpsStream.Write(oBytes, iWroteSoFar, iBlockSize);
					}
					else
					{
						this._baseSocket.Send(oBytes, iWroteSoFar, iBlockSize, SocketFlags.None);
					}
					Thread.Sleep(this._iTransmitDelayMS / 2);
				}
				return;
			}
			if (this.bIsSecured)
			{
				this._httpsStream.Write(oBytes, iOffset, iCount);
				return;
			}
			this._baseSocket.Send(oBytes, iOffset, iCount, SocketFlags.None);
		}

		// Token: 0x06000318 RID: 792 RVA: 0x0001D7F8 File Offset: 0x0001B9F8
		internal IAsyncResult BeginSend(byte[] arrData, int iOffset, int iSize, SocketFlags oSF, AsyncCallback oCB, object oContext)
		{
			if (this.bIsSecured)
			{
				return this._httpsStream.BeginWrite(arrData, iOffset, iSize, oCB, oContext);
			}
			return this._baseSocket.BeginSend(arrData, iOffset, iSize, oSF, oCB, oContext);
		}

		// Token: 0x06000319 RID: 793 RVA: 0x0001D836 File Offset: 0x0001BA36
		internal void EndSend(IAsyncResult oAR)
		{
			if (this.bIsSecured)
			{
				this._httpsStream.EndWrite(oAR);
				return;
			}
			this._baseSocket.EndSend(oAR);
		}

		// Token: 0x0600031A RID: 794 RVA: 0x0001D85A File Offset: 0x0001BA5A
		internal IAsyncResult BeginReceive(byte[] arrData, int iOffset, int iSize, SocketFlags oSF, AsyncCallback oCB, object oContext)
		{
			if (this.bIsSecured)
			{
				return this._httpsStream.BeginRead(arrData, iOffset, iSize, oCB, oContext);
			}
			return this._baseSocket.BeginReceive(arrData, iOffset, iSize, oSF, oCB, oContext);
		}

		// Token: 0x0600031B RID: 795 RVA: 0x0001D88B File Offset: 0x0001BA8B
		internal int EndReceive(IAsyncResult oAR)
		{
			if (this.bIsSecured)
			{
				return this._httpsStream.EndRead(oAR);
			}
			return this._baseSocket.EndReceive(oAR);
		}

		/// <summary>
		/// Receive bytes from the pipe into the DATA buffer.
		/// </summary>
		/// <exception cref="T:System.IO.IOException">Throws IO exceptions from the socket/stream</exception>
		/// <param name="arrBuffer">Array of data read</param>
		/// <returns>Bytes read</returns>
		// Token: 0x0600031C RID: 796 RVA: 0x0001D8B0 File Offset: 0x0001BAB0
		internal int Receive(byte[] arrBuffer)
		{
			int cBytes;
			if (this.bIsSecured)
			{
				cBytes = this._httpsStream.Read(arrBuffer, 0, arrBuffer.Length);
			}
			else
			{
				cBytes = this._baseSocket.Receive(arrBuffer);
			}
			return cBytes;
		}

		/// <summary>
		/// Return the raw socket this pipe wraps. Avoid calling this method if at all possible.
		/// </summary>
		/// <returns>The Socket object this Pipe wraps.</returns>
		// Token: 0x0600031D RID: 797 RVA: 0x0001D8E8 File Offset: 0x0001BAE8
		public Socket GetRawSocket()
		{
			return this._baseSocket;
		}

		/// <summary>
		/// Shutdown and close the socket inside this pipe. Eats exceptions.
		/// </summary>
		// Token: 0x0600031E RID: 798 RVA: 0x0001D8F0 File Offset: 0x0001BAF0
		public void End()
		{
			if (CONFIG.bDebugSpew)
			{
				FiddlerApplication.DebugSpew("Pipe::End() for {0}", new object[] { this._sPipeName });
			}
			try
			{
				if (this._httpsStream != null)
				{
					this._httpsStream.Close();
				}
				if (this._baseSocket != null)
				{
					this._baseSocket.Shutdown(SocketShutdown.Both);
					this._baseSocket.Close();
				}
			}
			catch (Exception eX)
			{
			}
			this._baseSocket = null;
			this._httpsStream = null;
		}

		/// <summary>
		/// Abruptly closes the socket by sending a RST packet
		/// </summary>
		// Token: 0x0600031F RID: 799 RVA: 0x0001D974 File Offset: 0x0001BB74
		public void EndWithRST()
		{
			try
			{
				if (this._baseSocket != null)
				{
					this._baseSocket.LingerState = new LingerOption(true, 0);
					this._baseSocket.Close();
				}
			}
			catch (Exception eX)
			{
			}
			this._baseSocket = null;
			this._httpsStream = null;
		}

		/// <summary>
		/// The base socket wrapped in this pipe
		/// </summary>
		// Token: 0x0400016A RID: 362
		protected Socket _baseSocket;

		/// <summary>
		/// The number of times that this Pipe has been used
		/// </summary>
		// Token: 0x0400016B RID: 363
		protected internal uint iUseCount;

		/// <summary>
		/// The HTTPS stream wrapped around the base socket
		/// </summary>
		// Token: 0x0400016C RID: 364
		protected SslStream _httpsStream;

		/// <summary>
		/// The display name of this Pipe
		/// </summary>
		// Token: 0x0400016D RID: 365
		protected internal string _sPipeName;

		/// <summary>
		/// Number of milliseconds to delay each 1024 bytes transmitted
		/// </summary>
		// Token: 0x0400016E RID: 366
		private int _iTransmitDelayMS;
	}
}
