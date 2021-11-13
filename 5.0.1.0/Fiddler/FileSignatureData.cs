using System;

namespace Fiddler
{
	// Token: 0x0200000F RID: 15
	internal class FileSignatureData
	{
		// Token: 0x060000DA RID: 218 RVA: 0x0000E872 File Offset: 0x0000CA72
		public FileSignatureData(byte[] magicBytes, int offset, string extension)
		{
			this.MagicBytes = magicBytes;
			this.Offset = offset;
			this.Extension = extension;
		}

		// Token: 0x060000DB RID: 219 RVA: 0x0000E88F File Offset: 0x0000CA8F
		public FileSignatureData(byte[] magicBytes, string extension)
			: this(magicBytes, 0, extension)
		{
		}

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x060000DC RID: 220 RVA: 0x0000E89A File Offset: 0x0000CA9A
		// (set) Token: 0x060000DD RID: 221 RVA: 0x0000E8A2 File Offset: 0x0000CAA2
		public byte[] MagicBytes { get; private set; }

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x060000DE RID: 222 RVA: 0x0000E8AB File Offset: 0x0000CAAB
		// (set) Token: 0x060000DF RID: 223 RVA: 0x0000E8B3 File Offset: 0x0000CAB3
		public int Offset { get; private set; }

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x060000E0 RID: 224 RVA: 0x0000E8BC File Offset: 0x0000CABC
		// (set) Token: 0x060000E1 RID: 225 RVA: 0x0000E8C4 File Offset: 0x0000CAC4
		public string Extension { get; private set; }
	}
}
