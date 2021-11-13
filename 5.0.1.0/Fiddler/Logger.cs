using System;

namespace Fiddler
{
	/// <summary>
	/// The Logger object is a simple event log message dispatcher
	/// </summary>
	// Token: 0x02000047 RID: 71
	public class Logger
	{
		/// <summary>
		/// The Event to raise when a string is logged
		/// </summary>
		// Token: 0x14000010 RID: 16
		// (add) Token: 0x060002E5 RID: 741 RVA: 0x0001C454 File Offset: 0x0001A654
		// (remove) Token: 0x060002E6 RID: 742 RVA: 0x0001C48C File Offset: 0x0001A68C
		public event EventHandler<LogEventArgs> OnLogString;

		/// <summary>
		/// Log a string with specified string formatting
		/// </summary>
		/// <param name="format">The format string</param>
		/// <param name="args">The arguments to replace in the string</param>
		// Token: 0x060002E7 RID: 743 RVA: 0x0001C4C1 File Offset: 0x0001A6C1
		public void LogFormat(string format, params object[] args)
		{
			this.LogString(string.Format(format, args));
		}

		/// <summary>
		/// Log a string
		/// </summary>
		/// <param name="sMsg">The string to log</param>
		// Token: 0x060002E8 RID: 744 RVA: 0x0001C4D0 File Offset: 0x0001A6D0
		public void LogString(string sMsg)
		{
			if (string.IsNullOrEmpty(sMsg))
			{
				return;
			}
			FiddlerApplication.DebugSpew(sMsg);
			if (this.OnLogString != null)
			{
				LogEventArgs olsEA = new LogEventArgs(sMsg);
				this.OnLogString(this, olsEA);
			}
		}
	}
}
