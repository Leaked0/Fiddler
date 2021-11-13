using System;

namespace FiddlerCore.Utilities.SmartAssembly.Attributes
{
	// Token: 0x02000087 RID: 135
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface)]
	public sealed class ObfuscateNamespaceToAttribute : Attribute
	{
		// Token: 0x060005D4 RID: 1492 RVA: 0x00034234 File Offset: 0x00032434
		public ObfuscateNamespaceToAttribute(string newName)
		{
		}
	}
}
