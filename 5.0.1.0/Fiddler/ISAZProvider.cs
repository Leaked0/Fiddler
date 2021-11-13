using System;

namespace Fiddler
{
	// Token: 0x0200006A RID: 106
	public interface ISAZProvider
	{
		// Token: 0x060004C6 RID: 1222
		ISAZReader LoadSAZ(string sFilename);

		// Token: 0x060004C7 RID: 1223
		ISAZWriter CreateSAZ(string sFilename);

		// Token: 0x170000DD RID: 221
		// (get) Token: 0x060004C8 RID: 1224
		bool SupportsEncryption { get; }

		// Token: 0x170000DE RID: 222
		// (get) Token: 0x060004C9 RID: 1225
		bool BufferLocally { get; }
	}
}
