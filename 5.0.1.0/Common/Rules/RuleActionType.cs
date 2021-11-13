using System;

namespace FiddlerCore.Common.Rules
{
	/// <summary>
	/// The different types of actions supported by the Fiddler rules.
	/// </summary>
	// Token: 0x020000AF RID: 175
	public enum RuleActionType
	{
		// Token: 0x040002ED RID: 749
		MarkSession = 1,
		// Token: 0x040002EE RID: 750
		UpdateRequestHeader,
		// Token: 0x040002EF RID: 751
		UpdateResponseHeader,
		// Token: 0x040002F0 RID: 752
		UpdateRequestBody,
		// Token: 0x040002F1 RID: 753
		UpdateResponseBody,
		// Token: 0x040002F2 RID: 754
		UpdateUrl,
		// Token: 0x040002F3 RID: 755
		UpdateQueryParams,
		// Token: 0x040002F4 RID: 756
		UpdateRequestCookies,
		// Token: 0x040002F5 RID: 757
		UpdateResponseCookies,
		// Token: 0x040002F6 RID: 758
		PredefinedResponse,
		// Token: 0x040002F7 RID: 759
		ManualResponse,
		// Token: 0x040002F8 RID: 760
		ResponseFile,
		// Token: 0x040002F9 RID: 761
		DoNotCapture,
		// Token: 0x040002FA RID: 762
		DelayRequest,
		// Token: 0x040002FB RID: 763
		GracefulClose,
		// Token: 0x040002FC RID: 764
		NonGracefulClose,
		// Token: 0x040002FD RID: 765
		MagicString
	}
}
