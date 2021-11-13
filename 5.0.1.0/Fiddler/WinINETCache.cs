using System;
using FiddlerCore.PlatformExtensions;
using FiddlerCore.PlatformExtensions.API;

namespace Fiddler
{
	/// <summary>
	/// Wrapper for WinINET cache APIs. 
	/// </summary>
	// Token: 0x02000075 RID: 117
	public class WinINETCache
	{
		/// <summary>
		/// Clear all HTTP Cookies from the WinINET Cache
		/// </summary>
		// Token: 0x060005AD RID: 1453 RVA: 0x00033BD4 File Offset: 0x00031DD4
		public static void ClearCookies()
		{
			WinINETCache.ClearCacheItems(false, true);
		}

		/// <summary>
		/// Clear all files from the WinINET Cache
		/// </summary>
		// Token: 0x060005AE RID: 1454 RVA: 0x00033BDD File Offset: 0x00031DDD
		public static void ClearFiles()
		{
			WinINETCache.ClearCacheItems(true, false);
		}

		/// <summary>
		/// Delete all permanent WinINET cookies for sHost; won't clear memory-only session cookies. Supports hostnames with an optional leading wildcard, e.g. *example.com. NOTE: Will not work on VistaIE Protected Mode cookies.
		/// </summary>
		/// <param name="sHost">The hostname whose cookies should be cleared</param>
		// Token: 0x060005AF RID: 1455 RVA: 0x00033BE8 File Offset: 0x00031DE8
		[CodeDescription("Delete all permanent WinINET cookies for sHost; won't clear memory-only session cookies. Supports hostnames with an optional leading wildcard, e.g. *example.com. NOTE: Will not work on VistaIE Protected Mode cookies.")]
		public static void ClearCookiesForHost(string sHost)
		{
			IWindowsSpecificPlatformExtensions extensions = PlatformExtensionsFactory.Instance.CreatePlatformExtensions() as IWindowsSpecificPlatformExtensions;
			if (extensions == null)
			{
				throw new NotSupportedException("This method is supported only on Windows.");
			}
			extensions.WinINetHelper.ClearCookiesForHost(sHost);
		}

		/// <summary>
		/// Clear the Cache items.  Note: May be synchronous, may be asynchronous.
		/// </summary>
		/// <param name="bClearFiles">TRUE if cache files should be cleared</param>
		/// <param name="bClearCookies">TRUE if cookies should be cleared</param>
		// Token: 0x060005B0 RID: 1456 RVA: 0x00033C20 File Offset: 0x00031E20
		public static void ClearCacheItems(bool bClearFiles, bool bClearCookies)
		{
			if (!bClearCookies && !bClearFiles)
			{
				throw new ArgumentException("You must call ClearCacheItems with at least one target");
			}
			if (!FiddlerApplication.DoClearCache(bClearFiles, bClearCookies))
			{
				FiddlerApplication.Log.LogString("Cache clearing was handled by an extension. Default clearing was skipped.");
				return;
			}
			IWindowsSpecificPlatformExtensions extensions = PlatformExtensionsFactory.Instance.CreatePlatformExtensions() as IWindowsSpecificPlatformExtensions;
			if (extensions == null)
			{
				throw new NotSupportedException("This method is supported only on Windows.");
			}
			extensions.WinINetHelper.ClearCacheItems(bClearFiles, bClearCookies);
		}

		// Token: 0x040002A4 RID: 676
		private const string SupportedOnlyOnWindowsMessage = "This method is supported only on Windows.";
	}
}
