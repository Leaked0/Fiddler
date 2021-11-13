using System;
using System.Collections.Generic;
using System.Linq;

namespace Fiddler
{
	// Token: 0x02000010 RID: 16
	internal class MimeSniffer
	{
		// Token: 0x060000E2 RID: 226 RVA: 0x0000E8CD File Offset: 0x0000CACD
		private MimeSniffer()
		{
			this.InitializeSignatures();
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060000E3 RID: 227 RVA: 0x0000E8DB File Offset: 0x0000CADB
		public static MimeSniffer Instance
		{
			get
			{
				if (MimeSniffer.instance == null)
				{
					MimeSniffer.instance = new MimeSniffer();
				}
				return MimeSniffer.instance;
			}
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x0000E8F4 File Offset: 0x0000CAF4
		public bool TrySniff(byte[] responseBodyBytes, out string sniffedExtension)
		{
			sniffedExtension = null;
			if (Utilities.IsNullOrEmpty(responseBodyBytes))
			{
				return false;
			}
			foreach (FileSignatureData signature in this.signatures)
			{
				if (Utilities.HasMagicBytes(responseBodyBytes, signature.Offset, signature.MagicBytes))
				{
					sniffedExtension = signature.Extension;
					return true;
				}
			}
			return false;
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x0000E970 File Offset: 0x0000CB70
		private void InitializeSignatures()
		{
			List<FileSignatureData> unsortedSignatures = new List<FileSignatureData>
			{
				new FileSignatureData(new byte[] { 80, 75 }, ".zip"),
				new FileSignatureData(new byte[] { 77, 90 }, ".exe"),
				new FileSignatureData(new byte[] { 55, 122 }, ".7z"),
				new FileSignatureData(new byte[] { 82, 97, 114, 33 }, ".rar"),
				new FileSignatureData(new byte[] { 37, 80, 68, 70, 45 }, ".pdf"),
				new FileSignatureData(new byte[] { 66, 77 }, ".bmp")
			};
			this.signatures = (from s in unsortedSignatures
				orderby s.MagicBytes.Length descending
				select s).ToList<FileSignatureData>();
		}

		// Token: 0x04000043 RID: 67
		private static MimeSniffer instance;

		// Token: 0x04000044 RID: 68
		private List<FileSignatureData> signatures;
	}
}
