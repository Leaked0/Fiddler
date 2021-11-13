using System;

namespace FiddlerCore.PlatformExtensions.API
{
	/// <summary>
	/// Implement this interface in order to provide FiddlerCore with access to native WinINet API.
	/// </summary>
	// Token: 0x020000A8 RID: 168
	internal interface IWinINetHelper
	{
		/// <summary>
		/// Clears WinINet's cache.
		/// </summary>
		/// <param name="clearFiles">true if cache files should be cleared, false otherwise.</param>
		/// <param name="clearCookies">true if cookies should be cleared, false otherwise.</param>
		// Token: 0x06000683 RID: 1667
		void ClearCacheItems(bool clearFiles, bool clearCookies);

		/// <summary>
		/// Delete all permanent WinINet cookies for a <paramref name="host" />.
		/// </summary>
		/// <param name="host">The hostname whose cookies should be cleared.</param>
		// Token: 0x06000684 RID: 1668
		void ClearCookiesForHost(string host);

		/// <summary>
		/// Use this method in order to get cache information for a <paramref name="url" />.
		/// </summary>
		/// <param name="url">The URL for which the cache info is requested.</param>
		/// <returns>String, containing cache information for the given <paramref name="url" />.</returns>
		// Token: 0x06000685 RID: 1669
		string GetCacheItemInfo(string url);
	}
}
