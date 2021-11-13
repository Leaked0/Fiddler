using System;

namespace FiddlerCore.Utilities.SmartAssembly.Attributes
{
	// Token: 0x0200007E RID: 126
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface)]
	public sealed class DoNotObfuscateTypeAttribute : Attribute
	{
	}
}
