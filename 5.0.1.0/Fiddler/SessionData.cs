using System;
using System.IO;
using System.Runtime.Serialization;

namespace Fiddler
{
	/// <summary>
	/// This object holds Session information as a set of four easily-marshalled byte arrays.
	/// It is serializable, which enables  cross-process transfer of this data (as in a drag/drop operation).
	/// (Internally, data is serialized as if it were being stored in a SAZ file)
	/// </summary>
	// Token: 0x0200005B RID: 91
	[Serializable]
	public class SessionData : ISerializable
	{
		/// <summary>
		/// Create a SessionData object. 
		/// Note: Method must run as cheaply as possible, since it runs on all Drag/Dropped sessions within Fiddler itself.
		/// </summary>
		/// <param name="oS"></param>
		// Token: 0x06000493 RID: 1171 RVA: 0x0002D7F0 File Offset: 0x0002B9F0
		public SessionData(Session oS)
		{
			MemoryStream oMS = new MemoryStream();
			oS.WriteRequestToStream(false, true, oMS);
			this.arrRequest = oMS.ToArray();
			oMS = new MemoryStream();
			oS.WriteResponseToStream(oMS, false);
			this.arrResponse = oMS.ToArray();
			oMS = new MemoryStream();
			oS.WriteMetadataToStream(oMS);
			this.arrMetadata = oMS.ToArray();
			oMS = new MemoryStream();
			oS.WriteWebSocketMessagesToStream(oMS);
			this.arrWebSocketMessages = oMS.ToArray();
		}

		// Token: 0x06000494 RID: 1172 RVA: 0x0002D870 File Offset: 0x0002BA70
		public SessionData(SerializationInfo info, StreamingContext ctxt)
		{
			this.arrRequest = (byte[])info.GetValue("Request", typeof(byte[]));
			this.arrResponse = (byte[])info.GetValue("Response", typeof(byte[]));
			this.arrMetadata = (byte[])info.GetValue("Metadata", typeof(byte[]));
			this.arrWebSocketMessages = (byte[])info.GetValue("WSMsgs", typeof(byte[]));
		}

		// Token: 0x06000495 RID: 1173 RVA: 0x0002D904 File Offset: 0x0002BB04
		public virtual void GetObjectData(SerializationInfo info, StreamingContext ctxt)
		{
			info.AddValue("Request", this.arrRequest);
			info.AddValue("Response", this.arrResponse);
			info.AddValue("Metadata", this.arrMetadata);
			info.AddValue("WSMsgs", this.arrWebSocketMessages);
		}

		// Token: 0x040001E5 RID: 485
		public byte[] arrRequest;

		// Token: 0x040001E6 RID: 486
		public byte[] arrResponse;

		// Token: 0x040001E7 RID: 487
		public byte[] arrMetadata;

		// Token: 0x040001E8 RID: 488
		public byte[] arrWebSocketMessages;
	}
}
