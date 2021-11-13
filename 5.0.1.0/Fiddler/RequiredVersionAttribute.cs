using System;

namespace Fiddler
{
	/// <summary>
	/// Attribute used to specify the minimum version of Fiddler compatible with this extension assembly. 
	/// </summary>
	// Token: 0x02000020 RID: 32
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	public sealed class RequiredVersionAttribute : Attribute
	{
		/// <summary>
		/// Attribute used to specify the minimum version of Fiddler compatible with this extension assembly.
		/// </summary>
		/// <param name="sVersion">The minimal version string (e.g. "2.2.8.8")</param>
		// Token: 0x0600018D RID: 397 RVA: 0x00013FCC File Offset: 0x000121CC
		public RequiredVersionAttribute(string sVersion)
		{
			if (sVersion.StartsWith("2."))
			{
				sVersion = "4." + sVersion.Substring(2);
			}
			this._sVersion = sVersion;
		}

		/// <summary>
		/// Getter for the required version string
		/// </summary>
		// Token: 0x17000050 RID: 80
		// (get) Token: 0x0600018E RID: 398 RVA: 0x00013FFB File Offset: 0x000121FB
		public string RequiredVersion
		{
			get
			{
				return this._sVersion;
			}
		}

		// Token: 0x040000C3 RID: 195
		private string _sVersion;
	}
}
