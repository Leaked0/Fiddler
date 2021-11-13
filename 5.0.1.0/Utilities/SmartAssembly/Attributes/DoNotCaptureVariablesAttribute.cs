using System;

namespace FiddlerCore.Utilities.SmartAssembly.Attributes
{
	// Token: 0x0200007B RID: 123
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method)]
	public sealed class DoNotCaptureVariablesAttribute : Attribute
	{
	}
}
