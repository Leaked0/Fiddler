using System;

namespace Fiddler
{
	/// <summary>
	/// When may requests be resent on a new connection?
	/// </summary>
	// Token: 0x02000013 RID: 19
	public enum RetryMode : byte
	{
		/// <summary>
		/// The request may always be retried.
		/// </summary>
		// Token: 0x04000050 RID: 80
		Always,
		/// <summary>
		/// The request may never be retried
		/// </summary>
		// Token: 0x04000051 RID: 81
		Never,
		/// <summary>
		/// The request may only be resent if the HTTP Method is idempotent.
		/// This SHOULD be the default per HTTP spec, but this appears to break tons of servers.
		/// </summary>
		// Token: 0x04000052 RID: 82
		IdempotentOnly
	}
}
