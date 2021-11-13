using System;

namespace FiddlerCore.Analytics
{
	// Token: 0x020000B7 RID: 183
	internal sealed class EmptyAnalytics : IAnalytics
	{
		// Token: 0x060006D5 RID: 1749 RVA: 0x00037836 File Offset: 0x00035A36
		public void Start()
		{
		}

		// Token: 0x060006D6 RID: 1750 RVA: 0x00037838 File Offset: 0x00035A38
		public void Stop()
		{
		}

		// Token: 0x060006D7 RID: 1751 RVA: 0x0003783A File Offset: 0x00035A3A
		public void TrackException(Exception exception)
		{
		}

		// Token: 0x060006D8 RID: 1752 RVA: 0x0003783C File Offset: 0x00035A3C
		public void TrackFeature(string category, string eventAction, string label = null)
		{
		}

		// Token: 0x060006D9 RID: 1753 RVA: 0x0003783E File Offset: 0x00035A3E
		public void TrackFeatureValue(string category, string eventAction, string label, long value)
		{
		}
	}
}
