using System;
using System.Runtime.InteropServices;

namespace FiddlerCore.PlatformExtensions.Windows
{
	// Token: 0x0200009A RID: 154
	internal static class Win32Helper
	{
		// Token: 0x06000633 RID: 1587
		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr GlobalFree(IntPtr hMem);

		// Token: 0x06000634 RID: 1588 RVA: 0x00034B59 File Offset: 0x00032D59
		internal static void GlobalFreeIfNonZero(IntPtr hMem)
		{
			if (IntPtr.Zero != hMem)
			{
				Win32Helper.GlobalFree(hMem);
			}
		}
	}
}
