using System;

namespace FiddlerCore.Utilities.SmartAssembly.Attributes
{
	// Token: 0x02000088 RID: 136
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method)]
	public sealed class DoNotEncodeStringsAttribute : Attribute
	{
	}
}
