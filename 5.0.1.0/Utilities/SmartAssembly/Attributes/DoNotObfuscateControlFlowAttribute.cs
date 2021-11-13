using System;

namespace FiddlerCore.Utilities.SmartAssembly.Attributes
{
	// Token: 0x02000085 RID: 133
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method)]
	public sealed class DoNotObfuscateControlFlowAttribute : Attribute
	{
	}
}
