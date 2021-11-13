using System;
using System.Collections.Generic;

namespace Fiddler
{
	/// <summary>
	/// ISessionImport allows loading of Session data
	/// </summary>
	// Token: 0x02000023 RID: 35
	public interface ISessionImporter : IDisposable
	{
		/// <summary>
		/// Import Sessions from a data source
		/// </summary>
		/// <param name="sImportFormat">Shortname of the format</param>
		/// <param name="dictOptions">Dictionary of options that the Importer class may use</param>
		/// <param name="evtProgressNotifications">Callback event on which progress is reported or the host may cancel</param>
		/// <returns>Array of Session objects imported from source</returns>
		// Token: 0x06000197 RID: 407
		Session[] ImportSessions(string sImportFormat, Dictionary<string, object> dictOptions, EventHandler<ProgressCallbackEventArgs> evtProgressNotifications, bool skipNewSessionEvent);
	}
}
