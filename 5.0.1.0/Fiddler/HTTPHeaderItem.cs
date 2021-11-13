using System;

namespace Fiddler
{
	/// <summary>
	/// Represents a single HTTP header
	/// </summary>
	// Token: 0x02000043 RID: 67
	public class HTTPHeaderItem : ICloneable
	{
		/// <summary>
		/// Clones a single HTTP header and returns the clone cast to an object
		/// </summary>
		/// <returns>HTTPHeader Name: Value pair, cast to an object</returns>
		// Token: 0x060002BD RID: 701 RVA: 0x00018A19 File Offset: 0x00016C19
		public object Clone()
		{
			return base.MemberwiseClone();
		}

		/// <summary>
		/// Creates a new HTTP Header item. WARNING: Doesn't do any trimming or validation on the name.
		/// </summary>
		/// <param name="sName">Header name</param>
		/// <param name="sValue">Header value</param>
		// Token: 0x060002BE RID: 702 RVA: 0x00018A21 File Offset: 0x00016C21
		public HTTPHeaderItem(string sName, string sValue)
		{
			if (string.IsNullOrEmpty(sName))
			{
				sName = string.Empty;
			}
			if (sValue == null)
			{
				sValue = string.Empty;
			}
			this.Name = sName;
			this.Value = sValue;
		}

		/// <summary>
		/// Return a string of the form "NAME: VALUE"
		/// </summary>
		/// <returns>"NAME: VALUE" Header string</returns>
		// Token: 0x060002BF RID: 703 RVA: 0x00018A50 File Offset: 0x00016C50
		public override string ToString()
		{
			return string.Format("{0}: {1}", this.Name, this.Value);
		}

		/// <summary>
		/// The name of the HTTP header
		/// </summary>
		// Token: 0x0400012B RID: 299
		[CodeDescription("String name of the HTTP header.")]
		public string Name;

		/// <summary>
		/// The value of the HTTP header
		/// </summary>
		// Token: 0x0400012C RID: 300
		[CodeDescription("String value of the HTTP header.")]
		public string Value;
	}
}
