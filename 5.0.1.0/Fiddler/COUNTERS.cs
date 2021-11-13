using System;

namespace Fiddler
{
	// Token: 0x02000035 RID: 53
	internal static class COUNTERS
	{
		// Token: 0x060001F9 RID: 505 RVA: 0x00015188 File Offset: 0x00013388
		public static string Summarize()
		{
			return string.Format("-= Counters =-\nDNS Lookups underway:\t{0:N0}\nTotal DNS Async:\t{1:N0}\nAsync DNS saved(ms):\t{2:N0}\nDNS Cache Hits:\t\t{3:N0}\n\nAwaiting Client Reuse:\t{4:N0}\nTotal Client Reuse:\t{5:N0}\n\nConnections Accepted:\t{6:N0}\nAccept delay ms:\t{7:N0}\n", new object[]
			{
				COUNTERS.ASYNC_DNS,
				COUNTERS.TOTAL_ASYNC_DNS,
				COUNTERS.TOTAL_ASYNC_DNS_MS,
				COUNTERS.DNSCACHE_HITS,
				COUNTERS.ASYNC_WAIT_CLIENT_REUSE,
				COUNTERS.TOTAL_ASYNC_WAIT_CLIENT_REUSE,
				COUNTERS.CONNECTIONS_ACCEPTED,
				COUNTERS.TOTAL_DELAY_ACCEPT_CONNECTION
			});
		}

		// Token: 0x040000D9 RID: 217
		internal static int ASYNC_DNS;

		// Token: 0x040000DA RID: 218
		internal static long TOTAL_ASYNC_DNS;

		// Token: 0x040000DB RID: 219
		internal static long TOTAL_ASYNC_DNS_MS;

		// Token: 0x040000DC RID: 220
		internal static int DNSCACHE_HITS;

		// Token: 0x040000DD RID: 221
		internal static int ASYNC_WAIT_CLIENT_REUSE;

		// Token: 0x040000DE RID: 222
		internal static long TOTAL_ASYNC_WAIT_CLIENT_REUSE;

		// Token: 0x040000DF RID: 223
		internal static long TOTAL_DELAY_ACCEPT_CONNECTION;

		// Token: 0x040000E0 RID: 224
		internal static long CONNECTIONS_ACCEPTED;
	}
}
