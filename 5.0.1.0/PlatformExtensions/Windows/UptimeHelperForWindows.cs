using System;
using System.Runtime.InteropServices;

namespace FiddlerCore.PlatformExtensions.Windows
{
	// Token: 0x02000098 RID: 152
	internal static class UptimeHelperForWindows
	{
		// Token: 0x0600062F RID: 1583
		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern ulong GetTickCount64();

		// Token: 0x06000630 RID: 1584 RVA: 0x00034B22 File Offset: 0x00032D22
		public static bool TryGetUptimeInMilliseconds(out ulong milliseconds)
		{
			milliseconds = 0UL;
			if (Environment.OSVersion.Version.Major > 5)
			{
				milliseconds = UptimeHelperForWindows.GetTickCount64();
				return true;
			}
			return false;
		}
	}
}
