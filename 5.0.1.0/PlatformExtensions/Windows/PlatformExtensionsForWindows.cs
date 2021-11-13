using System;
using Fiddler;
using FiddlerCore.PlatformExtensions.API;

namespace FiddlerCore.PlatformExtensions.Windows
{
	// Token: 0x02000093 RID: 147
	internal class PlatformExtensionsForWindows : BasePlatformExtensions, IWindowsSpecificPlatformExtensions, IPlatformExtensions
	{
		// Token: 0x06000608 RID: 1544 RVA: 0x00034539 File Offset: 0x00032739
		private PlatformExtensionsForWindows()
		{
		}

		// Token: 0x170000F5 RID: 245
		// (get) Token: 0x06000609 RID: 1545 RVA: 0x00034541 File Offset: 0x00032741
		public static PlatformExtensionsForWindows Instance
		{
			get
			{
				if (PlatformExtensionsForWindows.instance == null)
				{
					PlatformExtensionsForWindows.instance = new PlatformExtensionsForWindows();
				}
				return PlatformExtensionsForWindows.instance;
			}
		}

		// Token: 0x0600060A RID: 1546 RVA: 0x00034559 File Offset: 0x00032759
		public override bool TryMapPortToProcessId(int port, bool includeIPv6, out int processId, out string errorMessage)
		{
			return PortProcessMapperForWindows.TryMapLocalPortToProcessId(port, includeIPv6, out processId, out errorMessage);
		}

		// Token: 0x0600060B RID: 1547 RVA: 0x00034565 File Offset: 0x00032765
		public override bool TryGetListeningProcessOnPort(int port, out string processName, out int processId, out string errorMessage)
		{
			return PortProcessMapperForWindows.TryGetListeningProcess(port, out processName, out processId, out errorMessage);
		}

		// Token: 0x170000F6 RID: 246
		// (get) Token: 0x0600060C RID: 1548 RVA: 0x00034571 File Offset: 0x00032771
		public override bool HighResolutionTimersEnabled
		{
			get
			{
				return TimeResolutionHelperForWindows.EnableHighResolutionTimers;
			}
		}

		// Token: 0x0600060D RID: 1549 RVA: 0x00034578 File Offset: 0x00032778
		public override bool TryChangeTimersResolution(bool increase)
		{
			TimeResolutionHelperForWindows.EnableHighResolutionTimers = increase;
			return TimeResolutionHelperForWindows.EnableHighResolutionTimers == increase;
		}

		// Token: 0x170000F7 RID: 247
		// (get) Token: 0x0600060E RID: 1550 RVA: 0x00034588 File Offset: 0x00032788
		public override IProxyHelper ProxyHelper
		{
			get
			{
				return ProxyHelperForWindows.Instance;
			}
		}

		// Token: 0x170000F8 RID: 248
		// (get) Token: 0x0600060F RID: 1551 RVA: 0x0003458F File Offset: 0x0003278F
		public IWinINetHelper WinINetHelper
		{
			get
			{
				return FiddlerCore.PlatformExtensions.Windows.WinINetHelper.Instance;
			}
		}

		// Token: 0x06000610 RID: 1552 RVA: 0x00034596 File Offset: 0x00032796
		public override IAutoProxy CreateAutoProxy(bool autoDiscover, string pacUrl, bool autoProxyRunInProcess, bool autoLoginIfChallenged)
		{
			return new WinHttpAutoProxy(autoDiscover, pacUrl, autoProxyRunInProcess, autoLoginIfChallenged);
		}

		// Token: 0x06000611 RID: 1553 RVA: 0x000345A2 File Offset: 0x000327A2
		public override byte[] DecompressXpress(byte[] data)
		{
			return XpressCompressionHelperForWindows.Decompress(data);
		}

		// Token: 0x06000612 RID: 1554 RVA: 0x000345AA File Offset: 0x000327AA
		public override string PostProcessProcessName(int pid, string processName)
		{
			return ProcessHelperForWindows.DisambiguateWWAHostApps(pid, processName);
		}

		// Token: 0x06000613 RID: 1555 RVA: 0x000345B3 File Offset: 0x000327B3
		public override void SetUserAgentStringForCurrentProcess(string userAgent)
		{
			UserAgentHelperForWindows.SetUserAgentStringForCurrentProcess(userAgent);
		}

		// Token: 0x06000614 RID: 1556 RVA: 0x000345BB File Offset: 0x000327BB
		public override bool TryGetUptimeInMilliseconds(out ulong milliseconds)
		{
			return UptimeHelperForWindows.TryGetUptimeInMilliseconds(out milliseconds);
		}

		// Token: 0x06000615 RID: 1557 RVA: 0x000345C3 File Offset: 0x000327C3
		public override bool IsRootCertificateTrusted()
		{
			return CertMaker.rootCertIsTrusted() || CertMaker.rootCertIsMachineTrusted();
		}

		// Token: 0x06000616 RID: 1558 RVA: 0x000345D3 File Offset: 0x000327D3
		public override void TrustRootCertificate()
		{
			if (!CertMaker.trustRootCert())
			{
				throw new OperationCanceledException("Unable to trust the root certificate. The operation was cancelled.");
			}
		}

		// Token: 0x06000617 RID: 1559 RVA: 0x000345E7 File Offset: 0x000327E7
		public override void UntrustRootCertificate()
		{
			if (!CertMaker.removeFiddlerGeneratedCerts())
			{
				throw new OperationCanceledException("Unable to remove the root certificate. The operation was cancelled.");
			}
		}

		// Token: 0x06000618 RID: 1560 RVA: 0x000345FB File Offset: 0x000327FB
		public override int GetParentProcessId(int childProcessId, out string errorMessage)
		{
			return ProcessHelperForWindows.GetParentProcessId(childProcessId, out errorMessage);
		}

		// Token: 0x040002AD RID: 685
		private static PlatformExtensionsForWindows instance;
	}
}
