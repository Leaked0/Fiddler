using System;

namespace FiddlerCore.Utilities.SmartAssembly.Attributes
{
	// Token: 0x0200007C RID: 124
	[DoNotPrune]
	[DoNotObfuscate]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field, Inherited = true)]
	public sealed class DoNotCaptureAttribute : Attribute
	{
	}
}
