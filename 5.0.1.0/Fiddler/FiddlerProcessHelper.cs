using System;
using FiddlerCore.PlatformExtensions;
using FiddlerCore.PlatformExtensions.API;

namespace Fiddler
{
	// Token: 0x02000017 RID: 23
	internal static class FiddlerProcessHelper
	{
		/// <summary>
		/// This method is used to get the parent process Id.
		/// </summary>
		/// <param name="childProcessId">Contains the child process Id.</param>
		/// <returns> the parent process Id and error message equal to null if the operation is successful, otherwise the parent process Id is 0 and the error message contains the exception.</returns>
		// Token: 0x060000F1 RID: 241 RVA: 0x0000EF74 File Offset: 0x0000D174
		internal static int TryGetParentProcessId(int childProcessId)
		{
			string errorMessage;
			int parentProcessId = FiddlerProcessHelper.platformExtensions.GetParentProcessId(childProcessId, out errorMessage);
			if (!string.IsNullOrEmpty(errorMessage))
			{
				FiddlerApplication.Log.LogString(errorMessage);
			}
			return parentProcessId;
		}

		// Token: 0x04000057 RID: 87
		private static readonly IPlatformExtensions platformExtensions = PlatformExtensionsFactory.Instance.CreatePlatformExtensions();
	}
}
