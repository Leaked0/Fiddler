using System;

namespace FiddlerCore.Utilities.SmartAssembly.Attributes
{
	// Token: 0x02000083 RID: 131
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
	public class ReportUsageAttribute : Attribute
	{
		// Token: 0x060005CF RID: 1487 RVA: 0x0003420C File Offset: 0x0003240C
		public ReportUsageAttribute()
		{
		}

		// Token: 0x060005D0 RID: 1488 RVA: 0x00034214 File Offset: 0x00032414
		public ReportUsageAttribute(string featureName)
		{
		}
	}
}
