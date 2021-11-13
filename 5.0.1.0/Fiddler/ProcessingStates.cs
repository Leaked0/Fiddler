using System;

namespace Fiddler
{
	/// <summary>
	/// States for the (future) Session-processing State Machine.
	///
	/// Fun Idea: We can omit irrelevant states from FiddlerCore and thus not have to litter
	/// our state machine itself with a bunch of #if FIDDLERCORE checks...
	/// ... except no, that doesn't work because compiler still cares. Rats.
	///
	/// </summary>
	// Token: 0x02000060 RID: 96
	internal enum ProcessingStates : byte
	{
		// Token: 0x040001F7 RID: 503
		Created,
		// Token: 0x040001F8 RID: 504
		GetRequestStart,
		// Token: 0x040001F9 RID: 505
		GetRequestHeadersEnd,
		// Token: 0x040001FA RID: 506
		PauseForRequestTampering,
		// Token: 0x040001FB RID: 507
		ResumeFromRequestTampering,
		// Token: 0x040001FC RID: 508
		GetRequestEnd,
		// Token: 0x040001FD RID: 509
		RunRequestRulesStart,
		// Token: 0x040001FE RID: 510
		RunRequestRulesEnd,
		// Token: 0x040001FF RID: 511
		DetermineGatewayStart,
		// Token: 0x04000200 RID: 512
		DetermineGatewayEnd,
		// Token: 0x04000201 RID: 513
		DNSStart,
		// Token: 0x04000202 RID: 514
		DNSEnd,
		// Token: 0x04000203 RID: 515
		ConnectStart,
		// Token: 0x04000204 RID: 516
		ConnectEnd,
		// Token: 0x04000205 RID: 517
		HTTPSHandshakeStart,
		// Token: 0x04000206 RID: 518
		HTTPSHandshakeEnd,
		// Token: 0x04000207 RID: 519
		SendRequestStart,
		// Token: 0x04000208 RID: 520
		SendRequestEnd,
		// Token: 0x04000209 RID: 521
		ReadResponseStart,
		// Token: 0x0400020A RID: 522
		GetResponseHeadersEnd,
		// Token: 0x0400020B RID: 523
		ReadResponseEnd,
		// Token: 0x0400020C RID: 524
		RunResponseRulesStart,
		// Token: 0x0400020D RID: 525
		RunResponseRulesEnd,
		// Token: 0x0400020E RID: 526
		PauseForResponseTampering,
		// Token: 0x0400020F RID: 527
		ResumeFromResponseTampering,
		// Token: 0x04000210 RID: 528
		ReturnBufferedResponseStart,
		// Token: 0x04000211 RID: 529
		ReturnBufferedResponseEnd,
		// Token: 0x04000212 RID: 530
		DoAfterSessionEventStart,
		// Token: 0x04000213 RID: 531
		DoAfterSessionEventEnd,
		// Token: 0x04000214 RID: 532
		Finished
	}
}
