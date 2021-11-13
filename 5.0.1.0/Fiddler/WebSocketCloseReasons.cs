using System;

namespace Fiddler
{
	// Token: 0x02000073 RID: 115
	public enum WebSocketCloseReasons : short
	{
		// Token: 0x04000290 RID: 656
		Normal = 1000,
		// Token: 0x04000291 RID: 657
		GoingAway,
		// Token: 0x04000292 RID: 658
		ProtocolError,
		// Token: 0x04000293 RID: 659
		UnsupportedData,
		// Token: 0x04000294 RID: 660
		Undefined1004,
		// Token: 0x04000295 RID: 661
		Reserved1005,
		// Token: 0x04000296 RID: 662
		Reserved1006,
		// Token: 0x04000297 RID: 663
		InvalidPayloadData,
		// Token: 0x04000298 RID: 664
		PolicyViolation,
		// Token: 0x04000299 RID: 665
		MessageTooBig,
		// Token: 0x0400029A RID: 666
		MandatoryExtension,
		// Token: 0x0400029B RID: 667
		InternalServerError,
		// Token: 0x0400029C RID: 668
		Reserved1015 = 1015
	}
}
