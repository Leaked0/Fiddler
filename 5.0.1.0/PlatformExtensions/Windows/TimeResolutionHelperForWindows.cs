using System;
using System.Runtime.InteropServices;

namespace FiddlerCore.PlatformExtensions.Windows
{
	// Token: 0x02000097 RID: 151
	internal static class TimeResolutionHelperForWindows
	{
		// Token: 0x0600062B RID: 1579
		[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
		private static extern uint MM_timeBeginPeriod(uint iMS);

		// Token: 0x0600062C RID: 1580
		[DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
		private static extern uint MM_timeEndPeriod(uint iMS);

		// Token: 0x170000FA RID: 250
		// (get) Token: 0x0600062D RID: 1581 RVA: 0x00034ADE File Offset: 0x00032CDE
		// (set) Token: 0x0600062E RID: 1582 RVA: 0x00034AE8 File Offset: 0x00032CE8
		public static bool EnableHighResolutionTimers
		{
			get
			{
				return TimeResolutionHelperForWindows._EnabledHighResTimers;
			}
			set
			{
				if (value == TimeResolutionHelperForWindows._EnabledHighResTimers)
				{
					return;
				}
				if (value)
				{
					uint iRes = TimeResolutionHelperForWindows.MM_timeBeginPeriod(1U);
					TimeResolutionHelperForWindows._EnabledHighResTimers = iRes == 0U;
					return;
				}
				uint iRes2 = TimeResolutionHelperForWindows.MM_timeEndPeriod(1U);
				TimeResolutionHelperForWindows._EnabledHighResTimers = iRes2 > 0U;
			}
		}

		// Token: 0x040002BC RID: 700
		private static bool _EnabledHighResTimers;
	}
}
