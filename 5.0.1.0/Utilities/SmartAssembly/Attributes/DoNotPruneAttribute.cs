using System;

namespace FiddlerCore.Utilities.SmartAssembly.Attributes
{
	// Token: 0x0200007F RID: 127
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Delegate)]
	public sealed class DoNotPruneAttribute : Attribute
	{
	}
}
