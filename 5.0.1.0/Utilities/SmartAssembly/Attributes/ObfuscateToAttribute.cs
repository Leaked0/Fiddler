using System;

namespace FiddlerCore.Utilities.SmartAssembly.Attributes
{
	// Token: 0x02000086 RID: 134
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Interface)]
	public sealed class ObfuscateToAttribute : Attribute
	{
		// Token: 0x060005D3 RID: 1491 RVA: 0x0003422C File Offset: 0x0003242C
		public ObfuscateToAttribute(string newName)
		{
		}
	}
}
