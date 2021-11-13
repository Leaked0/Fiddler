using System;
using FiddlerCore.PlatformExtensions.API;

namespace FiddlerCore.PlatformExtensions.Unix.Linux
{
	// Token: 0x020000A2 RID: 162
	internal class PlatformExtensionsForLinux : PlatformExtensionsForUnix, IPlatformExtensions
	{
		// Token: 0x06000665 RID: 1637 RVA: 0x00036005 File Offset: 0x00034205
		private PlatformExtensionsForLinux()
		{
		}

		// Token: 0x170000FD RID: 253
		// (get) Token: 0x06000666 RID: 1638 RVA: 0x0003600D File Offset: 0x0003420D
		public static PlatformExtensionsForLinux Instance
		{
			get
			{
				if (PlatformExtensionsForLinux.instance == null)
				{
					PlatformExtensionsForLinux.instance = new PlatformExtensionsForLinux();
				}
				return PlatformExtensionsForLinux.instance;
			}
		}

		// Token: 0x040002DB RID: 731
		private static PlatformExtensionsForLinux instance;
	}
}
