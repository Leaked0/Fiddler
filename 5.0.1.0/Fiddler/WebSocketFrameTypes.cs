using System;

namespace Fiddler
{
	// Token: 0x02000072 RID: 114
	public enum WebSocketFrameTypes : byte
	{
		// Token: 0x0400027F RID: 639
		Continuation,
		// Token: 0x04000280 RID: 640
		Text,
		// Token: 0x04000281 RID: 641
		Binary,
		// Token: 0x04000282 RID: 642
		Reservedx3,
		// Token: 0x04000283 RID: 643
		Reservedx4,
		// Token: 0x04000284 RID: 644
		Reservedx5,
		// Token: 0x04000285 RID: 645
		Reservedx6,
		// Token: 0x04000286 RID: 646
		Reservedx7,
		// Token: 0x04000287 RID: 647
		Close,
		// Token: 0x04000288 RID: 648
		Ping,
		// Token: 0x04000289 RID: 649
		Pong,
		// Token: 0x0400028A RID: 650
		ReservedxB,
		// Token: 0x0400028B RID: 651
		ReservedxC,
		// Token: 0x0400028C RID: 652
		ReservedxD,
		// Token: 0x0400028D RID: 653
		ReservedxE,
		// Token: 0x0400028E RID: 654
		ReservedxF
	}
}
