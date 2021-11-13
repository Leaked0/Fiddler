using System;

namespace Fiddler
{
	// Token: 0x0200005D RID: 93
	public enum ContinueTransactionReason : byte
	{
		/// <summary>
		/// Unknown
		/// </summary>
		// Token: 0x040001ED RID: 493
		None,
		/// <summary>
		/// The new Session is needed to respond to an Authentication Challenge
		/// </summary>
		// Token: 0x040001EE RID: 494
		Authenticate,
		/// <summary>
		/// The new Session is needed to follow a Redirection
		/// </summary>
		// Token: 0x040001EF RID: 495
		Redirect,
		/// <summary>
		/// The new Session is needed to generate a CONNECT tunnel
		/// </summary>
		// Token: 0x040001F0 RID: 496
		Tunnel
	}
}
