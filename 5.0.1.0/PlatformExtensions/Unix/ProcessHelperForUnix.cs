using System;
using System.Diagnostics;

namespace FiddlerCore.PlatformExtensions.Unix
{
	// Token: 0x020000A0 RID: 160
	internal static class ProcessHelperForUnix
	{
		// Token: 0x0600065C RID: 1628 RVA: 0x00035854 File Offset: 0x00033A54
		internal static int GetParentProcessId(int childProcessId, out string errorMessage)
		{
			string command = string.Format("ps -o ppid= -p {0}", childProcessId);
			int parentProcessId = 0;
			try
			{
				using (Process oProc = new Process())
				{
					oProc.StartInfo.UseShellExecute = false;
					oProc.StartInfo.RedirectStandardOutput = true;
					oProc.StartInfo.RedirectStandardInput = true;
					oProc.StartInfo.RedirectStandardError = false;
					oProc.StartInfo.CreateNoWindow = true;
					oProc.StartInfo.FileName = "/bin/bash";
					oProc.Start();
					oProc.StandardInput.WriteLine(command);
					string output = oProc.StandardOutput.ReadLine();
					parentProcessId = int.Parse(output);
				}
				errorMessage = null;
			}
			catch (Exception ex)
			{
				errorMessage = string.Format("Process-determination failed.\n{0}", ex);
			}
			return parentProcessId;
		}
	}
}
