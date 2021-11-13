using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace FiddlerCore.PlatformExtensions.Windows
{
	// Token: 0x02000095 RID: 149
	internal static class ProcessHelperForWindows
	{
		// Token: 0x0600061E RID: 1566
		[DllImport("kernel32.dll")]
		internal static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

		// Token: 0x0600061F RID: 1567
		[DllImport("kernel32.dll")]
		private static extern bool CloseHandle(IntPtr hHandle);

		// Token: 0x06000620 RID: 1568
		[DllImport("kernel32.dll")]
		internal static extern int GetApplicationUserModelId(IntPtr hProcess, ref uint AppModelIDLength, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sbAppUserModelID);

		// Token: 0x06000621 RID: 1569 RVA: 0x00034848 File Offset: 0x00032A48
		public static string DisambiguateWWAHostApps(int iPID, string sResult)
		{
			if (sResult.Equals("WWAHost", StringComparison.OrdinalIgnoreCase))
			{
				try
				{
					IntPtr ptrProcess = ProcessHelperForWindows.OpenProcess(4096, false, iPID);
					if (IntPtr.Zero != ptrProcess)
					{
						uint cchLen = 130U;
						StringBuilder sbName = new StringBuilder((int)cchLen);
						int lResult = ProcessHelperForWindows.GetApplicationUserModelId(ptrProcess, ref cchLen, sbName);
						if (lResult == 0)
						{
							sResult = string.Format("{0}!{1}", sResult, sbName);
						}
						else if (122 == lResult)
						{
							sbName = new StringBuilder((int)cchLen);
							if (ProcessHelperForWindows.GetApplicationUserModelId(ptrProcess, ref cchLen, sbName) == 0)
							{
								sResult = string.Format("{0}!{1}", sResult, sbName);
							}
						}
						ProcessHelperForWindows.CloseHandle(ptrProcess);
					}
				}
				catch
				{
				}
			}
			return sResult;
		}

		// Token: 0x06000622 RID: 1570 RVA: 0x000348EC File Offset: 0x00032AEC
		public static int GetParentProcessId(int childProcessId, out string errorMessage)
		{
			string commandArgs = string.Format("process where (processid={0}) get parentprocessid", childProcessId);
			int parentProcessId = 0;
			try
			{
				using (Process oProc = new Process())
				{
					oProc.StartInfo.UseShellExecute = false;
					oProc.StartInfo.RedirectStandardOutput = true;
					oProc.StartInfo.RedirectStandardError = false;
					oProc.StartInfo.CreateNoWindow = true;
					oProc.StartInfo.FileName = "wmic";
					oProc.StartInfo.Arguments = commandArgs;
					oProc.Start();
					string output = oProc.StandardOutput.ReadToEnd();
					Regex regex = new Regex("^\\d+", RegexOptions.Multiline);
					string result = regex.Match(output).Value;
					parentProcessId = int.Parse(result);
				}
				errorMessage = null;
			}
			catch (Exception ex)
			{
				errorMessage = string.Format("Process-determination failed.\n{0}", ex);
			}
			return parentProcessId;
		}

		// Token: 0x040002B2 RID: 690
		private const int QueryLimitedInformation = 4096;

		// Token: 0x040002B3 RID: 691
		private const int ERROR_INSUFFICIENT_BUFFER = 122;

		// Token: 0x040002B4 RID: 692
		private const int ERROR_SUCCESS = 0;
	}
}
