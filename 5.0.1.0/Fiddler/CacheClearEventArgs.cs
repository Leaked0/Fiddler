using System;
using System.ComponentModel;

namespace Fiddler
{
	/// <summary>
	/// These EventArgs are constructed when FiddlerApplication.OnClearCache is called.
	/// </summary>
	// Token: 0x0200003A RID: 58
	public class CacheClearEventArgs : CancelEventArgs
	{
		/// <summary>
		/// True if the user wants cache files to be cleared
		/// </summary>
		// Token: 0x1700006F RID: 111
		// (get) Token: 0x06000254 RID: 596 RVA: 0x0001645D File Offset: 0x0001465D
		// (set) Token: 0x06000255 RID: 597 RVA: 0x00016465 File Offset: 0x00014665
		public bool ClearCacheFiles { get; set; }

		/// <summary>
		/// True if the user wants cookies to be cleared
		/// </summary>
		// Token: 0x17000070 RID: 112
		// (get) Token: 0x06000256 RID: 598 RVA: 0x0001646E File Offset: 0x0001466E
		// (set) Token: 0x06000257 RID: 599 RVA: 0x00016476 File Offset: 0x00014676
		public bool ClearCookies { get; set; }

		/// <summary>
		/// Constructs the Event Args
		/// </summary>
		/// <param name="bClearFiles">Should Cache Files be cleared?</param>
		/// <param name="bClearCookies">Should Cookies be cleared?</param>
		// Token: 0x06000258 RID: 600 RVA: 0x0001647F File Offset: 0x0001467F
		public CacheClearEventArgs(bool bClearFiles, bool bClearCookies)
		{
			this.ClearCacheFiles = bClearFiles;
			this.ClearCookies = bClearCookies;
		}
	}
}
