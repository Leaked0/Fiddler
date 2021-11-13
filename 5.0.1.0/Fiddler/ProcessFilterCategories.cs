using System;

namespace Fiddler
{
	/// <summary>
	/// A simple Process Type enumeration used by various filtering features
	/// </summary>
	// Token: 0x02000012 RID: 18
	public enum ProcessFilterCategories
	{
		/// <summary>
		/// Include all Processes
		/// </summary>
		// Token: 0x0400004B RID: 75
		All,
		/// <summary>
		/// Processes which appear to be Web Browsers
		/// </summary>
		// Token: 0x0400004C RID: 76
		Browsers,
		/// <summary>
		/// Processes which appear to NOT be Web Browsers
		/// </summary>
		// Token: 0x0400004D RID: 77
		NonBrowsers,
		/// <summary>
		/// Include only traffic where Process ID isn't known (e.g. remote clients)
		/// </summary>
		// Token: 0x0400004E RID: 78
		HideAll
	}
}
