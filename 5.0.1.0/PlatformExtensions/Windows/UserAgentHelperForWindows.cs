using System;
using System.Runtime.InteropServices;

namespace FiddlerCore.PlatformExtensions.Windows
{
	// Token: 0x02000099 RID: 153
	internal static class UserAgentHelperForWindows
	{
		// Token: 0x06000631 RID: 1585
		[DllImport("urlmon.dll", CharSet = CharSet.Ansi, EntryPoint = "UrlMkSetSessionOption", SetLastError = true)]
		private static extern int UrlMkSetSessionOptionUA(uint dwOption, string sNewUA, uint dwLen, uint dwZero);

		// Token: 0x06000632 RID: 1586 RVA: 0x00034B44 File Offset: 0x00032D44
		public static void SetUserAgentStringForCurrentProcess(string userAgent)
		{
			UserAgentHelperForWindows.UrlMkSetSessionOptionUA(268435457U, userAgent, (uint)userAgent.Length, 0U);
		}

		// Token: 0x040002BD RID: 701
		private const uint URLMON_OPTION_USERAGENT = 268435457U;

		// Token: 0x040002BE RID: 702
		private const uint URLMON_OPTION_USERAGENT_REFRESH = 268435458U;
	}
}
