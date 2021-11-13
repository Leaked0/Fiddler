using System;
using System.IO;
using System.Text;

namespace Fiddler
{
	/// <summary>
	/// A Responder rule maps a request to a response file or action
	/// </summary>
	// Token: 0x02000007 RID: 7
	public class ResponderRule
	{
		/// <summary>
		/// Comment for the rule. Also used as the rule name in Fiddler Everywhere.
		/// </summary>
		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000062 RID: 98 RVA: 0x00003C92 File Offset: 0x00001E92
		// (set) Token: 0x06000063 RID: 99 RVA: 0x00003C9A File Offset: 0x00001E9A
		public string sComment { get; set; }

		/// <summary>
		/// Should this rule be disabled after the first time it's used?
		/// (Useful for AJAX site playback scenarios)
		/// </summary>
		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000064 RID: 100 RVA: 0x00003CA3 File Offset: 0x00001EA3
		// (set) Token: 0x06000065 RID: 101 RVA: 0x00003CAB File Offset: 0x00001EAB
		public bool bDisableOnMatch
		{
			get
			{
				return this._bDisableOnMatch;
			}
			set
			{
				this._bDisableOnMatch = value;
			}
		}

		/// <summary>
		/// Replaces any characters in a filename that are unsafe with safe equivalents
		/// </summary>
		/// <param name="sFilename"></param>
		/// <returns></returns>
		// Token: 0x06000066 RID: 102 RVA: 0x00003CB4 File Offset: 0x00001EB4
		private static string _MakeSafeFilename(string sFilename)
		{
			char[] arrCharUnsafe = Path.GetInvalidFileNameChars();
			if (sFilename.IndexOfAny(arrCharUnsafe) < 0)
			{
				return Utilities.TrimTo(sFilename, 160);
			}
			StringBuilder sbFilename = new StringBuilder(sFilename);
			for (int x = 0; x < sbFilename.Length; x++)
			{
				if (Array.IndexOf<char>(arrCharUnsafe, sFilename[x]) > -1)
				{
					sbFilename[x] = '-';
				}
			}
			return Utilities.TrimTo(sbFilename.ToString(), 160);
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00003D20 File Offset: 0x00001F20
		internal void EnsureRuleIsFileBacked()
		{
			if (!this.HasImportedResponse)
			{
				return;
			}
			string sFilename = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + Path.DirectorySeparatorChar.ToString() + ResponderRule._MakeSafeFilename(this._sAction);
			string sMIME = Utilities.TrimAfter(this._oResponseHeaders["Content-Type"], ";");
			string sExt = Utilities.FileExtensionForMIMEType(sMIME);
			sFilename += sExt;
			FileStream oFS = File.Create(sFilename);
			bool bIncludeHeaders = true;
			if (this._oResponseHeaders.HTTPResponseCode == 200)
			{
				sMIME = this._oResponseHeaders["Content-Type"];
				if (sMIME.OICStartsWith("image/"))
				{
					bIncludeHeaders = false;
				}
			}
			if (bIncludeHeaders)
			{
				byte[] arrHeaders = this._oResponseHeaders.ToByteArray(true, true);
				oFS.Write(arrHeaders, 0, arrHeaders.Length);
			}
			if (this._arrResponseBodyBytes != null)
			{
				oFS.Write(this._arrResponseBodyBytes, 0, this._arrResponseBodyBytes.Length);
			}
			oFS.Close();
			this._oResponseHeaders = null;
			this._arrResponseBodyBytes = null;
			this._sAction = sFilename;
		}

		/// <summary>
		/// Number of milliseconds of latency before returning the response
		/// </summary>
		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000068 RID: 104 RVA: 0x00003E16 File Offset: 0x00002016
		// (set) Token: 0x06000069 RID: 105 RVA: 0x00003E1E File Offset: 0x0000201E
		public int iLatency
		{
			get
			{
				return this._MSLatency;
			}
			set
			{
				if (value > 0)
				{
					this._MSLatency = value;
					return;
				}
				this._MSLatency = 0;
			}
		}

		/// <summary>
		/// The partial-URI to which candidate requests will be matched
		/// </summary>
		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600006A RID: 106 RVA: 0x00003E33 File Offset: 0x00002033
		// (set) Token: 0x0600006B RID: 107 RVA: 0x00003E3B File Offset: 0x0000203B
		public string sMatch
		{
			get
			{
				return this._sMatch;
			}
			internal set
			{
				if (value == null || value.Trim().Length < 1)
				{
					this._sMatch = "*";
					return;
				}
				this._sMatch = value.Trim();
			}
		}

		/// <summary>
		/// The action (response file) to send in the event of a match
		/// </summary>
		// Token: 0x17000015 RID: 21
		// (get) Token: 0x0600006C RID: 108 RVA: 0x00003E66 File Offset: 0x00002066
		// (set) Token: 0x0600006D RID: 109 RVA: 0x00003E6E File Offset: 0x0000206E
		public string sAction
		{
			get
			{
				return this._sAction;
			}
			internal set
			{
				if (string.IsNullOrEmpty(value))
				{
					this._sAction = string.Empty;
					return;
				}
				this._sAction = value.Trim().Trim(new char[] { '"' });
			}
		}

		/// <summary>
		/// Create a responder rule which returns the response from a prior session  
		/// </summary>
		/// <param name="strMatch">The string to match</param>
		/// <param name="oResponseHeaders">A collection of response headers</param>
		/// <param name="arrResponseBytes">The body of the Rule response</param>
		/// <param name="strDescription">The textual description of this Rule</param>
		/// <param name="strComment">Comment for the Rule</param>
		/// <param name="iLatencyMS">Milliseconds of delay before returning the body</param>
		/// <param name="bEnabled">TRUE if this rule should be active</param>
		// Token: 0x0600006E RID: 110 RVA: 0x00003EA0 File Offset: 0x000020A0
		internal ResponderRule(string strMatch, HTTPResponseHeaders oResponseHeaders, byte[] arrResponseBytes, string strDescription, string strComment, int iLatencyMS, bool bEnabled)
		{
			this.sMatch = strMatch;
			this.sAction = strDescription;
			this.sComment = strComment;
			this.iLatency = iLatencyMS;
			this._oResponseHeaders = oResponseHeaders;
			this._arrResponseBodyBytes = arrResponseBytes;
			if (this._oResponseHeaders != null && this._arrResponseBodyBytes == null)
			{
				this._arrResponseBodyBytes = Utilities.emptyByteArray;
			}
			this._bEnabled = bEnabled;
		}

		/// <summary>
		/// Returns true if this ResponderRule returns an imported response
		/// </summary>
		// Token: 0x17000016 RID: 22
		// (get) Token: 0x0600006F RID: 111 RVA: 0x00003F0A File Offset: 0x0000210A
		internal bool HasImportedResponse
		{
			get
			{
				return this._oResponseHeaders != null;
			}
		}

		/// <summary>
		/// Returns true if this Rule is enabled.
		/// </summary>
		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000070 RID: 112 RVA: 0x00003F15 File Offset: 0x00002115
		// (set) Token: 0x06000071 RID: 113 RVA: 0x00003F1D File Offset: 0x0000211D
		internal bool IsEnabled
		{
			get
			{
				return this._bEnabled;
			}
			set
			{
				this._bEnabled = value;
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000072 RID: 114 RVA: 0x00003F26 File Offset: 0x00002126
		// (set) Token: 0x06000073 RID: 115 RVA: 0x00003F2E File Offset: 0x0000212E
		public ResponderGroup Group { get; set; }

		// Token: 0x0400001B RID: 27
		private string _sMatch;

		// Token: 0x0400001C RID: 28
		private bool _bEnabled = true;

		// Token: 0x0400001D RID: 29
		private string _sAction;

		// Token: 0x0400001E RID: 30
		private int _MSLatency;

		// Token: 0x0400001F RID: 31
		private bool _bDisableOnMatch;

		// Token: 0x04000020 RID: 32
		internal byte[] _arrResponseBodyBytes;

		// Token: 0x04000021 RID: 33
		internal HTTPResponseHeaders _oResponseHeaders;
	}
}
