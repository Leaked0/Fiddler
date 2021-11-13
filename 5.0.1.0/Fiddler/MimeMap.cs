using System;

namespace Fiddler
{
	/// <summary>
	/// The class that is used to store MIME-type-to-file-extension mapping.
	/// </summary>
	// Token: 0x0200000C RID: 12
	public class MimeMap
	{
		/// <summary>
		/// Gets or sets the MIME type for this mapping. The provided MIME type should be in the format "top-level type name / subtype name"
		/// and should not include the parameters section of the MIME type. E.g. application/json, text/html, image/gif etc. This property
		/// should not be null, empty string or string containing only white spaces, in order Telerik FiddlerCore to load it.
		/// </summary>
		// Token: 0x17000026 RID: 38
		// (get) Token: 0x060000CA RID: 202 RVA: 0x0000785A File Offset: 0x00005A5A
		// (set) Token: 0x060000CB RID: 203 RVA: 0x00007862 File Offset: 0x00005A62
		public string MimeType { get; set; }

		/// <summary>
		/// Gets or sets the file extension for this mapping. The provided file extension should start with . (dot). E.g. .txt, .html, .png etc.
		/// This property should not be null, empty string or string containing only white spaces, in order Telerik FiddlerCore to load it.
		/// </summary>
		// Token: 0x17000027 RID: 39
		// (get) Token: 0x060000CC RID: 204 RVA: 0x0000786B File Offset: 0x00005A6B
		// (set) Token: 0x060000CD RID: 205 RVA: 0x00007873 File Offset: 0x00005A73
		public string FileExtension { get; set; }

		// Token: 0x060000CE RID: 206 RVA: 0x0000787C File Offset: 0x00005A7C
		internal bool IsValid()
		{
			return !string.IsNullOrWhiteSpace(this.MimeType) && !string.IsNullOrWhiteSpace(this.FileExtension);
		}

		// Token: 0x060000CF RID: 207 RVA: 0x0000789B File Offset: 0x00005A9B
		public override string ToString()
		{
			return string.Format("MIME Type: \"{0}\", File extension: \"{1}\"", this.MimeType, this.FileExtension);
		}
	}
}
