using System;

namespace Fiddler
{
	/// <summary>
	/// EventArgs class for the ISessionImporter and ISessionExporter interface callbacks
	/// </summary>
	// Token: 0x02000025 RID: 37
	public class ProgressCallbackEventArgs : EventArgs
	{
		/// <summary>
		/// Set to TRUE to request that Import/Export process be aborted as soon as convenient
		/// </summary>
		// Token: 0x17000054 RID: 84
		// (get) Token: 0x06000199 RID: 409 RVA: 0x000140D4 File Offset: 0x000122D4
		// (set) Token: 0x0600019A RID: 410 RVA: 0x000140DC File Offset: 0x000122DC
		public bool Cancel { get; set; }

		/// <summary>
		/// Progress Callback 
		/// </summary>
		/// <param name="flCompletionRatio">Float indicating completion ratio, 0.0 to 1.0. Set to 0 if unknown.</param>
		/// <param name="sProgressText">Short string describing current operation, progress, etc</param>
		// Token: 0x0600019B RID: 411 RVA: 0x000140E5 File Offset: 0x000122E5
		public ProgressCallbackEventArgs(float flCompletionRatio, string sProgressText)
		{
			this._sProgressText = sProgressText ?? string.Empty;
			this._PercentDone = (int)Math.Truncate((double)(100f * Math.Max(0f, Math.Min(1f, flCompletionRatio))));
		}

		/// <summary>
		/// The string message of the notification
		/// </summary>
		// Token: 0x17000055 RID: 85
		// (get) Token: 0x0600019C RID: 412 RVA: 0x00014125 File Offset: 0x00012325
		public string ProgressText
		{
			get
			{
				return this._sProgressText;
			}
		}

		/// <summary>
		/// The percentage completed
		/// </summary>
		// Token: 0x17000056 RID: 86
		// (get) Token: 0x0600019D RID: 413 RVA: 0x0001412D File Offset: 0x0001232D
		public int PercentComplete
		{
			get
			{
				return this._PercentDone;
			}
		}

		// Token: 0x040000CA RID: 202
		private readonly string _sProgressText;

		// Token: 0x040000CB RID: 203
		private readonly int _PercentDone;
	}
}
