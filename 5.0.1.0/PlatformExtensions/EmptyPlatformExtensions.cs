using System;
using FiddlerCore.PlatformExtensions.API;

namespace FiddlerCore.PlatformExtensions
{
	// Token: 0x0200008F RID: 143
	internal abstract class EmptyPlatformExtensions : BasePlatformExtensions, IPlatformExtensions
	{
		// Token: 0x060005F4 RID: 1524 RVA: 0x00034415 File Offset: 0x00032615
		public override bool TryMapPortToProcessId(int port, bool includeIPv6, out int processId, out string errorMessage)
		{
			processId = 0;
			errorMessage = "This method is not supported on your platform.";
			return false;
		}

		// Token: 0x060005F5 RID: 1525 RVA: 0x00034423 File Offset: 0x00032623
		public override bool TryGetListeningProcessOnPort(int port, out string processName, out int processId, out string errorMessage)
		{
			processName = string.Empty;
			processId = 0;
			errorMessage = "This method is not supported on your platform.";
			return false;
		}

		// Token: 0x170000F1 RID: 241
		// (get) Token: 0x060005F6 RID: 1526 RVA: 0x00034438 File Offset: 0x00032638
		public override bool HighResolutionTimersEnabled
		{
			get
			{
				return false;
			}
		}

		// Token: 0x060005F7 RID: 1527 RVA: 0x0003443B File Offset: 0x0003263B
		public override bool TryChangeTimersResolution(bool increase)
		{
			return false;
		}

		// Token: 0x170000F2 RID: 242
		// (get) Token: 0x060005F8 RID: 1528 RVA: 0x0003443E File Offset: 0x0003263E
		public override IProxyHelper ProxyHelper
		{
			get
			{
				return EmptyProxyHelper.Instance;
			}
		}

		// Token: 0x060005F9 RID: 1529 RVA: 0x00034445 File Offset: 0x00032645
		public override IAutoProxy CreateAutoProxy(bool autoDiscover, string pacUrl, bool autoProxyRunInProcess, bool autoLoginIfChallenged)
		{
			throw new NotSupportedException("This method is not supported on your platform.");
		}

		// Token: 0x060005FA RID: 1530 RVA: 0x00034451 File Offset: 0x00032651
		public override byte[] DecompressXpress(byte[] data)
		{
			throw new NotSupportedException("This method is not supported on your platform.");
		}

		// Token: 0x060005FB RID: 1531 RVA: 0x0003445D File Offset: 0x0003265D
		public override string PostProcessProcessName(int pid, string processName)
		{
			return processName;
		}

		// Token: 0x060005FC RID: 1532 RVA: 0x00034460 File Offset: 0x00032660
		public override void SetUserAgentStringForCurrentProcess(string userAgent)
		{
			throw new NotSupportedException("This method is not supported on your platform.");
		}

		// Token: 0x060005FD RID: 1533 RVA: 0x0003446C File Offset: 0x0003266C
		public override bool TryGetUptimeInMilliseconds(out ulong milliseconds)
		{
			milliseconds = 0UL;
			return false;
		}
	}
}
