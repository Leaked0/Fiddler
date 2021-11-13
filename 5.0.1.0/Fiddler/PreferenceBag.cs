using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading;

namespace Fiddler
{
	/// <summary>
	/// The PreferenceBag is used to maintain a threadsafe Key/Value list of preferences, persisted in the registry, and with appropriate eventing when a value changes.
	/// </summary>
	// Token: 0x0200002F RID: 47
	public class PreferenceBag : IFiddlerPreferences, IEnumerable<KeyValuePair<string, string>>, IEnumerable
	{
		// Token: 0x060001CF RID: 463 RVA: 0x000149F8 File Offset: 0x00012BF8
		internal PreferenceBag(string sRegPath)
		{
			this._sRegistryPath = sRegPath;
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x00014A49 File Offset: 0x00012C49
		public static bool isValidName(string sName)
		{
			return !string.IsNullOrEmpty(sName) && 256 > sName.Length && !sName.OICContains("internal") && 0 > sName.IndexOfAny(PreferenceBag._arrForbiddenChars);
		}

		/// <summary>
		/// Returns a string naming the current profile
		/// </summary>
		// Token: 0x1700005E RID: 94
		// (get) Token: 0x060001D1 RID: 465 RVA: 0x00014A7D File Offset: 0x00012C7D
		public string CurrentProfile
		{
			get
			{
				return this._sCurrentProfile;
			}
		}

		/// <summary>
		/// Indexer into the Preference collection.
		/// </summary>
		/// <param name="sPrefName">The name of the Preference to update/create or return.</param>
		/// <returns>The string value of the preference, or null.</returns>
		// Token: 0x1700005F RID: 95
		public string this[string sPrefName]
		{
			get
			{
				string result;
				try
				{
					this._RWLockPrefs.EnterReadLock();
					result = this._dictPrefs[sPrefName];
				}
				finally
				{
					this._RWLockPrefs.ExitReadLock();
				}
				return result;
			}
			set
			{
				if (!PreferenceBag.isValidName(sPrefName))
				{
					throw new ArgumentException(string.Format("Preference name must contain 1 to 255 characters from the set A-z0-9-_ and may not contain the word Internal.\n\nCaller tried to set: \"{0}\"", sPrefName));
				}
				if (value == null)
				{
					this.RemovePref(sPrefName);
					return;
				}
				bool _bNotifyChange = false;
				try
				{
					this._RWLockPrefs.EnterWriteLock();
					if (value != this._dictPrefs[sPrefName])
					{
						_bNotifyChange = true;
						this._dictPrefs[sPrefName] = value;
					}
				}
				finally
				{
					this._RWLockPrefs.ExitWriteLock();
				}
				if (_bNotifyChange)
				{
					PrefChangeEventArgs oArgs = new PrefChangeEventArgs(sPrefName, value);
					this.AsyncNotifyWatchers(oArgs);
				}
			}
		}

		/// <summary>
		/// Get a string array of the preference names
		/// </summary>
		/// <returns>string[] of preference names</returns>
		// Token: 0x060001D4 RID: 468 RVA: 0x00014B60 File Offset: 0x00012D60
		public string[] GetPrefArray()
		{
			string[] result;
			try
			{
				this._RWLockPrefs.EnterReadLock();
				string[] arrResult = new string[this._dictPrefs.Count];
				this._dictPrefs.Keys.CopyTo(arrResult, 0);
				result = arrResult;
			}
			finally
			{
				this._RWLockPrefs.ExitReadLock();
			}
			return result;
		}

		/// <summary>
		/// Gets a preference's value as a string
		/// </summary>
		/// <param name="sPrefName">The Preference Name</param>
		/// <param name="sDefault">The default value if the preference is missing</param>
		/// <returns>A string</returns>
		// Token: 0x060001D5 RID: 469 RVA: 0x00014BBC File Offset: 0x00012DBC
		public string GetStringPref(string sPrefName, string sDefault)
		{
			string sRet = this[sPrefName];
			return sRet ?? sDefault;
		}

		/// <summary>
		/// Return a bool preference.
		/// </summary>
		/// <param name="sPrefName">The Preference name</param>
		/// <param name="bDefault">The default value to return if the specified preference does not exist</param>
		/// <returns>The boolean value of the Preference, or the default value</returns>
		// Token: 0x060001D6 RID: 470 RVA: 0x00014BD8 File Offset: 0x00012DD8
		public bool GetBoolPref(string sPrefName, bool bDefault)
		{
			string sRet = this[sPrefName];
			if (sRet == null)
			{
				return bDefault;
			}
			bool bRet;
			if (bool.TryParse(sRet, out bRet))
			{
				return bRet;
			}
			return bDefault;
		}

		/// <summary>
		/// Return an Int32 Preference.
		/// </summary>
		/// <param name="sPrefName">The Preference name</param>
		/// <param name="iDefault">The default value to return if the specified preference does not exist</param>
		/// <returns>The Int32 value of the Preference, or the default value</returns>
		// Token: 0x060001D7 RID: 471 RVA: 0x00014C00 File Offset: 0x00012E00
		public int GetInt32Pref(string sPrefName, int iDefault)
		{
			string sRet = this[sPrefName];
			if (sRet == null)
			{
				return iDefault;
			}
			int iRet;
			if (int.TryParse(sRet, out iRet))
			{
				return iRet;
			}
			return iDefault;
		}

		/// <summary>
		/// Update or create a string preference.
		/// </summary>
		/// <param name="sPrefName">The name of the Preference</param>
		/// <param name="sValue">The value to assign to the Preference</param>
		// Token: 0x060001D8 RID: 472 RVA: 0x00014C27 File Offset: 0x00012E27
		public void SetStringPref(string sPrefName, string sValue)
		{
			this[sPrefName] = sValue;
		}

		/// <summary>
		/// Update or create a Int32 Preference
		/// </summary>
		/// <param name="sPrefName">The name of the Preference</param>
		/// <param name="iValue">The value to assign to the Preference</param>
		// Token: 0x060001D9 RID: 473 RVA: 0x00014C31 File Offset: 0x00012E31
		public void SetInt32Pref(string sPrefName, int iValue)
		{
			this[sPrefName] = iValue.ToString();
		}

		/// <summary>
		/// Update or create a Boolean preference.
		/// </summary>
		/// <param name="sPrefName">The name of the Preference</param>
		/// <param name="bValue">The value to assign to the Preference</param>
		// Token: 0x060001DA RID: 474 RVA: 0x00014C41 File Offset: 0x00012E41
		public void SetBoolPref(string sPrefName, bool bValue)
		{
			this[sPrefName] = bValue.ToString();
		}

		/// <summary>
		/// Update or create multiple preferences.
		/// </summary>
		/// <param name="prefs">An enumeration of the preferences' names and values to store.</param>
		// Token: 0x060001DB RID: 475 RVA: 0x00014C54 File Offset: 0x00012E54
		public void SetPrefs(IEnumerable<KeyValuePair<string, string>> prefs)
		{
			foreach (KeyValuePair<string, string> pref in prefs)
			{
				this[pref.Key] = pref.Value;
			}
		}

		/// <summary>
		/// Delete a Preference from the collection.
		/// </summary>
		/// <param name="sPrefName">The name of the Preference to be removed.</param>
		// Token: 0x060001DC RID: 476 RVA: 0x00014CAC File Offset: 0x00012EAC
		public void RemovePref(string sPrefName)
		{
			bool _bNotifyChange = false;
			try
			{
				this._RWLockPrefs.EnterWriteLock();
				_bNotifyChange = this._dictPrefs.ContainsKey(sPrefName);
				this._dictPrefs.Remove(sPrefName);
			}
			finally
			{
				this._RWLockPrefs.ExitWriteLock();
			}
			if (_bNotifyChange)
			{
				PrefChangeEventArgs oArgs = new PrefChangeEventArgs(sPrefName, null);
				this.AsyncNotifyWatchers(oArgs);
			}
		}

		/// <summary>
		/// Remove all Watchers
		/// </summary>
		// Token: 0x060001DD RID: 477 RVA: 0x00014D10 File Offset: 0x00012F10
		private void _clearWatchers()
		{
			this._RWLockWatchers.EnterWriteLock();
			try
			{
				this._listWatchers.Clear();
			}
			finally
			{
				this._RWLockWatchers.ExitWriteLock();
			}
		}

		/// <summary>
		/// Remove all watchers and write the registry.
		/// </summary>
		// Token: 0x060001DE RID: 478 RVA: 0x00014D54 File Offset: 0x00012F54
		public void Close()
		{
			this._clearWatchers();
		}

		/// <summary>
		/// Return a description of the contents of the preference bag
		/// </summary>
		/// <returns>Multi-line string</returns>
		// Token: 0x060001DF RID: 479 RVA: 0x00014D5C File Offset: 0x00012F5C
		public override string ToString()
		{
			return this.ToString(true);
		}

		/// <summary>
		/// Return a string-based serialization of the Preferences settings.
		/// </summary>
		/// <param name="bVerbose">TRUE for a multi-line format with all preferences</param>
		/// <returns>String</returns>
		// Token: 0x060001E0 RID: 480 RVA: 0x00014D68 File Offset: 0x00012F68
		public string ToString(bool bVerbose)
		{
			StringBuilder sbResult = new StringBuilder(128);
			try
			{
				this._RWLockPrefs.EnterReadLock();
				sbResult.AppendFormat("PreferenceBag [{0} Preferences. {1} Watchers.]", this._dictPrefs.Count, this._listWatchers.Count);
				if (bVerbose)
				{
					sbResult.Append("\n");
					foreach (object obj in this._dictPrefs)
					{
						DictionaryEntry dePrefVal = (DictionaryEntry)obj;
						sbResult.AppendFormat("{0}:\t{1}\n", dePrefVal.Key, dePrefVal.Value);
					}
				}
			}
			finally
			{
				this._RWLockPrefs.ExitReadLock();
			}
			return sbResult.ToString();
		}

		/// <summary>
		/// Returns a CRLF-delimited string containing all Preferences whose Name case-insensitively contains the specified filter string.
		/// </summary>
		/// <param name="sFilter">Partial string to match</param>
		/// <returns>A string</returns>
		// Token: 0x060001E1 RID: 481 RVA: 0x00014E48 File Offset: 0x00013048
		internal string FindMatches(string sFilter)
		{
			StringBuilder sbResult = new StringBuilder(128);
			try
			{
				this._RWLockPrefs.EnterReadLock();
				foreach (object obj in this._dictPrefs)
				{
					DictionaryEntry dePrefVal = (DictionaryEntry)obj;
					if (((string)dePrefVal.Key).OICContains(sFilter))
					{
						sbResult.AppendFormat("{0}:\t{1}\r\n", dePrefVal.Key, dePrefVal.Value);
					}
				}
			}
			finally
			{
				this._RWLockPrefs.ExitReadLock();
			}
			return sbResult.ToString();
		}

		/// <summary>
		/// Add a watcher for changes to the specified preference or preference branch.
		/// </summary>
		/// <param name="sPrefixFilter">Preference branch to monitor, or String.Empty to watch all</param>
		/// <param name="pcehHandler">The EventHandler accepting PrefChangeEventArgs to notify</param>
		/// <returns>Returns the PrefWatcher object which has been added, store to pass to RemoveWatcher later.</returns>
		// Token: 0x060001E2 RID: 482 RVA: 0x00014EFC File Offset: 0x000130FC
		public PreferenceBag.PrefWatcher AddWatcher(string sPrefixFilter, EventHandler<PrefChangeEventArgs> pcehHandler)
		{
			PreferenceBag.PrefWatcher wliNew = new PreferenceBag.PrefWatcher(sPrefixFilter.ToLower(), pcehHandler);
			this._RWLockWatchers.EnterWriteLock();
			try
			{
				this._listWatchers.Add(wliNew);
			}
			finally
			{
				this._RWLockWatchers.ExitWriteLock();
			}
			return wliNew;
		}

		/// <summary>
		/// Remove a previously attached Watcher
		/// </summary>
		/// <param name="wliToRemove">The previously-specified Watcher</param>
		// Token: 0x060001E3 RID: 483 RVA: 0x00014F50 File Offset: 0x00013150
		public void RemoveWatcher(PreferenceBag.PrefWatcher wliToRemove)
		{
			this._RWLockWatchers.EnterWriteLock();
			try
			{
				this._listWatchers.Remove(wliToRemove);
			}
			finally
			{
				this._RWLockWatchers.ExitWriteLock();
			}
		}

		/// <summary>
		/// This function executes on a single background thread and notifies any registered
		/// Watchers of changes in preferences they care about.
		/// </summary>
		/// <param name="objThreadState">A string containing the name of the Branch that changed</param>
		// Token: 0x060001E4 RID: 484 RVA: 0x00014F94 File Offset: 0x00013194
		private void _NotifyThreadExecute(object objThreadState)
		{
			PrefChangeEventArgs oArgs = (PrefChangeEventArgs)objThreadState;
			string sBranch = oArgs.PrefName;
			List<EventHandler<PrefChangeEventArgs>> listToNotify = null;
			try
			{
				this._RWLockWatchers.EnterReadLock();
				try
				{
					foreach (PreferenceBag.PrefWatcher wliEntry in this._listWatchers)
					{
						if (sBranch.OICStartsWith(wliEntry.sPrefixToWatch))
						{
							if (listToNotify == null)
							{
								listToNotify = new List<EventHandler<PrefChangeEventArgs>>();
							}
							listToNotify.Add(wliEntry.fnToNotify);
						}
					}
				}
				finally
				{
					this._RWLockWatchers.ExitReadLock();
				}
				if (listToNotify != null)
				{
					foreach (EventHandler<PrefChangeEventArgs> oEach in listToNotify)
					{
						try
						{
							oEach(this, oArgs);
						}
						catch (Exception eX)
						{
							FiddlerApplication.Log.LogString(eX.ToString());
						}
					}
				}
			}
			catch (Exception eX2)
			{
				FiddlerApplication.Log.LogString(eX2.ToString());
			}
		}

		/// <summary>
		/// Spawn a background thread to notify any interested Watchers of changes to the Target preference branch.
		/// </summary>
		/// <param name="oNotifyArgs">The arguments to pass to the interested Watchers</param>
		// Token: 0x060001E5 RID: 485 RVA: 0x000150C8 File Offset: 0x000132C8
		private void AsyncNotifyWatchers(PrefChangeEventArgs oNotifyArgs)
		{
			ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback(this._NotifyThreadExecute), oNotifyArgs);
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x000150DD File Offset: 0x000132DD
		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			this._RWLockPrefs.EnterReadLock();
			DictionaryEntry[] dictionaryEntries = new DictionaryEntry[this._dictPrefs.Count];
			this._dictPrefs.CopyTo(dictionaryEntries, 0);
			this._RWLockPrefs.ExitReadLock();
			int num;
			for (int i = 0; i < dictionaryEntries.Length; i = num + 1)
			{
				yield return new KeyValuePair<string, string>((string)dictionaryEntries[i].Key, (string)dictionaryEntries[i].Value);
				num = i;
			}
			yield break;
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x000150EC File Offset: 0x000132EC
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// Token: 0x040000D1 RID: 209
		private readonly StringDictionary _dictPrefs = new StringDictionary();

		// Token: 0x040000D2 RID: 210
		private readonly List<PreferenceBag.PrefWatcher> _listWatchers = new List<PreferenceBag.PrefWatcher>();

		// Token: 0x040000D3 RID: 211
		private readonly ReaderWriterLockSlim _RWLockPrefs = new ReaderWriterLockSlim();

		// Token: 0x040000D4 RID: 212
		private readonly ReaderWriterLockSlim _RWLockWatchers = new ReaderWriterLockSlim();

		// Token: 0x040000D5 RID: 213
		private string _sRegistryPath;

		// Token: 0x040000D6 RID: 214
		private string _sCurrentProfile = ".default";

		// Token: 0x040000D7 RID: 215
		private static char[] _arrForbiddenChars = new char[] { '*', ' ', '$', '%', '@', '?', '!' };

		/// <summary>
		/// A simple struct which contains a Branch identifier and EventHandler
		/// </summary>
		// Token: 0x020000C8 RID: 200
		public struct PrefWatcher
		{
			// Token: 0x06000708 RID: 1800 RVA: 0x00038B64 File Offset: 0x00036D64
			internal PrefWatcher(string sPrefixFilter, EventHandler<PrefChangeEventArgs> fnHandler)
			{
				this.sPrefixToWatch = sPrefixFilter;
				this.fnToNotify = fnHandler;
			}

			// Token: 0x04000355 RID: 853
			internal readonly EventHandler<PrefChangeEventArgs> fnToNotify;

			// Token: 0x04000356 RID: 854
			internal readonly string sPrefixToWatch;
		}
	}
}
