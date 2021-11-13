using System;

namespace Fiddler
{
	/// <summary>
	/// EventArgs for preference-change events.  See http://msdn.microsoft.com/en-us/library/ms229011.aspx.
	/// </summary>
	// Token: 0x0200002E RID: 46
	public class PrefChangeEventArgs : EventArgs
	{
		// Token: 0x060001CB RID: 459 RVA: 0x000149BF File Offset: 0x00012BBF
		internal PrefChangeEventArgs(string prefName, string prefValueString)
		{
			this._prefName = prefName;
			this._prefValueString = prefValueString;
		}

		/// <summary>
		/// The name of the preference being added, changed, or removed
		/// </summary>
		// Token: 0x1700005B RID: 91
		// (get) Token: 0x060001CC RID: 460 RVA: 0x000149D5 File Offset: 0x00012BD5
		public string PrefName
		{
			get
			{
				return this._prefName;
			}
		}

		/// <summary>
		/// The string value of the preference, or null if the preference is being removed
		/// </summary>
		// Token: 0x1700005C RID: 92
		// (get) Token: 0x060001CD RID: 461 RVA: 0x000149DD File Offset: 0x00012BDD
		public string ValueString
		{
			get
			{
				return this._prefValueString;
			}
		}

		/// <summary>
		/// Returns TRUE if ValueString=="true", case-insensitively
		/// </summary>
		// Token: 0x1700005D RID: 93
		// (get) Token: 0x060001CE RID: 462 RVA: 0x000149E5 File Offset: 0x00012BE5
		public bool ValueBool
		{
			get
			{
				return "True".OICEquals(this._prefValueString);
			}
		}

		// Token: 0x040000CF RID: 207
		private readonly string _prefName;

		// Token: 0x040000D0 RID: 208
		private readonly string _prefValueString;
	}
}
