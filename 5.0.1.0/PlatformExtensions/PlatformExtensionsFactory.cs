using System;
using System.Runtime.InteropServices;
using FiddlerCore.PlatformExtensions.API;
using FiddlerCore.PlatformExtensions.Unix.Linux;
using FiddlerCore.PlatformExtensions.Unix.Mac;
using FiddlerCore.PlatformExtensions.Windows;

namespace FiddlerCore.PlatformExtensions
{
	// Token: 0x02000091 RID: 145
	internal sealed class PlatformExtensionsFactory : IPlatformExtensionsFactory
	{
		// Token: 0x06000605 RID: 1541 RVA: 0x000344CB File Offset: 0x000326CB
		private PlatformExtensionsFactory()
		{
		}

		// Token: 0x170000F4 RID: 244
		// (get) Token: 0x06000606 RID: 1542 RVA: 0x000344D3 File Offset: 0x000326D3
		public static PlatformExtensionsFactory Instance
		{
			get
			{
				if (PlatformExtensionsFactory.instance == null)
				{
					PlatformExtensionsFactory.instance = new PlatformExtensionsFactory();
				}
				return PlatformExtensionsFactory.instance;
			}
		}

		// Token: 0x06000607 RID: 1543 RVA: 0x000344EC File Offset: 0x000326EC
		public IPlatformExtensions CreatePlatformExtensions()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return PlatformExtensionsForWindows.Instance;
			}
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return PlatformExtensionsForLinux.Instance;
			}
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return PlatformExtensionsForMac.Instance;
			}
			throw new PlatformNotSupportedException("Your platform is not supported by FiddlerCore.PlatformExtensions");
		}

		// Token: 0x040002AB RID: 683
		private static PlatformExtensionsFactory instance;
	}
}
