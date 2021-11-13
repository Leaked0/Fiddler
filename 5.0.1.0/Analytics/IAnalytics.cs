using System;
using FiddlerCore.Utilities.SmartAssembly.Attributes;

namespace FiddlerCore.Analytics
{
	// Token: 0x020000B8 RID: 184
	[DoNotObfuscateType]
	internal interface IAnalytics
	{
		// Token: 0x060006DB RID: 1755
		void TrackFeature(string category, string eventAction, string label = null);

		// Token: 0x060006DC RID: 1756
		void TrackFeatureValue(string category, string eventAction, string label, long value);

		// Token: 0x060006DD RID: 1757
		void TrackException(Exception exception);

		// Token: 0x060006DE RID: 1758
		void Start();

		// Token: 0x060006DF RID: 1759
		void Stop();
	}
}
