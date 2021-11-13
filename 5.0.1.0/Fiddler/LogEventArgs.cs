using System;

namespace Fiddler
{
	/// <summary>
	/// EventArgs class for the LogEvent handler
	/// </summary>
	// Token: 0x02000048 RID: 72
	public class LogEventArgs : EventArgs
	{
		// Token: 0x060002EA RID: 746 RVA: 0x0001C510 File Offset: 0x0001A710
		internal LogEventArgs(string sMsg)
		{
			this._sMessage = sMsg;
		}

		/// <summary>
		/// The String which has been logged
		/// </summary>
		// Token: 0x17000089 RID: 137
		// (get) Token: 0x060002EB RID: 747 RVA: 0x0001C51F File Offset: 0x0001A71F
		public string LogString
		{
			get
			{
				return this._sMessage;
			}
		}

		// Token: 0x04000147 RID: 327
		private readonly string _sMessage;
	}
}
