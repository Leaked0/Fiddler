using System;

namespace Fiddler
{
	/// <summary>
	/// The policy which describes how this pipe may be reused by a later request. Ordered by least restrictive to most.
	/// </summary>
	// Token: 0x02000054 RID: 84
	public enum PipeReusePolicy
	{
		/// <summary>
		/// The ServerPipe may be freely reused by any subsequent request
		/// </summary>
		// Token: 0x04000183 RID: 387
		NoRestrictions,
		/// <summary>
		/// The ServerPipe may be reused only by a subsequent request from the same client process
		/// </summary>
		// Token: 0x04000184 RID: 388
		MarriedToClientProcess,
		/// <summary>
		/// The ServerPipe may be reused only by a subsequent request from the same client pipe
		/// </summary>
		// Token: 0x04000185 RID: 389
		MarriedToClientPipe,
		/// <summary>
		/// The ServerPipe may not be reused for a subsequent request
		/// </summary>
		// Token: 0x04000186 RID: 390
		NoReuse
	}
}
