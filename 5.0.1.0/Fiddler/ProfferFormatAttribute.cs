using System;

namespace Fiddler
{
	/// <summary>
	/// Attribute allowing developer to specify that a class supports the specified Import/Export Format.
	/// </summary>
	// Token: 0x02000021 RID: 33
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class ProfferFormatAttribute : Attribute
	{
		// Token: 0x0600018F RID: 399 RVA: 0x00014003 File Offset: 0x00012203
		internal string[] getExtensions()
		{
			if (string.IsNullOrEmpty(this._sExtensions))
			{
				return new string[0];
			}
			return this._sExtensions.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
		}

		/// <summary>
		/// Attribute allowing developer to specify that a class supports the specified Import/Export Format
		/// </summary>
		/// <param name="sFormatName">Shortname of the Format (e.g. WebText XML)</param>
		/// <param name="sDescription">Description of the format</param>
		// Token: 0x06000190 RID: 400 RVA: 0x00014030 File Offset: 0x00012230
		public ProfferFormatAttribute(string sFormatName, string sDescription)
			: this(sFormatName, sDescription, string.Empty)
		{
		}

		/// <summary>
		/// Attribute allowing developer to specify that a class supports the specified Import/Export Format
		/// </summary>
		/// <param name="sFormatName">Shortname of the Format (e.g. WebText XML)</param>
		/// <param name="sDescription">Description of the format</param>
		/// <param name="sExtensions">Semi-colon delimited file extensions (e.g. ".har;.harx")</param>
		// Token: 0x06000191 RID: 401 RVA: 0x0001403F File Offset: 0x0001223F
		public ProfferFormatAttribute(string sFormatName, string sDescription, string sExtensions)
		{
			this._sFormatName = sFormatName;
			this._sFormatDesc = sDescription;
			this._sExtensions = sExtensions;
		}

		/// <summary>
		/// Returns the Shortname for this format
		/// </summary>
		// Token: 0x17000051 RID: 81
		// (get) Token: 0x06000192 RID: 402 RVA: 0x0001405C File Offset: 0x0001225C
		public string FormatName
		{
			get
			{
				return this._sFormatName;
			}
		}

		/// <summary>
		/// Returns the Description of this format
		/// </summary>
		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000193 RID: 403 RVA: 0x00014064 File Offset: 0x00012264
		public string FormatDescription
		{
			get
			{
				return this._sFormatDesc;
			}
		}

		// Token: 0x040000C4 RID: 196
		private string _sFormatName;

		// Token: 0x040000C5 RID: 197
		private string _sFormatDesc;

		// Token: 0x040000C6 RID: 198
		private string _sExtensions;
	}
}
