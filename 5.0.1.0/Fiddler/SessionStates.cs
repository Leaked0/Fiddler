using System;

namespace Fiddler
{
	/// <summary>
	/// State of the current session
	/// </summary>
	// Token: 0x02000061 RID: 97
	public enum SessionStates
	{
		/// <summary>
		/// Object created but nothing's happening yet
		/// </summary>
		// Token: 0x04000216 RID: 534
		Created,
		/// <summary>
		/// Thread is reading the HTTP Request
		/// </summary>
		// Token: 0x04000217 RID: 535
		ReadingRequest,
		/// <summary>
		/// AutoTamperRequest pass 1	 (IAutoTamper,  OnBeforeRequest script method)
		/// </summary>
		// Token: 0x04000218 RID: 536
		AutoTamperRequestBefore,
		/// <summary>
		/// User can tamper using Fiddler Inspectors
		/// </summary>
		// Token: 0x04000219 RID: 537
		HandTamperRequest,
		/// <summary>
		/// AutoTamperRequest pass 2	 (Only used by IAutoTamper)
		/// </summary>
		// Token: 0x0400021A RID: 538
		AutoTamperRequestAfter,
		/// <summary>
		/// Thread is sending the Request to the server
		/// </summary>
		// Token: 0x0400021B RID: 539
		SendingRequest,
		/// <summary>
		/// Thread is reading the HTTP Response
		/// </summary>
		// Token: 0x0400021C RID: 540
		ReadingResponse,
		/// <summary>
		/// AutoTamperResponse pass 1 (Only used by IAutoTamper)
		/// </summary>
		// Token: 0x0400021D RID: 541
		AutoTamperResponseBefore,
		/// <summary>
		/// User can tamper using Fiddler Inspectors
		/// </summary>
		// Token: 0x0400021E RID: 542
		HandTamperResponse,
		/// <summary>
		/// AutoTamperResponse pass 2 (Only used by IAutoTamper)
		/// </summary>
		// Token: 0x0400021F RID: 543
		AutoTamperResponseAfter,
		/// <summary>
		/// Sending response to client application
		/// </summary>
		// Token: 0x04000220 RID: 544
		SendingResponse,
		/// <summary>
		/// Session complete
		/// </summary>
		// Token: 0x04000221 RID: 545
		Done,
		/// <summary>
		/// Session was aborted (client didn't want response, fatal error, etc)
		/// </summary>
		// Token: 0x04000222 RID: 546
		Aborted
	}
}
