using System;

namespace Fiddler
{
	/// <summary>
	/// This tuple maps a display descriptive string to a Import/Export type.
	/// (The parent dictionary contains the shortname string)
	/// </summary>
	// Token: 0x02000022 RID: 34
	public class TranscoderTuple
	{
		/// <summary>
		/// Create a new Transcoder Tuple
		/// </summary>
		/// <param name="pFA">Proffer format description</param>
		/// <param name="oFormatter">Type implementing this format</param>
		// Token: 0x06000194 RID: 404 RVA: 0x0001406C File Offset: 0x0001226C
		internal TranscoderTuple(ProfferFormatAttribute pFA, Type oFormatter)
		{
			this._pfa = pFA;
			this.sFormatDescription = pFA.FormatDescription;
			this.typeFormatter = oFormatter;
		}

		// Token: 0x06000195 RID: 405 RVA: 0x00014090 File Offset: 0x00012290
		internal bool HandlesExtension(string sExt)
		{
			foreach (string sCandidate in this._pfa.getExtensions())
			{
				if (sExt.OICEquals(sCandidate))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x06000196 RID: 406 RVA: 0x000140C7 File Offset: 0x000122C7
		public string sFormatName
		{
			get
			{
				return this._pfa.FormatName;
			}
		}

		/// <summary>
		/// Textual description of the Format
		/// </summary>
		// Token: 0x040000C7 RID: 199
		public string sFormatDescription;

		/// <summary>
		/// Class implementing the format
		/// </summary>
		// Token: 0x040000C8 RID: 200
		public Type typeFormatter;

		/// <summary>
		/// All metadata about the provider
		/// </summary>
		// Token: 0x040000C9 RID: 201
		private ProfferFormatAttribute _pfa;
	}
}
