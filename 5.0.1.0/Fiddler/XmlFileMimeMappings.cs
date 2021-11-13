using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Fiddler
{
	/// <summary>
	/// This class is used to deserialize and store MIME-type-to-file-extension mappings from given XML file.
	/// </summary>
	/// <remarks>
	/// The XML file should be in the following format:
	/// <![CDATA[
	///
	/// <ArrayOfMimeMap>
	///   <MimeMap>
	///     <MimeType>mime/type</MimeType>
	///     <FileExtension>.ext</FileExtension>
	///   </MimeMap>
	/// </ArrayOfMimeMap>
	///
	/// ]]>
	/// </remarks>
	// Token: 0x0200000E RID: 14
	public class XmlFileMimeMappings : IEnumerable<MimeMap>, IEnumerable
	{
		/// <summary>
		/// Initializes new instance of <typeparamref name="XmlFileMimeMappings" /> with the specified file path.
		/// </summary>
		/// <param name="filePath">A relative or absolute path to the XML file.</param>
		// Token: 0x060000D7 RID: 215 RVA: 0x0000E7F0 File Offset: 0x0000C9F0
		public XmlFileMimeMappings(string filePath)
		{
			this.mappings = new List<MimeMap>();
			FileStream xmlFile = new FileStream(filePath, FileMode.Open);
			using (xmlFile)
			{
				XmlSerializer serializer = new XmlSerializer(typeof(List<MimeMap>));
				List<MimeMap> fileMappings = serializer.Deserialize(xmlFile) as List<MimeMap>;
				this.mappings = fileMappings;
			}
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x0000E858 File Offset: 0x0000CA58
		public IEnumerator<MimeMap> GetEnumerator()
		{
			return this.mappings.GetEnumerator();
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x0000E86A File Offset: 0x0000CA6A
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// Token: 0x0400003F RID: 63
		private List<MimeMap> mappings;
	}
}
