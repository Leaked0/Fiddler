using System;
using Fiddler;

namespace FiddlerCore.SazProvider
{
	// Token: 0x020000B2 RID: 178
	internal class SazProvider : ISAZProvider
	{
		// Token: 0x060006AF RID: 1711 RVA: 0x0003716C File Offset: 0x0003536C
		public ISAZWriter CreateSAZ(string sFilename)
		{
			return new SazWriter(this, sFilename);
		}

		// Token: 0x060006B0 RID: 1712 RVA: 0x00037175 File Offset: 0x00035375
		public ISAZReader LoadSAZ(string sFilename)
		{
			return new SazReader(this, sFilename);
		}

		// Token: 0x1700010D RID: 269
		// (get) Token: 0x060006B1 RID: 1713 RVA: 0x0003717E File Offset: 0x0003537E
		public bool BufferLocally
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700010E RID: 270
		// (get) Token: 0x060006B2 RID: 1714 RVA: 0x00037181 File Offset: 0x00035381
		public bool SupportsEncryption
		{
			get
			{
				return true;
			}
		}
	}
}
