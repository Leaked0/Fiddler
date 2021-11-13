using System;
using System.Collections;
using System.Collections.Generic;

namespace Fiddler
{
	/// <summary>
	/// The IFiddlerPreferences Interface is exposed by the FiddlerApplication.Prefs object, and enables
	/// callers to Add, Update, and Remove preferences, as well as observe changes to the preferences.
	/// </summary>
	// Token: 0x0200002D RID: 45
	public interface IFiddlerPreferences : IEnumerable<KeyValuePair<string, string>>, IEnumerable
	{
		/// <summary>
		/// Store a boolean value for a preference
		/// </summary>
		/// <param name="sPrefName">The named preference</param>
		/// <param name="bValue">The boolean value to store</param>
		// Token: 0x060001BF RID: 447
		void SetBoolPref(string sPrefName, bool bValue);

		/// <summary>
		/// Store an Int32 value for a preference
		/// </summary>
		/// <param name="sPrefName">The named preference</param>
		/// <param name="iValue">The int32 value to store</param>
		// Token: 0x060001C0 RID: 448
		void SetInt32Pref(string sPrefName, int iValue);

		/// <summary>
		/// Store a string value for a preference
		/// </summary>
		/// <param name="sPrefName">The named preference</param>
		/// <param name="sValue">The string value to store</param>
		// Token: 0x060001C1 RID: 449
		void SetStringPref(string sPrefName, string sValue);

		/// <summary>
		/// Store multiple preferences.
		/// </summary>
		/// <param name="prefs">An enumeration of the preferences' names and values to store.</param>
		// Token: 0x060001C2 RID: 450
		void SetPrefs(IEnumerable<KeyValuePair<string, string>> prefs);

		/// <summary>
		/// Get a preference's value as a boolean
		/// </summary>
		/// <param name="sPrefName">The Preference Name</param>
		/// <param name="bDefault">The default value for missing or invalid preferences</param>
		/// <returns>A Boolean</returns>
		// Token: 0x060001C3 RID: 451
		bool GetBoolPref(string sPrefName, bool bDefault);

		/// <summary>
		/// Gets a preference's value as a string
		/// </summary>
		/// <param name="sPrefName">The Preference Name</param>
		/// <param name="sDefault">The default value for missing preferences</param>
		/// <returns>A string</returns>
		// Token: 0x060001C4 RID: 452
		string GetStringPref(string sPrefName, string sDefault);

		/// <summary>
		/// Gets a preference's value as a 32-bit integer
		/// </summary>
		/// <param name="sPrefName">The Preference Name</param>
		/// <param name="iDefault">The default value for missing or invalid preferences</param>
		/// <returns>An integer</returns>
		// Token: 0x060001C5 RID: 453
		int GetInt32Pref(string sPrefName, int iDefault);

		/// <summary>
		/// Removes a named preference from storage
		/// </summary>
		/// <param name="sPrefName">The name of the preference to remove</param>
		// Token: 0x060001C6 RID: 454
		void RemovePref(string sPrefName);

		/// <summary>
		/// Add a Watcher that will be notified when a value has changed within the specified prefix.
		/// </summary>
		/// <param name="sPrefixFilter">The prefix of preferences for which changes are interesting</param>
		/// <param name="pcehHandler">The Event handler to notify</param>
		/// <returns>Returns the Watcher object added to the notification list</returns>
		// Token: 0x060001C7 RID: 455
		PreferenceBag.PrefWatcher AddWatcher(string sPrefixFilter, EventHandler<PrefChangeEventArgs> pcehHandler);

		/// <summary>
		/// Removes a previously-created preference Watcher from the notification queue
		/// </summary>
		/// <param name="wliToRemove">The Watcher to remove</param>
		// Token: 0x060001C8 RID: 456
		void RemoveWatcher(PreferenceBag.PrefWatcher wliToRemove);

		/// <summary>
		/// Indexer. Returns the value of the preference as a string
		/// </summary>
		/// <param name="sName">The Preference Name</param>
		/// <returns>The Preference value as a string, or null</returns>
		// Token: 0x1700005A RID: 90
		string this[string sName] { get; set; }
	}
}
