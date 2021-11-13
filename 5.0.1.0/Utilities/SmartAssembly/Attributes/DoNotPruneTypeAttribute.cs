using System;

namespace FiddlerCore.Utilities.SmartAssembly.Attributes
{
	// Token: 0x02000080 RID: 128
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface)]
	public sealed class DoNotPruneTypeAttribute : Attribute
	{
	}
}
