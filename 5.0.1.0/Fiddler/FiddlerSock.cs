using System;
using FiddlerCore.PlatformExtensions;
using FiddlerCore.PlatformExtensions.API;

namespace Fiddler
{
	// Token: 0x02000016 RID: 22
	internal static class FiddlerSock
	{
		/// <summary>
		/// Map a local port number to the originating process ID
		/// </summary>
		/// <param name="iPort">The local port number</param>
		/// <returns>The originating process ID</returns>
		// Token: 0x060000EE RID: 238 RVA: 0x0000EEEC File Offset: 0x0000D0EC
		internal static int MapLocalPortToProcessId(int iPort)
		{
			int processId;
			string errorMessage;
			if (!FiddlerSock.platformExtensions.TryMapPortToProcessId(iPort, CONFIG.EnableIPv6, out processId, out errorMessage))
			{
				FiddlerApplication.Log.LogString(errorMessage);
			}
			return processId;
		}

		/// <summary>
		/// Returns a string containing the process listening on a given port
		/// </summary>
		// Token: 0x060000EF RID: 239 RVA: 0x0000EF1C File Offset: 0x0000D11C
		internal static string GetListeningProcess(int iPort)
		{
			string processName;
			int processId;
			string errorMessage;
			if (!FiddlerSock.platformExtensions.TryGetListeningProcessOnPort(iPort, out processName, out processId, out errorMessage))
			{
				FiddlerApplication.Log.LogString(errorMessage);
			}
			if (processId < 1)
			{
				return string.Empty;
			}
			return processName + ":" + processId;
		}

		// Token: 0x04000056 RID: 86
		private static readonly IPlatformExtensions platformExtensions = PlatformExtensionsFactory.Instance.CreatePlatformExtensions();
	}
}
