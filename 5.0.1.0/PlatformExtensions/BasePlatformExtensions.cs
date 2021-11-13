using System;
using FiddlerCore.PlatformExtensions.API;

namespace FiddlerCore.PlatformExtensions
{
	// Token: 0x0200008E RID: 142
	internal abstract class BasePlatformExtensions : IPlatformExtensions
	{
		// Token: 0x170000EF RID: 239
		// (get) Token: 0x060005DB RID: 1499
		public abstract bool HighResolutionTimersEnabled { get; }

		// Token: 0x170000F0 RID: 240
		// (get) Token: 0x060005DC RID: 1500
		public abstract IProxyHelper ProxyHelper { get; }

		// Token: 0x14000018 RID: 24
		// (add) Token: 0x060005DD RID: 1501 RVA: 0x0003426C File Offset: 0x0003246C
		// (remove) Token: 0x060005DE RID: 1502 RVA: 0x000342A4 File Offset: 0x000324A4
		public event EventHandler<MessageEventArgs> DebugSpew;

		// Token: 0x14000019 RID: 25
		// (add) Token: 0x060005DF RID: 1503 RVA: 0x000342DC File Offset: 0x000324DC
		// (remove) Token: 0x060005E0 RID: 1504 RVA: 0x00034314 File Offset: 0x00032514
		public event EventHandler<MessageEventArgs> Error;

		// Token: 0x1400001A RID: 26
		// (add) Token: 0x060005E1 RID: 1505 RVA: 0x0003434C File Offset: 0x0003254C
		// (remove) Token: 0x060005E2 RID: 1506 RVA: 0x00034384 File Offset: 0x00032584
		public event EventHandler<MessageEventArgs> Log;

		// Token: 0x060005E3 RID: 1507 RVA: 0x000343B9 File Offset: 0x000325B9
		public virtual void TrustRootCertificate()
		{
			throw new NotImplementedException();
		}

		// Token: 0x060005E4 RID: 1508 RVA: 0x000343C0 File Offset: 0x000325C0
		public virtual void UntrustRootCertificate()
		{
			throw new NotImplementedException();
		}

		// Token: 0x060005E5 RID: 1509 RVA: 0x000343C7 File Offset: 0x000325C7
		public virtual bool IsRootCertificateTrusted()
		{
			throw new NotImplementedException();
		}

		// Token: 0x060005E6 RID: 1510
		public abstract IAutoProxy CreateAutoProxy(bool autoDiscover, string pacUrl, bool autoProxyRunInProcess, bool autoLoginIfChallenged);

		// Token: 0x060005E7 RID: 1511
		public abstract byte[] DecompressXpress(byte[] data);

		// Token: 0x060005E8 RID: 1512
		public abstract string PostProcessProcessName(int pid, string processName);

		// Token: 0x060005E9 RID: 1513
		public abstract void SetUserAgentStringForCurrentProcess(string userAgent);

		// Token: 0x060005EA RID: 1514
		public abstract bool TryChangeTimersResolution(bool increase);

		// Token: 0x060005EB RID: 1515
		public abstract bool TryGetUptimeInMilliseconds(out ulong milliseconds);

		// Token: 0x060005EC RID: 1516
		public abstract bool TryGetListeningProcessOnPort(int port, out string processName, out int processId, out string errorMessage);

		// Token: 0x060005ED RID: 1517
		public abstract bool TryMapPortToProcessId(int port, bool includeIPv6, out int processId, out string errorMessage);

		// Token: 0x060005EE RID: 1518
		public abstract int GetParentProcessId(int childProcessId, out string errorMessage);

		// Token: 0x060005EF RID: 1519 RVA: 0x000343CE File Offset: 0x000325CE
		internal void OnDebugSpew(string message)
		{
			this.OnMessageEvent(this.DebugSpew, message);
		}

		// Token: 0x060005F0 RID: 1520 RVA: 0x000343DD File Offset: 0x000325DD
		internal void OnError(string message)
		{
			this.OnMessageEvent(this.Error, message);
		}

		// Token: 0x060005F1 RID: 1521 RVA: 0x000343EC File Offset: 0x000325EC
		internal void OnLog(string message)
		{
			this.OnMessageEvent(this.Log, message);
		}

		// Token: 0x060005F2 RID: 1522 RVA: 0x000343FB File Offset: 0x000325FB
		private void OnMessageEvent(EventHandler<MessageEventArgs> handler, string message)
		{
			if (handler != null)
			{
				handler(this, new MessageEventArgs(message));
			}
		}
	}
}
