using System;
using System.IO;
using System.Text;
using Fiddler;
using Ionic.Zip;

namespace FiddlerCore.SazProvider
{
	// Token: 0x020000B4 RID: 180
	internal class SazWriter : ISAZWriter
	{
		// Token: 0x060006C3 RID: 1731 RVA: 0x00037480 File Offset: 0x00035680
		internal SazWriter(SazProvider provider, string sFilename)
		{
			this.provider = provider;
			this.Filename = sFilename;
			this._oZip = new ZipFile(sFilename);
			this._oZip.UseZip64WhenSaving = Zip64Option.AsNecessary;
			this._oZip.AddDirectoryByName("raw");
		}

		// Token: 0x17000114 RID: 276
		// (get) Token: 0x060006C4 RID: 1732 RVA: 0x000374BF File Offset: 0x000356BF
		public string EncryptionMethod
		{
			get
			{
				if (string.IsNullOrEmpty(this._EncryptionMethod))
				{
					this.StoreEncryptionInfo(this._oZip.Encryption);
				}
				return this._EncryptionMethod;
			}
		}

		// Token: 0x060006C5 RID: 1733 RVA: 0x000374E8 File Offset: 0x000356E8
		private void StoreEncryptionInfo(EncryptionAlgorithm oEA)
		{
			switch (oEA)
			{
			case EncryptionAlgorithm.PkzipWeak:
				this._EncryptionMethod = "PKZip";
				this._EncryptionStrength = "56";
				return;
			case EncryptionAlgorithm.WinZipAes128:
				this._EncryptionMethod = "WinZipAes";
				this._EncryptionStrength = "128";
				return;
			case EncryptionAlgorithm.WinZipAes256:
				this._EncryptionMethod = "WinZipAes";
				this._EncryptionStrength = "256";
				return;
			default:
				this._EncryptionMethod = "Unknown";
				this._EncryptionStrength = "0";
				return;
			}
		}

		// Token: 0x17000115 RID: 277
		// (get) Token: 0x060006C6 RID: 1734 RVA: 0x00037566 File Offset: 0x00035766
		public string EncryptionStrength
		{
			get
			{
				if (string.IsNullOrEmpty(this._EncryptionStrength))
				{
					this.StoreEncryptionInfo(this._oZip.Encryption);
				}
				return this._EncryptionStrength;
			}
		}

		// Token: 0x060006C7 RID: 1735 RVA: 0x0003758C File Offset: 0x0003578C
		public void AddFile(string sFilename, SAZWriterDelegate oSWD)
		{
			WriteDelegate oWD = delegate(string sFN, Stream oS)
			{
				oSWD(oS);
			};
			this._oZip.AddEntry(sFilename, oWD);
		}

		/// <summary>
		/// Writes the ContentTypes XML to the ZIP so Packaging APIs can read it.
		/// See http://en.wikipedia.org/wiki/Open_Packaging_Conventions
		/// </summary>
		/// <param name="odfZip"></param>
		// Token: 0x060006C8 RID: 1736 RVA: 0x000375C1 File Offset: 0x000357C1
		private void WriteODCXML()
		{
			this._oZip.AddEntry("[Content_Types].xml", delegate(string sn, Stream strmToWrite)
			{
				byte[] arrODCXML = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">\r\n<Default Extension=\"htm\" ContentType=\"text/html\" />\r\n<Default Extension=\"xml\" ContentType=\"application/xml\" />\r\n<Default Extension=\"txt\" ContentType=\"text/plain\" />\r\n</Types>");
				strmToWrite.Write(arrODCXML, 0, arrODCXML.Length);
			});
		}

		// Token: 0x060006C9 RID: 1737 RVA: 0x000375F3 File Offset: 0x000357F3
		public bool CompleteArchive()
		{
			this.WriteODCXML();
			this._oZip.Save();
			this._oZip.Dispose();
			this._oZip = null;
			return true;
		}

		// Token: 0x17000116 RID: 278
		// (get) Token: 0x060006CA RID: 1738 RVA: 0x00037619 File Offset: 0x00035819
		public string Filename { get; }

		// Token: 0x17000117 RID: 279
		// (get) Token: 0x060006CB RID: 1739 RVA: 0x00037621 File Offset: 0x00035821
		// (set) Token: 0x060006CC RID: 1740 RVA: 0x0003762E File Offset: 0x0003582E
		public string Comment
		{
			get
			{
				return this._oZip.Comment;
			}
			set
			{
				this._oZip.Comment = value;
			}
		}

		// Token: 0x060006CD RID: 1741 RVA: 0x0003763C File Offset: 0x0003583C
		public bool SetPassword(string sPassword)
		{
			if (!string.IsNullOrEmpty(sPassword))
			{
				if (CONFIG.bUseAESForSAZ)
				{
					if (FiddlerApplication.Prefs.GetBoolPref("fiddler.saz.AES.Use256Bit", false))
					{
						this._oZip.Encryption = EncryptionAlgorithm.WinZipAes256;
					}
					else
					{
						this._oZip.Encryption = EncryptionAlgorithm.WinZipAes128;
					}
				}
				this._oZip.Password = sPassword;
			}
			return true;
		}

		// Token: 0x04000319 RID: 793
		private ZipFile _oZip;

		// Token: 0x0400031A RID: 794
		private string _EncryptionMethod;

		// Token: 0x0400031B RID: 795
		private string _EncryptionStrength;

		// Token: 0x0400031C RID: 796
		private readonly SazProvider provider;
	}
}
