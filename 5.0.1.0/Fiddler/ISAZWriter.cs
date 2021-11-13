using System;

namespace Fiddler
{
	// Token: 0x02000067 RID: 103
	public interface ISAZWriter
	{
		// Token: 0x060004B5 RID: 1205
		void AddFile(string sFilename, SAZWriterDelegate oSWD);

		// Token: 0x170000D4 RID: 212
		// (set) Token: 0x060004B6 RID: 1206
		string Comment { set; }

		// Token: 0x170000D5 RID: 213
		// (get) Token: 0x060004B7 RID: 1207
		string Filename { get; }

		// Token: 0x060004B8 RID: 1208
		bool SetPassword(string sPassword);

		// Token: 0x170000D6 RID: 214
		// (get) Token: 0x060004B9 RID: 1209
		string EncryptionStrength { get; }

		// Token: 0x170000D7 RID: 215
		// (get) Token: 0x060004BA RID: 1210
		string EncryptionMethod { get; }

		// Token: 0x060004BB RID: 1211
		bool CompleteArchive();
	}
}
