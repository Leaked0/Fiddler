using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using FiddlerCore.PlatformExtensions.API;

namespace FiddlerCore.PlatformExtensions.Windows
{
	// Token: 0x0200009C RID: 156
	internal class WinINetHelper : IWinINetHelper
	{
		// Token: 0x06000640 RID: 1600 RVA: 0x00034DC5 File Offset: 0x00032FC5
		private WinINetHelper()
		{
		}

		// Token: 0x170000FB RID: 251
		// (get) Token: 0x06000641 RID: 1601 RVA: 0x00034DCD File Offset: 0x00032FCD
		public static WinINetHelper Instance
		{
			get
			{
				if (WinINetHelper.instance == null)
				{
					WinINetHelper.instance = new WinINetHelper();
				}
				return WinINetHelper.instance;
			}
		}

		// Token: 0x06000642 RID: 1602 RVA: 0x00034DE8 File Offset: 0x00032FE8
		public void ClearCacheItems(bool clearFiles, bool clearCookies)
		{
			if (Environment.OSVersion.Version.Major > 5)
			{
				PlatformExtensionsForWindows.Instance.OnLog(string.Format("Windows Vista+ detected. Calling INETCPL to clear [{0}{1}].", clearFiles ? "CacheFiles" : string.Empty, clearCookies ? "Cookies" : string.Empty));
				this.VistaClearTracks(clearFiles, clearCookies);
				return;
			}
			if (clearCookies)
			{
				this.ClearCookiesForHost("*");
			}
			if (!clearFiles)
			{
				return;
			}
			PlatformExtensionsForWindows.Instance.OnLog("Beginning WinINET Cache clearing...");
			long groupId = 0L;
			int cacheEntryInfoBufferSizeInitial = 0;
			IntPtr cacheEntryInfoBuffer = IntPtr.Zero;
			IntPtr enumHandle = IntPtr.Zero;
			enumHandle = WinINetHelper.FindFirstUrlCacheGroup(0, 0, IntPtr.Zero, 0, ref groupId, IntPtr.Zero);
			int iLastError = Marshal.GetLastWin32Error();
			bool returnValue;
			if (enumHandle != IntPtr.Zero && 259 != iLastError && 2 != iLastError)
			{
				do
				{
					returnValue = WinINetHelper.DeleteUrlCacheGroup(groupId, 2, IntPtr.Zero);
					iLastError = Marshal.GetLastWin32Error();
					if (!returnValue && 2 == iLastError)
					{
						returnValue = WinINetHelper.FindNextUrlCacheGroup(enumHandle, ref groupId, IntPtr.Zero);
						iLastError = Marshal.GetLastWin32Error();
					}
				}
				while (returnValue || (259 != iLastError && 2 != iLastError));
			}
			enumHandle = WinINetHelper.FindFirstUrlCacheEntryEx(null, 0, WinINetHelper.WININETCACHEENTRYTYPE.ALL, 0L, IntPtr.Zero, ref cacheEntryInfoBufferSizeInitial, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			iLastError = Marshal.GetLastWin32Error();
			if (IntPtr.Zero == enumHandle && 259 == iLastError)
			{
				return;
			}
			int cacheEntryInfoBufferSize = cacheEntryInfoBufferSizeInitial;
			cacheEntryInfoBuffer = Marshal.AllocHGlobal(cacheEntryInfoBufferSize);
			enumHandle = WinINetHelper.FindFirstUrlCacheEntryEx(null, 0, WinINetHelper.WININETCACHEENTRYTYPE.ALL, 0L, cacheEntryInfoBuffer, ref cacheEntryInfoBufferSizeInitial, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			iLastError = Marshal.GetLastWin32Error();
			do
			{
				WinINetHelper.INTERNET_CACHE_ENTRY_INFOA internetCacheEntry = (WinINetHelper.INTERNET_CACHE_ENTRY_INFOA)Marshal.PtrToStructure(cacheEntryInfoBuffer, typeof(WinINetHelper.INTERNET_CACHE_ENTRY_INFOA));
				cacheEntryInfoBufferSizeInitial = cacheEntryInfoBufferSize;
				if (WinINetHelper.WININETCACHEENTRYTYPE.COOKIE_CACHE_ENTRY != (internetCacheEntry.CacheEntryType & WinINetHelper.WININETCACHEENTRYTYPE.COOKIE_CACHE_ENTRY))
				{
					returnValue = WinINetHelper.DeleteUrlCacheEntry(internetCacheEntry.lpszSourceUrlName);
				}
				returnValue = WinINetHelper.FindNextUrlCacheEntryEx(enumHandle, cacheEntryInfoBuffer, ref cacheEntryInfoBufferSizeInitial, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				iLastError = Marshal.GetLastWin32Error();
				if (!returnValue && 259 == iLastError)
				{
					break;
				}
				if (!returnValue && cacheEntryInfoBufferSizeInitial > cacheEntryInfoBufferSize)
				{
					cacheEntryInfoBufferSize = cacheEntryInfoBufferSizeInitial;
					cacheEntryInfoBuffer = Marshal.ReAllocHGlobal(cacheEntryInfoBuffer, (IntPtr)cacheEntryInfoBufferSize);
					returnValue = WinINetHelper.FindNextUrlCacheEntryEx(enumHandle, cacheEntryInfoBuffer, ref cacheEntryInfoBufferSizeInitial, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				}
			}
			while (returnValue);
			Marshal.FreeHGlobal(cacheEntryInfoBuffer);
			PlatformExtensionsForWindows.Instance.OnLog("Completed WinINET Cache clearing.");
		}

		// Token: 0x06000643 RID: 1603 RVA: 0x0003502C File Offset: 0x0003322C
		public void ClearCookiesForHost(string host)
		{
			host = host.Trim();
			if (host.Length < 1)
			{
				return;
			}
			string sFilter;
			if (host == "*")
			{
				sFilter = string.Empty;
				if (Environment.OSVersion.Version.Major > 5)
				{
					this.VistaClearTracks(false, true);
					return;
				}
			}
			else
			{
				sFilter = (host.StartsWith("*") ? host.Substring(1).ToLower() : ("@" + host.ToLower()));
			}
			int cacheEntryInfoBufferSizeInitial = 0;
			IntPtr cacheEntryInfoBuffer = IntPtr.Zero;
			IntPtr enumHandle = IntPtr.Zero;
			enumHandle = WinINetHelper.FindFirstUrlCacheEntry("cookie:", IntPtr.Zero, ref cacheEntryInfoBufferSizeInitial);
			if (enumHandle == IntPtr.Zero && 259 == Marshal.GetLastWin32Error())
			{
				return;
			}
			int cacheEntryInfoBufferSize = cacheEntryInfoBufferSizeInitial;
			cacheEntryInfoBuffer = Marshal.AllocHGlobal(cacheEntryInfoBufferSize);
			enumHandle = WinINetHelper.FindFirstUrlCacheEntry("cookie:", cacheEntryInfoBuffer, ref cacheEntryInfoBufferSizeInitial);
			for (;;)
			{
				WinINetHelper.INTERNET_CACHE_ENTRY_INFOA internetCacheEntry = (WinINetHelper.INTERNET_CACHE_ENTRY_INFOA)Marshal.PtrToStructure(cacheEntryInfoBuffer, typeof(WinINetHelper.INTERNET_CACHE_ENTRY_INFOA));
				cacheEntryInfoBufferSizeInitial = cacheEntryInfoBufferSize;
				if (WinINetHelper.WININETCACHEENTRYTYPE.COOKIE_CACHE_ENTRY == (internetCacheEntry.CacheEntryType & WinINetHelper.WININETCACHEENTRYTYPE.COOKIE_CACHE_ENTRY))
				{
					bool bDeleteThisCookie;
					if (sFilter.Length == 0)
					{
						bDeleteThisCookie = true;
					}
					else
					{
						string sCandidateHost = Marshal.PtrToStringAnsi(internetCacheEntry.lpszSourceUrlName);
						int ixSlash = sCandidateHost.IndexOf('/');
						if (ixSlash > 0)
						{
							sCandidateHost = sCandidateHost.Remove(ixSlash);
						}
						sCandidateHost = sCandidateHost.ToLower();
						bDeleteThisCookie = sCandidateHost.EndsWith(sFilter);
					}
					if (bDeleteThisCookie)
					{
						bool returnValue = WinINetHelper.DeleteUrlCacheEntry(internetCacheEntry.lpszSourceUrlName);
					}
				}
				for (;;)
				{
					bool returnValue = WinINetHelper.FindNextUrlCacheEntry(enumHandle, cacheEntryInfoBuffer, ref cacheEntryInfoBufferSizeInitial);
					if (!returnValue && 259 == Marshal.GetLastWin32Error())
					{
						goto IL_186;
					}
					if (returnValue || cacheEntryInfoBufferSizeInitial <= cacheEntryInfoBufferSize)
					{
						break;
					}
					cacheEntryInfoBufferSize = cacheEntryInfoBufferSizeInitial;
					cacheEntryInfoBuffer = Marshal.ReAllocHGlobal(cacheEntryInfoBuffer, (IntPtr)cacheEntryInfoBufferSize);
				}
			}
			IL_186:
			Marshal.FreeHGlobal(cacheEntryInfoBuffer);
		}

		// Token: 0x06000644 RID: 1604 RVA: 0x000351C8 File Offset: 0x000333C8
		public string GetCacheItemInfo(string url)
		{
			int cacheEntryInfoBufferSizeInitial = 0;
			IntPtr cacheEntryInfoBuffer = IntPtr.Zero;
			bool bResult = WinINetHelper.GetUrlCacheEntryInfoA(url, cacheEntryInfoBuffer, ref cacheEntryInfoBufferSizeInitial);
			int iLastError = Marshal.GetLastWin32Error();
			if (bResult || iLastError != 122)
			{
				return string.Format("This URL is not present in the WinINET cache. [Code: {0}]", iLastError);
			}
			int cacheEntryInfoBufferSize = cacheEntryInfoBufferSizeInitial;
			cacheEntryInfoBuffer = Marshal.AllocHGlobal(cacheEntryInfoBufferSize);
			bResult = WinINetHelper.GetUrlCacheEntryInfoA(url, cacheEntryInfoBuffer, ref cacheEntryInfoBufferSizeInitial);
			iLastError = Marshal.GetLastWin32Error();
			if (!bResult)
			{
				Marshal.FreeHGlobal(cacheEntryInfoBuffer);
				return "GetUrlCacheEntryInfoA with buffer failed. 2=filenotfound 122=insufficient buffer, 259=nomoreitems. Last error: " + iLastError.ToString() + "\n";
			}
			WinINetHelper.INTERNET_CACHE_ENTRY_INFOA internetCacheEntry = (WinINetHelper.INTERNET_CACHE_ENTRY_INFOA)Marshal.PtrToStructure(cacheEntryInfoBuffer, typeof(WinINetHelper.INTERNET_CACHE_ENTRY_INFOA));
			cacheEntryInfoBufferSizeInitial = cacheEntryInfoBufferSize;
			long lngLastMod = ((long)internetCacheEntry.LastModifiedTime.dwHighDateTime << 32) | (long)((ulong)internetCacheEntry.LastModifiedTime.dwLowDateTime);
			long lngLastAccess = ((long)internetCacheEntry.LastAccessTime.dwHighDateTime << 32) | (long)((ulong)internetCacheEntry.LastAccessTime.dwLowDateTime);
			long lngLastSync = ((long)internetCacheEntry.LastSyncTime.dwHighDateTime << 32) | (long)((ulong)internetCacheEntry.LastSyncTime.dwLowDateTime);
			long lngExpire = ((long)internetCacheEntry.ExpireTime.dwHighDateTime << 32) | (long)((ulong)internetCacheEntry.ExpireTime.dwLowDateTime);
			string sResult = string.Concat(new string[]
			{
				"Url:\t\t",
				Marshal.PtrToStringAnsi(internetCacheEntry.lpszSourceUrlName),
				"\nCache File:\t",
				Marshal.PtrToStringAnsi(internetCacheEntry.lpszLocalFileName),
				"\nSize:\t\t",
				(((ulong)internetCacheEntry.dwSizeHigh << 32) + (ulong)internetCacheEntry.dwSizeLow).ToString("0,0"),
				" bytes\nFile Extension:\t",
				Marshal.PtrToStringAnsi(internetCacheEntry.lpszFileExtension),
				"\nHit Rate:\t",
				internetCacheEntry.dwHitRate.ToString(),
				"\nUse Count:\t",
				internetCacheEntry.dwUseCount.ToString(),
				"\nDon't Scavenge for:\t",
				internetCacheEntry._Union.dwExemptDelta.ToString(),
				" seconds\nLast Modified:\t",
				DateTime.FromFileTime(lngLastMod).ToString(),
				"\nLast Accessed:\t",
				DateTime.FromFileTime(lngLastAccess).ToString(),
				"\nLast Synced:  \t",
				DateTime.FromFileTime(lngLastSync).ToString(),
				"\nEntry Expires:\t",
				DateTime.FromFileTime(lngExpire).ToString(),
				"\n"
			});
			Marshal.FreeHGlobal(cacheEntryInfoBuffer);
			return sResult;
		}

		// Token: 0x06000645 RID: 1605 RVA: 0x00035434 File Offset: 0x00033634
		private void VistaClearTracks(bool clearFiles, bool clearCookies)
		{
			int iFlag = 0;
			if (clearCookies)
			{
				iFlag |= 2;
			}
			if (clearFiles)
			{
				iFlag |= 4108;
			}
			try
			{
				using (Process.Start("rundll32.exe", "inetcpl.cpl,ClearMyTracksByProcess " + iFlag.ToString()))
				{
				}
			}
			catch (Exception eX)
			{
				PlatformExtensionsForWindows.Instance.OnError("Failed to launch ClearMyTracksByProcess.\n" + eX.Message);
			}
		}

		// Token: 0x06000646 RID: 1606
		[DllImport("wininet.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetUrlCacheEntryInfoA(string lpszUrlName, IntPtr lpCacheEntryInfo, ref int lpdwCacheEntryInfoBufferSize);

		// Token: 0x06000647 RID: 1607
		[DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr FindFirstUrlCacheGroup(int dwFlags, int dwFilter, IntPtr lpSearchCondition, int dwSearchCondition, ref long lpGroupId, IntPtr lpReserved);

		// Token: 0x06000648 RID: 1608
		[DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool FindNextUrlCacheGroup(IntPtr hFind, ref long lpGroupId, IntPtr lpReserved);

		// Token: 0x06000649 RID: 1609
		[DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteUrlCacheGroup(long GroupId, int dwFlags, IntPtr lpReserved);

		// Token: 0x0600064A RID: 1610
		[DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "FindFirstUrlCacheEntryA", ExactSpelling = true, SetLastError = true)]
		private static extern IntPtr FindFirstUrlCacheEntry([MarshalAs(UnmanagedType.LPTStr)] string lpszUrlSearchPattern, IntPtr lpFirstCacheEntryInfo, ref int lpdwFirstCacheEntryInfoBufferSize);

		// Token: 0x0600064B RID: 1611
		[DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "FindNextUrlCacheEntryA", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool FindNextUrlCacheEntry(IntPtr hFind, IntPtr lpNextCacheEntryInfo, ref int lpdwNextCacheEntryInfoBufferSize);

		// Token: 0x0600064C RID: 1612
		[DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "FindFirstUrlCacheEntryExA", ExactSpelling = true, SetLastError = true)]
		private static extern IntPtr FindFirstUrlCacheEntryEx([MarshalAs(UnmanagedType.LPTStr)] string lpszUrlSearchPattern, int dwFlags, WinINetHelper.WININETCACHEENTRYTYPE dwFilter, long GroupId, IntPtr lpFirstCacheEntryInfo, ref int lpdwFirstCacheEntryInfoBufferSize, IntPtr lpReserved, IntPtr pcbReserved2, IntPtr lpReserved3);

		// Token: 0x0600064D RID: 1613
		[DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "FindNextUrlCacheEntryExA", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool FindNextUrlCacheEntryEx(IntPtr hEnumHandle, IntPtr lpNextCacheEntryInfo, ref int lpdwNextCacheEntryInfoBufferSize, IntPtr lpReserved, IntPtr pcbReserved2, IntPtr lpReserved3);

		// Token: 0x0600064E RID: 1614
		[DllImport("wininet.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "DeleteUrlCacheEntryA", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteUrlCacheEntry(IntPtr lpszUrlName);

		// Token: 0x040002D0 RID: 720
		private static WinINetHelper instance;

		// Token: 0x040002D1 RID: 721
		private const int CACHEGROUP_SEARCH_ALL = 0;

		// Token: 0x040002D2 RID: 722
		private const int CACHEGROUP_FLAG_FLUSHURL_ONDELETE = 2;

		// Token: 0x040002D3 RID: 723
		private const int ERROR_FILE_NOT_FOUND = 2;

		// Token: 0x040002D4 RID: 724
		private const int ERROR_NO_MORE_ITEMS = 259;

		// Token: 0x040002D5 RID: 725
		private const int ERROR_INSUFFICENT_BUFFER = 122;

		// Token: 0x020000E4 RID: 228
		private enum WININETCACHEENTRYTYPE
		{
			// Token: 0x040003B3 RID: 947
			None,
			// Token: 0x040003B4 RID: 948
			NORMAL_CACHE_ENTRY,
			// Token: 0x040003B5 RID: 949
			STICKY_CACHE_ENTRY = 4,
			// Token: 0x040003B6 RID: 950
			EDITED_CACHE_ENTRY = 8,
			// Token: 0x040003B7 RID: 951
			TRACK_OFFLINE_CACHE_ENTRY = 16,
			// Token: 0x040003B8 RID: 952
			TRACK_ONLINE_CACHE_ENTRY = 32,
			// Token: 0x040003B9 RID: 953
			SPARSE_CACHE_ENTRY = 65536,
			// Token: 0x040003BA RID: 954
			COOKIE_CACHE_ENTRY = 1048576,
			// Token: 0x040003BB RID: 955
			URLHISTORY_CACHE_ENTRY = 2097152,
			// Token: 0x040003BC RID: 956
			ALL = 3211325
		}

		/// <summary>
		/// For PInvoke: Contains information about an entry in the Internet cache
		/// </summary>
		// Token: 0x020000E5 RID: 229
		[StructLayout(LayoutKind.Sequential)]
		private class INTERNET_CACHE_ENTRY_INFOA
		{
			// Token: 0x040003BD RID: 957
			public uint dwStructureSize;

			// Token: 0x040003BE RID: 958
			public IntPtr lpszSourceUrlName;

			// Token: 0x040003BF RID: 959
			public IntPtr lpszLocalFileName;

			// Token: 0x040003C0 RID: 960
			public WinINetHelper.WININETCACHEENTRYTYPE CacheEntryType;

			// Token: 0x040003C1 RID: 961
			public uint dwUseCount;

			// Token: 0x040003C2 RID: 962
			public uint dwHitRate;

			// Token: 0x040003C3 RID: 963
			public uint dwSizeLow;

			// Token: 0x040003C4 RID: 964
			public uint dwSizeHigh;

			// Token: 0x040003C5 RID: 965
			public FILETIME LastModifiedTime;

			// Token: 0x040003C6 RID: 966
			public FILETIME ExpireTime;

			// Token: 0x040003C7 RID: 967
			public FILETIME LastAccessTime;

			// Token: 0x040003C8 RID: 968
			public FILETIME LastSyncTime;

			// Token: 0x040003C9 RID: 969
			public IntPtr lpHeaderInfo;

			// Token: 0x040003CA RID: 970
			public uint dwHeaderInfoSize;

			// Token: 0x040003CB RID: 971
			public IntPtr lpszFileExtension;

			// Token: 0x040003CC RID: 972
			public WinINetHelper.WININETCACHEENTRYINFOUNION _Union;
		}

		// Token: 0x020000E6 RID: 230
		[StructLayout(LayoutKind.Explicit)]
		private struct WININETCACHEENTRYINFOUNION
		{
			// Token: 0x040003CD RID: 973
			[FieldOffset(0)]
			public uint dwReserved;

			// Token: 0x040003CE RID: 974
			[FieldOffset(0)]
			public uint dwExemptDelta;
		}
	}
}
