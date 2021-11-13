using System;
using System.Runtime.InteropServices;
using FiddlerCore.PlatformExtensions.API;
using FiddlerCore.Utilities;

namespace FiddlerCore.PlatformExtensions.Windows
{
	// Token: 0x02000096 RID: 150
	internal class ProxyHelperForWindows : IProxyHelper
	{
		// Token: 0x06000623 RID: 1571
		[DllImport("urlmon.dll", CharSet = CharSet.Auto, EntryPoint = "UrlMkSetSessionOption", SetLastError = true)]
		private static extern int UrlMkSetSessionOptionProxy(uint dwOption, ProxyHelperForWindows.INTERNET_PROXY_INFO structNewProxy, uint dwLen, uint dwZero);

		// Token: 0x06000624 RID: 1572
		[DllImport("wininet.dll", CharSet = CharSet.Ansi, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool InternetQueryOption(IntPtr hInternet, int Option, byte[] OptionInfo, ref int size);

		// Token: 0x06000625 RID: 1573 RVA: 0x000349D8 File Offset: 0x00032BD8
		private ProxyHelperForWindows()
		{
		}

		// Token: 0x170000F9 RID: 249
		// (get) Token: 0x06000626 RID: 1574 RVA: 0x000349E0 File Offset: 0x00032BE0
		public static ProxyHelperForWindows Instance
		{
			get
			{
				if (ProxyHelperForWindows.instance == null)
				{
					ProxyHelperForWindows.instance = new ProxyHelperForWindows();
				}
				return ProxyHelperForWindows.instance;
			}
		}

		// Token: 0x06000627 RID: 1575 RVA: 0x000349F8 File Offset: 0x00032BF8
		public void DisableProxyForCurrentProcess()
		{
			ProxyHelperForWindows.INTERNET_PROXY_INFO oInfo = new ProxyHelperForWindows.INTERNET_PROXY_INFO();
			oInfo.dwAccessType = 1U;
			oInfo.lpszProxy = (oInfo.lpszProxyBypass = null);
			uint dwSize = (uint)Marshal.SizeOf<ProxyHelperForWindows.INTERNET_PROXY_INFO>(oInfo);
			int iResult = ProxyHelperForWindows.UrlMkSetSessionOptionProxy(38U, oInfo, dwSize, 0U);
		}

		// Token: 0x06000628 RID: 1576 RVA: 0x00034A34 File Offset: 0x00032C34
		public string GetProxyForCurrentProcessAsHexView()
		{
			int size = 0;
			byte[] buffer = new byte[1];
			size = buffer.Length;
			if (!ProxyHelperForWindows.InternetQueryOption(IntPtr.Zero, 38, buffer, ref size) && size != buffer.Length)
			{
				buffer = new byte[size];
				size = buffer.Length;
				bool r = ProxyHelperForWindows.InternetQueryOption(IntPtr.Zero, 38, buffer, ref size);
			}
			return HexViewHelper.ByteArrayToHexView(buffer, 16);
		}

		// Token: 0x06000629 RID: 1577 RVA: 0x00034A8C File Offset: 0x00032C8C
		public void ResetProxyForCurrentProcess()
		{
			int iResult = ProxyHelperForWindows.UrlMkSetSessionOptionProxy(37U, null, 0U, 0U);
		}

		// Token: 0x0600062A RID: 1578 RVA: 0x00034AA4 File Offset: 0x00032CA4
		public void SetProxyForCurrentProcess(string proxy, string bypassList)
		{
			ProxyHelperForWindows.INTERNET_PROXY_INFO oInfo = new ProxyHelperForWindows.INTERNET_PROXY_INFO();
			oInfo.dwAccessType = 3U;
			oInfo.lpszProxy = proxy;
			oInfo.lpszProxyBypass = bypassList;
			uint dwSize = (uint)Marshal.SizeOf<ProxyHelperForWindows.INTERNET_PROXY_INFO>(oInfo);
			int iResult = ProxyHelperForWindows.UrlMkSetSessionOptionProxy(38U, oInfo, dwSize, 0U);
		}

		// Token: 0x040002B5 RID: 693
		private static ProxyHelperForWindows instance;

		// Token: 0x040002B6 RID: 694
		private const uint INTERNET_OPEN_TYPE_PRECONFIG = 0U;

		// Token: 0x040002B7 RID: 695
		private const uint INTERNET_OPEN_TYPE_DIRECT = 1U;

		// Token: 0x040002B8 RID: 696
		private const uint INTERNET_OPEN_TYPE_PROXY = 3U;

		// Token: 0x040002B9 RID: 697
		private const uint INTERNET_OPEN_TYPE_PRECONFIG_WITH_NO_AUTOPROXY = 4U;

		// Token: 0x040002BA RID: 698
		private const uint INTERNET_OPTION_REFRESH = 37U;

		// Token: 0x040002BB RID: 699
		private const uint INTERNET_OPTION_PROXY = 38U;

		// Token: 0x020000E1 RID: 225
		[StructLayout(LayoutKind.Sequential)]
		private class INTERNET_PROXY_INFO
		{
			// Token: 0x040003A6 RID: 934
			[MarshalAs(UnmanagedType.U4)]
			public uint dwAccessType;

			// Token: 0x040003A7 RID: 935
			[MarshalAs(UnmanagedType.LPStr)]
			public string lpszProxy;

			// Token: 0x040003A8 RID: 936
			[MarshalAs(UnmanagedType.LPStr)]
			public string lpszProxyBypass;
		}
	}
}
