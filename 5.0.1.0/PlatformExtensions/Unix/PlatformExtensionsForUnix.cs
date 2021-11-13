using System;
using FiddlerCore.PlatformExtensions.API;

namespace FiddlerCore.PlatformExtensions.Unix
{
	// Token: 0x0200009E RID: 158
	internal abstract class PlatformExtensionsForUnix : EmptyPlatformExtensions, IPlatformExtensions
	{
		// Token: 0x06000654 RID: 1620 RVA: 0x000355D2 File Offset: 0x000337D2
		public override bool TryMapPortToProcessId(int port, bool includeIPv6, out int processId, out string errorMessage)
		{
			return PortProcessMapperForUnix.TryMapLocalPortToProcessId(port, out processId, out errorMessage);
		}

		// Token: 0x06000655 RID: 1621 RVA: 0x000355DD File Offset: 0x000337DD
		public override bool TryGetListeningProcessOnPort(int port, out string processName, out int processId, out string errorMessage)
		{
			return PortProcessMapperForUnix.TryGetListeningProcessOnPort(port, out processName, out processId, out errorMessage);
		}

		// Token: 0x06000656 RID: 1622 RVA: 0x000355E9 File Offset: 0x000337E9
		public override int GetParentProcessId(int childProcessId, out string errorMessage)
		{
			return ProcessHelperForUnix.GetParentProcessId(childProcessId, out errorMessage);
		}
	}
}
