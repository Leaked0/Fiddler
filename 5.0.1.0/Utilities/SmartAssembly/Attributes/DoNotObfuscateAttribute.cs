using System;

namespace FiddlerCore.Utilities.SmartAssembly.Attributes
{
	// Token: 0x0200007D RID: 125
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Delegate)]
	public sealed class DoNotObfuscateAttribute : Attribute
	{
	}
}
