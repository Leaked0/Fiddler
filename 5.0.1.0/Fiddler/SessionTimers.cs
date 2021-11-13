using System;
using System.Collections.Generic;
using System.Text;
using FiddlerCore.PlatformExtensions;
using FiddlerCore.PlatformExtensions.API;

namespace Fiddler
{
	/// <summary>
	/// A SessionTimers object holds timing information about a single Session.
	/// </summary>
	// Token: 0x02000063 RID: 99
	public class SessionTimers
	{
		// Token: 0x170000D0 RID: 208
		// (get) Token: 0x0600049C RID: 1180 RVA: 0x0002D9C1 File Offset: 0x0002BBC1
		// (set) Token: 0x0600049D RID: 1181 RVA: 0x0002D9DC File Offset: 0x0002BBDC
		public SessionTimers.NetTimestamps ClientReads
		{
			get
			{
				if (this.tsClientReads == null)
				{
					this.tsClientReads = new SessionTimers.NetTimestamps();
				}
				return this.tsClientReads;
			}
			internal set
			{
				this.tsClientReads = value;
			}
		}

		// Token: 0x170000D1 RID: 209
		// (get) Token: 0x0600049E RID: 1182 RVA: 0x0002D9E5 File Offset: 0x0002BBE5
		// (set) Token: 0x0600049F RID: 1183 RVA: 0x0002DA00 File Offset: 0x0002BC00
		public SessionTimers.NetTimestamps ServerReads
		{
			get
			{
				if (this.tsServerReads == null)
				{
					this.tsServerReads = new SessionTimers.NetTimestamps();
				}
				return this.tsServerReads;
			}
			internal set
			{
				this.tsServerReads = value;
			}
		}

		/// <summary>
		/// The total amount of time spent for the Session in milliseconds. (ClientDoneResponse - ClientBeginRequest).
		/// </summary>
		// Token: 0x170000D2 RID: 210
		// (get) Token: 0x060004A0 RID: 1184 RVA: 0x0002DA0C File Offset: 0x0002BC0C
		public long Duration
		{
			get
			{
				if (this.duration >= 0L || this.ClientDoneResponse == default(DateTime) || this.ClientBeginRequest == default(DateTime))
				{
					return this.duration;
				}
				this.duration = Convert.ToInt64(this.ClientDoneResponse.Subtract(this.ClientBeginRequest).TotalMilliseconds);
				return this.duration;
			}
		}

		// Token: 0x060004A1 RID: 1185 RVA: 0x0002DA80 File Offset: 0x0002BC80
		internal SessionTimers Clone()
		{
			return (SessionTimers)base.MemberwiseClone();
		}

		/// <summary>
		/// Override of ToString shows timer info in a fancy format
		/// </summary>
		/// <returns>Timing information as a string</returns>
		// Token: 0x060004A2 RID: 1186 RVA: 0x0002DA8D File Offset: 0x0002BC8D
		public override string ToString()
		{
			return this.ToString(false);
		}

		/// <summary>
		/// Override of ToString shows timer info in a fancy format
		/// </summary>
		/// <param name="bMultiLine">TRUE if the result can contain linebreaks; false if comma-delimited format preferred</param>
		/// <returns>Timing information as a string</returns>
		// Token: 0x060004A3 RID: 1187 RVA: 0x0002DA98 File Offset: 0x0002BC98
		public string ToString(bool bMultiLine)
		{
			if (bMultiLine)
			{
				return string.Format("ClientConnected:\t{0:HH:mm:ss.fff}\r\nClientBeginRequest:\t{1:HH:mm:ss.fff}\r\nGotRequestHeaders:\t{2:HH:mm:ss.fff}\r\nClientDoneRequest:\t{3:HH:mm:ss.fff}\r\nDetermine Gateway:\t{4,0}ms\r\nDNS Lookup: \t\t{5,0}ms\r\nTCP/IP Connect:\t{6,0}ms\r\nHTTPS Handshake:\t{7,0}ms\r\nServerConnected:\t{8:HH:mm:ss.fff}\r\nFiddlerBeginRequest:\t{9:HH:mm:ss.fff}\r\nServerGotRequest:\t{10:HH:mm:ss.fff}\r\nServerBeginResponse:\t{11:HH:mm:ss.fff}\r\nGotResponseHeaders:\t{12:HH:mm:ss.fff}\r\nServerDoneResponse:\t{13:HH:mm:ss.fff}\r\nClientBeginResponse:\t{14:HH:mm:ss.fff}\r\nClientDoneResponse:\t{15:HH:mm:ss.fff}\r\n\r\n{16}", new object[]
				{
					this.ClientConnected,
					this.ClientBeginRequest,
					this.FiddlerGotRequestHeaders,
					this.ClientDoneRequest,
					this.GatewayDeterminationTime,
					this.DNSTime,
					this.TCPConnectTime,
					this.HTTPSHandshakeTime,
					this.ServerConnected,
					this.FiddlerBeginRequest,
					this.ServerGotRequest,
					this.ServerBeginResponse,
					this.FiddlerGotResponseHeaders,
					this.ServerDoneResponse,
					this.ClientBeginResponse,
					this.ClientDoneResponse,
					(TimeSpan.Zero < this.ClientDoneResponse - this.ClientBeginRequest) ? string.Format("\tOverall Elapsed:\t{0:h\\:mm\\:ss\\.fff}\r\n", this.ClientDoneResponse - this.ClientBeginRequest) : string.Empty
				});
			}
			return string.Format("ClientConnected: {0:HH:mm:ss.fff}, ClientBeginRequest: {1:HH:mm:ss.fff}, GotRequestHeaders: {2:HH:mm:ss.fff}, ClientDoneRequest: {3:HH:mm:ss.fff}, Determine Gateway: {4,0}ms, DNS Lookup: {5,0}ms, TCP/IP Connect: {6,0}ms, HTTPS Handshake: {7,0}ms, ServerConnected: {8:HH:mm:ss.fff},FiddlerBeginRequest: {9:HH:mm:ss.fff}, ServerGotRequest: {10:HH:mm:ss.fff}, ServerBeginResponse: {11:HH:mm:ss.fff}, GotResponseHeaders: {12:HH:mm:ss.fff}, ServerDoneResponse: {13:HH:mm:ss.fff}, ClientBeginResponse: {14:HH:mm:ss.fff}, ClientDoneResponse: {15:HH:mm:ss.fff}{16}", new object[]
			{
				this.ClientConnected,
				this.ClientBeginRequest,
				this.FiddlerGotRequestHeaders,
				this.ClientDoneRequest,
				this.GatewayDeterminationTime,
				this.DNSTime,
				this.TCPConnectTime,
				this.HTTPSHandshakeTime,
				this.ServerConnected,
				this.FiddlerBeginRequest,
				this.ServerGotRequest,
				this.ServerBeginResponse,
				this.FiddlerGotResponseHeaders,
				this.ServerDoneResponse,
				this.ClientBeginResponse,
				this.ClientDoneResponse,
				(TimeSpan.Zero < this.ClientDoneResponse - this.ClientBeginRequest) ? string.Format(", Overall Elapsed: {0:h\\:mm\\:ss\\.fff}", this.ClientDoneResponse - this.ClientBeginRequest) : string.Empty
			});
		}

		/// <summary>
		/// Enables High-Resolution timers, which are bad for battery-life but good for the accuracy of timestamps.
		/// See http://technet.microsoft.com/en-us/sysinternals/bb897568 for the ClockRes utility that shows current clock resolution.
		/// NB: Exiting Fiddler reverts this to the default value.
		/// </summary>
		// Token: 0x170000D3 RID: 211
		// (get) Token: 0x060004A4 RID: 1188 RVA: 0x0002DD2C File Offset: 0x0002BF2C
		// (set) Token: 0x060004A5 RID: 1189 RVA: 0x0002DD38 File Offset: 0x0002BF38
		public static bool EnableHighResolutionTimers
		{
			get
			{
				return SessionTimers.platformExtensions.HighResolutionTimersEnabled;
			}
			set
			{
				SessionTimers.platformExtensions.TryChangeTimersResolution(value);
			}
		}

		// Token: 0x0400023C RID: 572
		private SessionTimers.NetTimestamps tsClientReads;

		// Token: 0x0400023D RID: 573
		private SessionTimers.NetTimestamps tsServerReads;

		/// <summary>
		/// The time at which the client's HTTP connection to Fiddler was established
		/// </summary>
		// Token: 0x0400023E RID: 574
		public DateTime ClientConnected;

		/// <summary>
		/// The time at which the request's first Send() to Fiddler completes
		/// </summary>
		// Token: 0x0400023F RID: 575
		public DateTime ClientBeginRequest;

		/// <summary>
		/// The time at which the request headers were received
		/// </summary>
		// Token: 0x04000240 RID: 576
		public DateTime FiddlerGotRequestHeaders;

		/// <summary>
		/// The time at which the request to Fiddler completes (aka RequestLastWrite)
		/// </summary>
		// Token: 0x04000241 RID: 577
		public DateTime ClientDoneRequest;

		/// <summary>
		/// The time at which the server connection has been established
		/// </summary>
		// Token: 0x04000242 RID: 578
		public DateTime ServerConnected;

		/// <summary>
		/// The time at which Fiddler begins sending the HTTP request to the server (FiddlerRequestFirstSend)
		/// </summary>
		// Token: 0x04000243 RID: 579
		public DateTime FiddlerBeginRequest;

		/// <summary>
		/// The time at which Fiddler has completed sending the HTTP request to the server (FiddlerRequestLastSend).
		/// BUG: Should be named "FiddlerEndRequest". 
		/// NOTE: Value here is often misleading due to buffering inside WinSock's send() call.
		/// </summary>
		// Token: 0x04000244 RID: 580
		public DateTime ServerGotRequest;

		/// <summary>
		/// The time at which Fiddler receives the first byte of the server's response (ServerResponseFirstRead)
		/// </summary>
		// Token: 0x04000245 RID: 581
		public DateTime ServerBeginResponse;

		/// <summary>
		/// The time at which Fiddler received the server's headers
		/// </summary>
		// Token: 0x04000246 RID: 582
		public DateTime FiddlerGotResponseHeaders;

		/// <summary>
		/// The time at which Fiddler has completed receipt of the server's response (ServerResponseLastRead)
		/// </summary>
		// Token: 0x04000247 RID: 583
		public DateTime ServerDoneResponse;

		/// <summary>
		/// The time at which Fiddler has begun sending the Response to the client (ClientResponseFirstSend)
		/// </summary>
		// Token: 0x04000248 RID: 584
		public DateTime ClientBeginResponse;

		/// <summary>
		/// The time at which Fiddler has completed sending the Response to the client (ClientResponseLastSend)
		/// </summary>
		// Token: 0x04000249 RID: 585
		public DateTime ClientDoneResponse;

		// Token: 0x0400024A RID: 586
		private long duration = -1L;

		/// <summary>
		/// The number of milliseconds spent determining which gateway should be used to handle this request
		/// (Should be mutually exclusive to DNSTime!=0)
		/// </summary>
		// Token: 0x0400024B RID: 587
		public int GatewayDeterminationTime;

		/// <summary>
		/// The number of milliseconds spent waiting for DNS
		/// </summary>
		// Token: 0x0400024C RID: 588
		public int DNSTime;

		/// <summary>
		/// The number of milliseconds spent waiting for the server TCP/IP connection establishment
		/// </summary>
		// Token: 0x0400024D RID: 589
		public int TCPConnectTime;

		/// <summary>
		/// The number of milliseconds elapsed while performing the HTTPS handshake with the server
		/// </summary>
		// Token: 0x0400024E RID: 590
		public int HTTPSHandshakeTime;

		// Token: 0x0400024F RID: 591
		private static readonly IPlatformExtensions platformExtensions = PlatformExtensionsFactory.Instance.CreatePlatformExtensions();

		// Token: 0x020000DD RID: 221
		public class NetTimestamps
		{
			/// <summary>
			/// Log a Read's size and timestamp
			/// </summary>
			/// <param name="tsRead">Number of milliseconds since first calling .Read()</param>
			/// <param name="bytesRead">Number of bytes returned in this read</param>
			// Token: 0x06000743 RID: 1859 RVA: 0x0003930A File Offset: 0x0003750A
			public void AddRead(long tsRead, int bytesRead)
			{
				this.listTimeAndSize.Add(new SessionTimers.NetTimestamps.NetTimestamp(tsRead, bytesRead));
			}

			// Token: 0x06000744 RID: 1860 RVA: 0x0003931E File Offset: 0x0003751E
			public SessionTimers.NetTimestamps.NetTimestamp[] ToArray()
			{
				return this.listTimeAndSize.ToArray();
			}

			/// <summary>
			/// Return the ReadTimings as an array. Only one read is counted per millisecond
			/// </summary>
			/// <returns></returns>
			// Token: 0x06000745 RID: 1861 RVA: 0x0003932C File Offset: 0x0003752C
			public SessionTimers.NetTimestamps.NetTimestamp[] ToFoldedArray(int iMSFold)
			{
				List<SessionTimers.NetTimestamps.NetTimestamp> listFolded = new List<SessionTimers.NetTimestamps.NetTimestamp>();
				foreach (SessionTimers.NetTimestamps.NetTimestamp NTS in this.listTimeAndSize)
				{
					if (listFolded.Count < 1 || listFolded[listFolded.Count - 1].tsRead + (long)iMSFold < NTS.tsRead)
					{
						listFolded.Add(NTS);
					}
					int cbTotal = listFolded[listFolded.Count - 1].cbRead;
					cbTotal += NTS.cbRead;
					listFolded.RemoveAt(listFolded.Count - 1);
					listFolded.Add(new SessionTimers.NetTimestamps.NetTimestamp(NTS.tsRead, cbTotal));
				}
				return listFolded.ToArray();
			}

			// Token: 0x17000122 RID: 290
			// (get) Token: 0x06000746 RID: 1862 RVA: 0x000393F0 File Offset: 0x000375F0
			public int Count
			{
				get
				{
					return this.listTimeAndSize.Count;
				}
			}

			// Token: 0x06000747 RID: 1863 RVA: 0x00039400 File Offset: 0x00037600
			public override string ToString()
			{
				StringBuilder sbResult = new StringBuilder();
				sbResult.AppendFormat("There were {0} reads.\n<table>", this.listTimeAndSize.Count);
				foreach (SessionTimers.NetTimestamps.NetTimestamp oNTS in this.listTimeAndSize)
				{
					sbResult.AppendFormat("<tr><td>{0}<td>{1:N0}</td><tr>\n", oNTS.tsRead, oNTS.cbRead);
				}
				sbResult.AppendFormat("</table>", Array.Empty<object>());
				return sbResult.ToString();
			}

			/// <summary>
			/// Create a new List and append to it
			/// </summary>
			/// <param name="oExistingTS"></param>
			/// <returns></returns>
			// Token: 0x06000748 RID: 1864 RVA: 0x000394A8 File Offset: 0x000376A8
			internal static SessionTimers.NetTimestamps FromCopy(SessionTimers.NetTimestamps oExistingTS)
			{
				SessionTimers.NetTimestamps ntsResult = new SessionTimers.NetTimestamps();
				if (oExistingTS != null)
				{
					ntsResult.listTimeAndSize.AddRange(oExistingTS.listTimeAndSize);
				}
				return ntsResult;
			}

			// Token: 0x04000397 RID: 919
			private List<SessionTimers.NetTimestamps.NetTimestamp> listTimeAndSize = new List<SessionTimers.NetTimestamps.NetTimestamp>();

			// Token: 0x020000F9 RID: 249
			public struct NetTimestamp
			{
				// Token: 0x06000780 RID: 1920 RVA: 0x00039CF4 File Offset: 0x00037EF4
				public NetTimestamp(long tsReadMS, int count)
				{
					this.tsRead = tsReadMS;
					this.cbRead = count;
				}

				// Token: 0x040003FB RID: 1019
				public readonly long tsRead;

				// Token: 0x040003FC RID: 1020
				public readonly int cbRead;
			}
		}
	}
}
