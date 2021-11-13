using System;
using System.Runtime.InteropServices;
using FiddlerCore.PlatformExtensions.API;

namespace FiddlerCore.PlatformExtensions.Windows
{
	// Token: 0x0200009B RID: 155
	internal class WinHttpAutoProxy : IAutoProxy, IDisposable
	{
		// Token: 0x06000635 RID: 1589 RVA: 0x00034B6F File Offset: 0x00032D6F
		public WinHttpAutoProxy(bool autoDiscover, string pacUrl, bool autoProxyRunInProcess, bool autoLoginIfChallenged)
		{
			this.autoProxyOptions = WinHttpAutoProxy.GetAutoProxyOptionsStruct(autoDiscover, pacUrl, autoProxyRunInProcess, autoLoginIfChallenged);
			this.internetSessionHandle = WinHttpAutoProxy.WinHttpOpen("Fiddler", 1, IntPtr.Zero, IntPtr.Zero, 0);
		}

		// Token: 0x06000636 RID: 1590 RVA: 0x00034BA4 File Offset: 0x00032DA4
		public bool TryGetProxyForUrl(string url, out string proxy, out string errorMessage)
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException("WinHttpAutoProxy");
			}
			int iLastError = 0;
			WinHttpAutoProxy.WINHTTP_PROXY_INFO oProxy;
			bool bResult = WinHttpAutoProxy.WinHttpGetProxyForUrl(this.internetSessionHandle, url, ref this.autoProxyOptions, out oProxy);
			if (!bResult)
			{
				iLastError = Marshal.GetLastWin32Error();
			}
			if (bResult)
			{
				proxy = Marshal.PtrToStringUni(oProxy.lpszProxy);
				errorMessage = null;
				Win32Helper.GlobalFreeIfNonZero(oProxy.lpszProxy);
				Win32Helper.GlobalFreeIfNonZero(oProxy.lpszProxyBypass);
				return true;
			}
			if (iLastError <= 12015)
			{
				if (iLastError == 12006)
				{
					string wpadUrl = (this.TryGetPacUrl(out wpadUrl) ? wpadUrl : string.Empty);
					errorMessage = string.Format("PAC Script download failure; Fiddler only supports HTTP/HTTPS for PAC script URLs; WPAD returned '{0}'.", wpadUrl.Replace("\n", "\\n"));
					goto IL_F8;
				}
				if (iLastError == 12015)
				{
					errorMessage = "PAC Script download failure; you must set the AutoProxyLogon registry key to TRUE.";
					goto IL_F8;
				}
			}
			else
			{
				if (iLastError == 12166)
				{
					errorMessage = "PAC Script contents were not valid.";
					goto IL_F8;
				}
				if (iLastError == 12167)
				{
					errorMessage = "PAC Script download failed.";
					goto IL_F8;
				}
				if (iLastError == 12180)
				{
					errorMessage = "AutoProxy Detection failed.";
					goto IL_F8;
				}
			}
			errorMessage = "Proxy determination failed with error code: " + iLastError.ToString();
			IL_F8:
			proxy = null;
			Win32Helper.GlobalFreeIfNonZero(oProxy.lpszProxy);
			Win32Helper.GlobalFreeIfNonZero(oProxy.lpszProxyBypass);
			return false;
		}

		/// <summary>
		/// Outs WPAD-discovered URL for display purposes (e.g. Help&gt; About); note that we don't actually use this when determining the gateway,
		/// instead relying on the WinHTTPGetProxyForUrl function to do this work for us.
		/// </summary>
		/// <returns>A WPAD url, if found, or String.Empty</returns>
		// Token: 0x06000637 RID: 1591 RVA: 0x00034CC4 File Offset: 0x00032EC4
		public bool TryGetPacUrl(out string pacUrl)
		{
			IntPtr pszResult;
			bool bResult = WinHttpAutoProxy.WinHttpDetectAutoProxyConfigUrl(3, out pszResult);
			if (!bResult || pszResult == IntPtr.Zero)
			{
				pacUrl = null;
				return false;
			}
			pacUrl = Marshal.PtrToStringUni(pszResult);
			Win32Helper.GlobalFreeIfNonZero(pszResult);
			return true;
		}

		// Token: 0x06000638 RID: 1592 RVA: 0x00034CFE File Offset: 0x00032EFE
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06000639 RID: 1593 RVA: 0x00034D0D File Offset: 0x00032F0D
		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
			{
				return;
			}
			this.disposed = true;
			WinHttpAutoProxy.WinHttpCloseHandle(this.internetSessionHandle);
		}

		// Token: 0x0600063A RID: 1594 RVA: 0x00034D30 File Offset: 0x00032F30
		~WinHttpAutoProxy()
		{
			this.Dispose(false);
		}

		// Token: 0x0600063B RID: 1595 RVA: 0x00034D60 File Offset: 0x00032F60
		private static WinHttpAutoProxy.WINHTTP_AUTOPROXY_OPTIONS GetAutoProxyOptionsStruct(bool autoDiscover, string pacUrl, bool autoProxyRunInProcess, bool autoLoginIfChallenged)
		{
			WinHttpAutoProxy.WINHTTP_AUTOPROXY_OPTIONS result = default(WinHttpAutoProxy.WINHTTP_AUTOPROXY_OPTIONS);
			if (autoProxyRunInProcess)
			{
				result.dwFlags = 65536;
			}
			else
			{
				result.dwFlags = 0;
			}
			if (autoDiscover)
			{
				result.dwFlags |= 1;
				result.dwAutoDetectFlags = 3;
			}
			if (pacUrl != null)
			{
				result.dwFlags |= 2;
				result.lpszAutoConfigUrl = pacUrl;
			}
			result.fAutoLoginIfChallenged = autoLoginIfChallenged;
			return result;
		}

		// Token: 0x0600063C RID: 1596
		[DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr WinHttpOpen([MarshalAs(UnmanagedType.LPWStr)] [In] string pwszUserAgent, [In] int dwAccessType, [In] IntPtr pwszProxyName, [In] IntPtr pwszProxyBypass, [In] int dwFlags);

		/// <summary>
		/// Note: Be sure to use the same hSession to prevent redownload of the proxy script
		/// </summary>
		// Token: 0x0600063D RID: 1597
		[DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool WinHttpGetProxyForUrl(IntPtr hSession, [MarshalAs(UnmanagedType.LPWStr)] string lpcwszUrl, [In] ref WinHttpAutoProxy.WINHTTP_AUTOPROXY_OPTIONS pAutoProxyOptions, out WinHttpAutoProxy.WINHTTP_PROXY_INFO pProxyInfo);

		// Token: 0x0600063E RID: 1598
		[DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool WinHttpDetectAutoProxyConfigUrl([MarshalAs(UnmanagedType.U4)] int dwAutoDetectFlags, out IntPtr ppwszAutoConfigUrl);

		// Token: 0x0600063F RID: 1599
		[DllImport("winhttp.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool WinHttpCloseHandle([In] IntPtr hInternet);

		// Token: 0x040002BF RID: 703
		private const int WINHTTP_ACCESS_TYPE_DEFAULT_PROXY = 0;

		// Token: 0x040002C0 RID: 704
		private const int WINHTTP_ACCESS_TYPE_NO_PROXY = 1;

		// Token: 0x040002C1 RID: 705
		private const int WINHTTP_ACCESS_TYPE_NAMED_PROXY = 3;

		// Token: 0x040002C2 RID: 706
		private const int WINHTTP_AUTOPROXY_AUTO_DETECT = 1;

		// Token: 0x040002C3 RID: 707
		private const int WINHTTP_AUTOPROXY_CONFIG_URL = 2;

		// Token: 0x040002C4 RID: 708
		private const int WINHTTP_AUTOPROXY_RUN_INPROCESS = 65536;

		// Token: 0x040002C5 RID: 709
		private const int WINHTTP_AUTOPROXY_RUN_OUTPROCESS_ONLY = 131072;

		// Token: 0x040002C6 RID: 710
		private const int WINHTTP_AUTO_DETECT_TYPE_DHCP = 1;

		// Token: 0x040002C7 RID: 711
		private const int WINHTTP_AUTO_DETECT_TYPE_DNS_A = 2;

		// Token: 0x040002C8 RID: 712
		private const int ERROR_WINHTTP_LOGIN_FAILURE = 12015;

		// Token: 0x040002C9 RID: 713
		private const int ERROR_WINHTTP_UNABLE_TO_DOWNLOAD_SCRIPT = 12167;

		// Token: 0x040002CA RID: 714
		private const int ERROR_WINHTTP_UNRECOGNIZED_SCHEME = 12006;

		// Token: 0x040002CB RID: 715
		private const int ERROR_WINHTTP_AUTODETECTION_FAILED = 12180;

		// Token: 0x040002CC RID: 716
		private const int ERROR_WINHTTP_BAD_AUTO_PROXY_SCRIPT = 12166;

		// Token: 0x040002CD RID: 717
		private readonly IntPtr internetSessionHandle;

		// Token: 0x040002CE RID: 718
		private WinHttpAutoProxy.WINHTTP_AUTOPROXY_OPTIONS autoProxyOptions;

		// Token: 0x040002CF RID: 719
		private bool disposed;

		// Token: 0x020000E2 RID: 226
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct WINHTTP_AUTOPROXY_OPTIONS
		{
			// Token: 0x040003A9 RID: 937
			[MarshalAs(UnmanagedType.U4)]
			public int dwFlags;

			// Token: 0x040003AA RID: 938
			[MarshalAs(UnmanagedType.U4)]
			public int dwAutoDetectFlags;

			// Token: 0x040003AB RID: 939
			[MarshalAs(UnmanagedType.LPWStr)]
			public string lpszAutoConfigUrl;

			// Token: 0x040003AC RID: 940
			public IntPtr lpvReserved;

			// Token: 0x040003AD RID: 941
			[MarshalAs(UnmanagedType.U4)]
			public int dwReserved;

			/// <summary>
			/// Set to true to send Negotiate creds when challenged to download the script
			/// </summary>
			// Token: 0x040003AE RID: 942
			[MarshalAs(UnmanagedType.Bool)]
			public bool fAutoLoginIfChallenged;
		}

		// Token: 0x020000E3 RID: 227
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct WINHTTP_PROXY_INFO
		{
			// Token: 0x040003AF RID: 943
			[MarshalAs(UnmanagedType.U4)]
			public int dwAccessType;

			// Token: 0x040003B0 RID: 944
			public IntPtr lpszProxy;

			// Token: 0x040003B1 RID: 945
			public IntPtr lpszProxyBypass;
		}
	}
}
