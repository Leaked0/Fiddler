using System;
using System.ComponentModel;
using System.Diagnostics;

namespace FiddlerCore.PlatformExtensions.Unix
{
	// Token: 0x0200009F RID: 159
	internal static class PortProcessMapperForUnix
	{
		// Token: 0x06000658 RID: 1624 RVA: 0x000355FC File Offset: 0x000337FC
		internal static bool TryMapLocalPortToProcessId(int iPort, out int processId, out string errorMessage)
		{
			string lsofArguments = string.Format("-n -o -P -F p -i tcp:{0}{1}", iPort, string.Empty);
			processId = PortProcessMapperForUnix.GetPIDFromLSOF(lsofArguments, out errorMessage);
			return string.IsNullOrEmpty(errorMessage);
		}

		// Token: 0x06000659 RID: 1625 RVA: 0x00035634 File Offset: 0x00033834
		internal static bool TryGetListeningProcessOnPort(int port, out string processName, out int processId, out string errorMessage)
		{
			string lsofArguments = string.Format("-n -o -P -F p -i tcp:{0}{1}", port, " -s tcp:LISTEN");
			processId = PortProcessMapperForUnix.GetPIDFromLSOF(lsofArguments, out errorMessage);
			if (processId < 1)
			{
				processName = string.Empty;
				return string.IsNullOrEmpty(errorMessage);
			}
			try
			{
				processName = Process.GetProcessById(processId).ProcessName.ToLower();
			}
			catch (Exception eX)
			{
				errorMessage = string.Format("Unable to get process name of processId: {0}\n{1}", processId, eX);
				processName = string.Empty;
				return false;
			}
			if (string.IsNullOrEmpty(processName))
			{
				processName = "unknown";
			}
			return true;
		}

		// Token: 0x0600065A RID: 1626 RVA: 0x000356D4 File Offset: 0x000338D4
		private static int GetPIDFromLSOF(string lsofArguments, out string errorMessage)
		{
			int iCandidatePID = 0;
			try
			{
				using (Process oProc = new Process())
				{
					oProc.StartInfo.UseShellExecute = false;
					oProc.StartInfo.RedirectStandardOutput = true;
					oProc.StartInfo.RedirectStandardError = false;
					oProc.StartInfo.CreateNoWindow = true;
					oProc.StartInfo.FileName = PortProcessMapperForUnix.lsofCommand;
					oProc.StartInfo.Arguments = lsofArguments;
					oProc.Start();
					string sLine;
					while ((sLine = oProc.StandardOutput.ReadLine()) != null)
					{
						if (sLine.StartsWith("p", StringComparison.OrdinalIgnoreCase))
						{
							string sProcID = sLine.Substring(1);
							int iPID;
							if (int.TryParse(sProcID, out iPID) && iPID != PortProcessMapperForUnix.iProxyPID)
							{
								iCandidatePID = iPID;
							}
						}
					}
					try
					{
						oProc.WaitForExit(1);
					}
					catch
					{
					}
				}
				errorMessage = null;
				return iCandidatePID;
			}
			catch (Win32Exception eXX)
			{
				errorMessage = string.Format("Process-determination failed. lsof returned {0}.\n{1}", eXX.NativeErrorCode, eXX);
			}
			catch (Exception eX)
			{
				errorMessage = string.Format("Process-determination failed.\n{0}", eX);
				if (!PortProcessMapperForUnix.lsofCommand.StartsWith("/"))
				{
					PortProcessMapperForUnix.lsofCommand = "/usr/sbin/" + PortProcessMapperForUnix.lsofCommand;
					errorMessage = null;
					return PortProcessMapperForUnix.GetPIDFromLSOF(lsofArguments, out errorMessage);
				}
			}
			return 0;
		}

		// Token: 0x040002D6 RID: 726
		private const string lsofArgumentsFormat = "-n -o -P -F p -i tcp:{0}{1}";

		// Token: 0x040002D7 RID: 727
		private const string tcpListenStateOnly = " -s tcp:LISTEN";

		// Token: 0x040002D8 RID: 728
		private static readonly int iProxyPID = Process.GetCurrentProcess().Id;

		// Token: 0x040002D9 RID: 729
		private static string lsofCommand = "lsof";
	}
}
