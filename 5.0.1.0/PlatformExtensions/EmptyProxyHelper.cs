using System;
using FiddlerCore.PlatformExtensions.API;

namespace FiddlerCore.PlatformExtensions
{
	// Token: 0x02000090 RID: 144
	internal class EmptyProxyHelper : IProxyHelper
	{
		// Token: 0x060005FF RID: 1535 RVA: 0x0003447B File Offset: 0x0003267B
		private EmptyProxyHelper()
		{
		}

		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x06000600 RID: 1536 RVA: 0x00034483 File Offset: 0x00032683
		public static EmptyProxyHelper Instance
		{
			get
			{
				if (EmptyProxyHelper.instance == null)
				{
					EmptyProxyHelper.instance = new EmptyProxyHelper();
				}
				return EmptyProxyHelper.instance;
			}
		}

		// Token: 0x06000601 RID: 1537 RVA: 0x0003449B File Offset: 0x0003269B
		public void DisableProxyForCurrentProcess()
		{
			throw new NotSupportedException("This method is not supported on your platform.");
		}

		// Token: 0x06000602 RID: 1538 RVA: 0x000344A7 File Offset: 0x000326A7
		public string GetProxyForCurrentProcessAsHexView()
		{
			throw new NotSupportedException("This method is not supported on your platform.");
		}

		// Token: 0x06000603 RID: 1539 RVA: 0x000344B3 File Offset: 0x000326B3
		public void ResetProxyForCurrentProcess()
		{
			throw new NotSupportedException("This method is not supported on your platform.");
		}

		// Token: 0x06000604 RID: 1540 RVA: 0x000344BF File Offset: 0x000326BF
		public void SetProxyForCurrentProcess(string proxy, string bypassList)
		{
			throw new NotSupportedException("This method is not supported on your platform.");
		}

		// Token: 0x040002AA RID: 682
		private static EmptyProxyHelper instance;
	}
}
