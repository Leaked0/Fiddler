using System;
using System.Collections.Generic;
using System.Diagnostics;
using FiddlerCore.PlatformExtensions;
using FiddlerCore.PlatformExtensions.API;

namespace Fiddler
{
	/// <summary>
	/// This class allows fast-lookup of a ProcessName from a ProcessID.
	/// </summary>
	// Token: 0x02000056 RID: 86
	internal static class ProcessHelper
	{
		/// <summary>
		/// Static constructor which registers for cleanup
		/// </summary>
		// Token: 0x0600035E RID: 862 RVA: 0x0001F8B8 File Offset: 0x0001DAB8
		static ProcessHelper()
		{
			FiddlerApplication.Janitor.assignWork(new SimpleEventHandler(ProcessHelper.ScavengeCache), 60000U);
			if (Utilities.IsWin8OrLater() && FiddlerApplication.Prefs.GetBoolPref("fiddler.ProcessInfo.DecorateWithAppName", true))
			{
				ProcessHelper.bDisambiguateWWAHostApps = true;
			}
		}

		/// <summary>
		/// Prune the cache of expiring PIDs
		/// </summary>
		// Token: 0x0600035F RID: 863 RVA: 0x0001F910 File Offset: 0x0001DB10
		internal static void ScavengeCache()
		{
			Dictionary<int, ProcessHelper.ProcessNameCacheEntry> obj = ProcessHelper.dictProcessNames;
			lock (obj)
			{
				List<int> oExpiringPIDs = new List<int>();
				foreach (KeyValuePair<int, ProcessHelper.ProcessNameCacheEntry> oEntry in ProcessHelper.dictProcessNames)
				{
					if (oEntry.Value.ulLastLookup < Utilities.GetTickCount() - 30000UL)
					{
						oExpiringPIDs.Add(oEntry.Key);
					}
				}
				foreach (int iKey in oExpiringPIDs)
				{
					ProcessHelper.dictProcessNames.Remove(iKey);
				}
			}
		}

		/// <summary>
		/// Map a Process ID (PID) to a Process Name
		/// </summary>
		/// <param name="iPID">The PID</param>
		/// <returns>A Process Name (e.g. IEXPLORE.EXE) or String.Empty</returns>
		// Token: 0x06000360 RID: 864 RVA: 0x0001F9F4 File Offset: 0x0001DBF4
		internal static string GetProcessName(int iPID)
		{
			string result;
			try
			{
				ProcessHelper.ProcessNameCacheEntry oCacheEntry;
				if (ProcessHelper.dictProcessNames.TryGetValue(iPID, out oCacheEntry))
				{
					if (oCacheEntry.ulLastLookup > Utilities.GetTickCount() - 30000UL)
					{
						return oCacheEntry.sProcessName;
					}
					Dictionary<int, ProcessHelper.ProcessNameCacheEntry> obj = ProcessHelper.dictProcessNames;
					lock (obj)
					{
						ProcessHelper.dictProcessNames.Remove(iPID);
					}
				}
				string sResult = Process.GetProcessById(iPID).ProcessName.ToLower();
				if (string.IsNullOrEmpty(sResult))
				{
					result = string.Empty;
				}
				else
				{
					if (ProcessHelper.bDisambiguateWWAHostApps)
					{
						IPlatformExtensions extensions = PlatformExtensionsFactory.Instance.CreatePlatformExtensions();
						sResult = extensions.PostProcessProcessName(iPID, sResult);
					}
					Dictionary<int, ProcessHelper.ProcessNameCacheEntry> obj2 = ProcessHelper.dictProcessNames;
					lock (obj2)
					{
						if (!ProcessHelper.dictProcessNames.ContainsKey(iPID))
						{
							ProcessHelper.dictProcessNames.Add(iPID, new ProcessHelper.ProcessNameCacheEntry(sResult));
						}
					}
					result = sResult;
				}
			}
			catch (Exception eX)
			{
				result = string.Empty;
			}
			return result;
		}

		// Token: 0x04000197 RID: 407
		private static bool bDisambiguateWWAHostApps = false;

		// Token: 0x04000198 RID: 408
		private const uint MSEC_PROCESSNAME_CACHE_LIFETIME = 30000U;

		// Token: 0x04000199 RID: 409
		private static readonly Dictionary<int, ProcessHelper.ProcessNameCacheEntry> dictProcessNames = new Dictionary<int, ProcessHelper.ProcessNameCacheEntry>();

		/// <summary>
		/// Structure mapping a Process ID (PID) to a ProcessName
		/// </summary>
		// Token: 0x020000D2 RID: 210
		internal struct ProcessNameCacheEntry
		{
			/// <summary>
			/// Create a PID-&gt;ProcessName mapping
			/// </summary>
			/// <param name="_sProcessName">The ProcessName (e.g. IEXPLORE.EXE)</param>
			// Token: 0x0600072E RID: 1838 RVA: 0x00038FF6 File Offset: 0x000371F6
			internal ProcessNameCacheEntry(string _sProcessName)
			{
				this.ulLastLookup = Utilities.GetTickCount();
				this.sProcessName = _sProcessName;
			}

			/// <summary>
			/// The TickCount when this entry was created
			/// </summary>
			// Token: 0x04000370 RID: 880
			internal readonly ulong ulLastLookup;

			/// <summary>
			/// The ProcessName (e.g. IEXPLORE.EXE)
			/// </summary>
			// Token: 0x04000371 RID: 881
			internal readonly string sProcessName;
		}
	}
}
