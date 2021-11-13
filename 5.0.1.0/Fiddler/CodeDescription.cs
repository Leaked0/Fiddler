using System;

namespace Fiddler
{
	/// <summary>
	/// CodeDescription attributes are used to enable the FiddlerScript Editor to describe available methods, properties, fields, and events.
	/// </summary>
	// Token: 0x02000031 RID: 49
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event, Inherited = false, AllowMultiple = false)]
	public sealed class CodeDescription : Attribute
	{
		/// <summary>
		/// CodeDescription attributes should be constructed by annotating a property, method, or field.
		/// </summary>
		/// <param name="desc">The descriptive string which should be displayed for this this property, method, or field</param>
		// Token: 0x060001EB RID: 491 RVA: 0x0001516F File Offset: 0x0001336F
		public CodeDescription(string desc)
		{
			this.sDesc = desc;
		}

		/// <summary>
		/// The descriptive string which should be displayed for this this property, method, or field
		/// </summary>
		// Token: 0x17000060 RID: 96
		// (get) Token: 0x060001EC RID: 492 RVA: 0x0001517E File Offset: 0x0001337E
		public string Description
		{
			get
			{
				return this.sDesc;
			}
		}

		// Token: 0x040000D8 RID: 216
		private string sDesc;
	}
}
