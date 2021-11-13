using System;

namespace FiddlerCore.Analytics
{
	// Token: 0x020000B6 RID: 182
	internal sealed class AnalyticsFactory
	{
		// Token: 0x060006D1 RID: 1745 RVA: 0x00037808 File Offset: 0x00035A08
		private AnalyticsFactory()
		{
			this.analytics = new EmptyAnalytics();
		}

		// Token: 0x17000118 RID: 280
		// (get) Token: 0x060006D2 RID: 1746 RVA: 0x0003781B File Offset: 0x00035A1B
		internal static AnalyticsFactory Instance
		{
			get
			{
				return AnalyticsFactory.instance;
			}
		}

		// Token: 0x060006D3 RID: 1747 RVA: 0x00037822 File Offset: 0x00035A22
		internal IAnalytics GetAnalytics()
		{
			return this.analytics;
		}

		// Token: 0x0400031E RID: 798
		private static readonly AnalyticsFactory instance = new AnalyticsFactory();

		// Token: 0x0400031F RID: 799
		private readonly IAnalytics analytics;
	}
}
