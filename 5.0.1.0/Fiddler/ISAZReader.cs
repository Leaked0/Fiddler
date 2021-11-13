using System;
using System.IO;

namespace Fiddler
{
	// Token: 0x02000068 RID: 104
	public interface ISAZReader
	{
		// Token: 0x060004BC RID: 1212
		string[] GetRequestFileList();

		// Token: 0x060004BD RID: 1213
		Stream GetFileStream(string sFilename);

		// Token: 0x060004BE RID: 1214
		byte[] GetFileBytes(string sFilename);

		// Token: 0x170000D8 RID: 216
		// (get) Token: 0x060004BF RID: 1215
		string Comment { get; }

		// Token: 0x170000D9 RID: 217
		// (get) Token: 0x060004C0 RID: 1216
		string Filename { get; }

		// Token: 0x170000DA RID: 218
		// (get) Token: 0x060004C1 RID: 1217
		string EncryptionStrength { get; }

		// Token: 0x170000DB RID: 219
		// (get) Token: 0x060004C2 RID: 1218
		string EncryptionMethod { get; }

		// Token: 0x060004C3 RID: 1219
		void Close();
	}
}
