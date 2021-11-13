using System;
using System.Collections.Generic;
using System.IO;
using Fiddler;
using Ionic.Zip;

namespace FiddlerCore.SazProvider
{
	// Token: 0x020000B3 RID: 179
	internal class SazReader : ISAZReader2, ISAZReader
	{
		// Token: 0x1700010F RID: 271
		// (get) Token: 0x060006B4 RID: 1716 RVA: 0x0003718C File Offset: 0x0003538C
		public string Filename { get; }

		// Token: 0x060006B5 RID: 1717 RVA: 0x00037194 File Offset: 0x00035394
		public void Close()
		{
			this._oZip.Dispose();
			this._oZip = null;
		}

		// Token: 0x17000110 RID: 272
		// (get) Token: 0x060006B6 RID: 1718 RVA: 0x000371A8 File Offset: 0x000353A8
		public string Comment
		{
			get
			{
				return this._oZip.Comment;
			}
		}

		// Token: 0x17000111 RID: 273
		// (get) Token: 0x060006B7 RID: 1719 RVA: 0x000371B5 File Offset: 0x000353B5
		// (set) Token: 0x060006B8 RID: 1720 RVA: 0x000371BD File Offset: 0x000353BD
		public string EncryptionMethod { get; private set; }

		// Token: 0x17000112 RID: 274
		// (get) Token: 0x060006B9 RID: 1721 RVA: 0x000371C6 File Offset: 0x000353C6
		// (set) Token: 0x060006BA RID: 1722 RVA: 0x000371CE File Offset: 0x000353CE
		public string EncryptionStrength { get; private set; }

		// Token: 0x17000113 RID: 275
		// (get) Token: 0x060006BB RID: 1723 RVA: 0x000371D7 File Offset: 0x000353D7
		// (set) Token: 0x060006BC RID: 1724 RVA: 0x000371DF File Offset: 0x000353DF
		public GetPasswordDelegate PasswordCallback { get; set; }

		// Token: 0x060006BD RID: 1725 RVA: 0x000371E8 File Offset: 0x000353E8
		private bool PromptForPassword(string partName)
		{
			if (this.PasswordCallback == null)
			{
				throw new ArgumentNullException("GetPasswordDelegate not set. Use the Utilities.ReadSessionArchive(string, string, GetPasswordDelegate) overload.");
			}
			this._sPassword = this.PasswordCallback(this.Filename, partName);
			if (!string.IsNullOrEmpty(this._sPassword))
			{
				this._oZip.Password = this._sPassword;
				return true;
			}
			return false;
		}

		// Token: 0x060006BE RID: 1726 RVA: 0x00037244 File Offset: 0x00035444
		public Stream GetFileStream(string sFilename)
		{
			ZipEntry oZE = this._oZip[sFilename];
			if (oZE == null)
			{
				return null;
			}
			if (oZE.UsesEncryption && string.IsNullOrEmpty(this._sPassword))
			{
				this.StoreEncryptionInfo(oZE.Encryption);
				if (!this.PromptForPassword(sFilename))
				{
					throw new OperationCanceledException("Password required.");
				}
			}
			Stream strmResult = null;
			for (;;)
			{
				try
				{
					strmResult = oZE.OpenReader();
				}
				catch (BadPasswordException)
				{
					if (!this.PromptForPassword(sFilename))
					{
						throw new OperationCanceledException("Password required.");
					}
					continue;
				}
				catch (Exception eX)
				{
					if (eX is OperationCanceledException)
					{
						throw;
					}
				}
				break;
			}
			return strmResult;
		}

		// Token: 0x060006BF RID: 1727 RVA: 0x000372E8 File Offset: 0x000354E8
		private void StoreEncryptionInfo(EncryptionAlgorithm oEA)
		{
			switch (oEA)
			{
			case EncryptionAlgorithm.PkzipWeak:
				this.EncryptionMethod = "PKZip";
				this.EncryptionStrength = "56";
				return;
			case EncryptionAlgorithm.WinZipAes128:
				this.EncryptionMethod = "WinZipAes";
				this.EncryptionStrength = "128";
				return;
			case EncryptionAlgorithm.WinZipAes256:
				this.EncryptionMethod = "WinZipAes";
				this.EncryptionStrength = "256";
				return;
			default:
				this.EncryptionMethod = "Unknown";
				this.EncryptionStrength = "0";
				return;
			}
		}

		// Token: 0x060006C0 RID: 1728 RVA: 0x00037368 File Offset: 0x00035568
		public byte[] GetFileBytes(string sFilename)
		{
			Stream strmBytes = this.GetFileStream(sFilename);
			if (strmBytes == null)
			{
				return null;
			}
			byte[] arrData = Utilities.ReadEntireStream(strmBytes);
			strmBytes.Close();
			return arrData;
		}

		// Token: 0x060006C1 RID: 1729 RVA: 0x00037390 File Offset: 0x00035590
		public string[] GetRequestFileList()
		{
			List<string> listFiles = new List<string>();
			foreach (ZipEntry oZE in this._oZip)
			{
				if (oZE.FileName.EndsWith("_c.txt", StringComparison.OrdinalIgnoreCase) && oZE.FileName.StartsWith("raw/", StringComparison.OrdinalIgnoreCase))
				{
					listFiles.Add(oZE.FileName);
				}
			}
			return listFiles.ToArray();
		}

		// Token: 0x060006C2 RID: 1730 RVA: 0x00037414 File Offset: 0x00035614
		internal SazReader(SazProvider provider, string sFilename)
		{
			this.provider = provider;
			this.Filename = sFilename;
			this._oZip = new ZipFile(sFilename);
			foreach (string s in this._oZip.EntryFileNames)
			{
			}
		}

		// Token: 0x04000312 RID: 786
		private ZipFile _oZip;

		// Token: 0x04000313 RID: 787
		private string _sPassword;

		// Token: 0x04000314 RID: 788
		private readonly SazProvider provider;
	}
}
