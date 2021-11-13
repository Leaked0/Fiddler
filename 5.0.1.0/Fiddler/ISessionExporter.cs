using System;
using System.Collections.Generic;

namespace Fiddler
{
	/// <summary>
	/// ISessionExport allows saving of Session data
	/// </summary>
	// Token: 0x02000024 RID: 36
	public interface ISessionExporter : IDisposable
	{
		/// <summary>
		/// Export Sessions to a data store
		/// </summary>
		/// <param name="sExportFormat">Shortname of the format</param>
		/// <param name="oSessions">Array of Sessions being exported</param>
		/// <param name="dictOptions">Dictionary of options that the Exporter class may use</param>
		/// <param name="evtProgressNotifications">Callback event on which progress is reported or the host may cancel</param>
		/// <returns>TRUE if the export was successful</returns>
		// Token: 0x06000198 RID: 408
		bool ExportSessions(string sExportFormat, Session[] oSessions, Dictionary<string, object> dictOptions, EventHandler<ProgressCallbackEventArgs> evtProgressNotifications);
	}
}
