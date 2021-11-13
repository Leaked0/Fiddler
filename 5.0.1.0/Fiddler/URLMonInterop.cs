using System;
using FiddlerCore.PlatformExtensions;
using FiddlerCore.PlatformExtensions.API;

namespace Fiddler
{
	/// <summary>
	/// URLMon Interop Class
	/// </summary>
	// Token: 0x02000064 RID: 100
	public static class URLMonInterop
	{
		/// <summary>
		/// Set the user-agent string for the current process
		/// </summary>
		/// <param name="sUA">New UA string</param>
		// Token: 0x060004A8 RID: 1192 RVA: 0x0002DD68 File Offset: 0x0002BF68
		public static void SetUAStringInProcess(string sUA)
		{
			IPlatformExtensions extensions = PlatformExtensionsFactory.Instance.CreatePlatformExtensions();
			extensions.SetUserAgentStringForCurrentProcess(sUA);
		}

		/// <summary>
		/// Query WinINET for the current process' proxy settings. Oddly, there's no way to UrlMkGetSessionOption for the current proxy.
		/// </summary>
		/// <returns>String of hex suitable for display</returns>
		// Token: 0x060004A9 RID: 1193 RVA: 0x0002DD88 File Offset: 0x0002BF88
		public static string GetProxyInProcess()
		{
			IPlatformExtensions extensions = PlatformExtensionsFactory.Instance.CreatePlatformExtensions();
			return extensions.ProxyHelper.GetProxyForCurrentProcessAsHexView();
		}

		/// <summary>
		/// Configures the current process to use the system proxy for URLMon/WinINET traffic.
		/// </summary>
		// Token: 0x060004AA RID: 1194 RVA: 0x0002DDAC File Offset: 0x0002BFAC
		public static void ResetProxyInProcessToDefault()
		{
			IPlatformExtensions extensions = PlatformExtensionsFactory.Instance.CreatePlatformExtensions();
			extensions.ProxyHelper.ResetProxyForCurrentProcess();
		}

		/// <summary>
		/// Configures the current process to use no Proxy for URLMon/WinINET traffic.
		/// </summary>
		// Token: 0x060004AB RID: 1195 RVA: 0x0002DDD0 File Offset: 0x0002BFD0
		public static void SetProxyDisabledForProcess()
		{
			IPlatformExtensions extensions = PlatformExtensionsFactory.Instance.CreatePlatformExtensions();
			extensions.ProxyHelper.DisableProxyForCurrentProcess();
		}

		/// <summary>
		/// Sets the proxy for the current process to the specified list. See http://msdn.microsoft.com/en-us/library/aa383996(VS.85).aspx
		/// </summary>
		/// <param name="sProxy">e.g. "127.0.0.1:8888" or "http=insecProxy:80;https=secProxy:444"</param>
		/// <param name="sBypassList">Semi-colon delimted list of hosts to bypass proxy; use &lt;local&gt; to bypass for Intranet</param>
		// Token: 0x060004AC RID: 1196 RVA: 0x0002DDF4 File Offset: 0x0002BFF4
		public static void SetProxyInProcess(string sProxy, string sBypassList)
		{
			IPlatformExtensions extensions = PlatformExtensionsFactory.Instance.CreatePlatformExtensions();
			extensions.ProxyHelper.SetProxyForCurrentProcess(sProxy, sBypassList);
		}
	}
}
