using System;
using System.IO;
using System.Text;

namespace Fiddler
{
	/// <summary>
	/// A WebSocketMessage stores a single frame of a single WebSocket message
	/// http://tools.ietf.org/html/rfc6455
	/// </summary>
	// Token: 0x02000070 RID: 112
	public class WebSocketMessage
	{
		// Token: 0x06000586 RID: 1414 RVA: 0x00032FE8 File Offset: 0x000311E8
		internal void SetBitFlags(WSMFlags oF)
		{
			this.BitFlags = oF;
		}

		// Token: 0x170000E5 RID: 229
		// (get) Token: 0x06000587 RID: 1415 RVA: 0x00032FF1 File Offset: 0x000311F1
		public bool IsFinalFrame
		{
			get
			{
				return this._bIsFinalFragment;
			}
		}

		// Token: 0x06000588 RID: 1416 RVA: 0x00032FF9 File Offset: 0x000311F9
		internal void AssignHeader(byte byteHeader)
		{
			this._bIsFinalFragment = 128 == (byteHeader & 128);
			this._byteReservedFlags = (byte)((byteHeader & 112) >> 4);
			this.FrameType = (WebSocketFrameTypes)(byteHeader & 15);
		}

		// Token: 0x170000E6 RID: 230
		// (get) Token: 0x06000589 RID: 1417 RVA: 0x00033027 File Offset: 0x00031227
		[CodeDescription("Indicates whether this WebSocketMessage was aborted.")]
		public bool WasAborted
		{
			get
			{
				return (this.BitFlags & WSMFlags.Aborted) == WSMFlags.Aborted;
			}
		}

		// Token: 0x0600058A RID: 1418 RVA: 0x00033034 File Offset: 0x00031234
		[CodeDescription("Cancel transmission of this WebSocketMessage.")]
		public void Abort()
		{
			this.BitFlags |= WSMFlags.Aborted;
		}

		// Token: 0x0600058B RID: 1419 RVA: 0x00033044 File Offset: 0x00031244
		[CodeDescription("Returns the entire WebSocketMessage, including headers.")]
		public byte[] ToByteArray()
		{
			if (this._arrRawPayload == null)
			{
				return Utilities.emptyByteArray;
			}
			MemoryStream oMS = new MemoryStream();
			oMS.WriteByte((byte)((this._bIsFinalFragment ? ((WebSocketFrameTypes)128) : WebSocketFrameTypes.Continuation) | (WebSocketFrameTypes)(this._byteReservedFlags << 4) | this.FrameType));
			ulong ulPayloadLen = (ulong)((long)this._arrRawPayload.Length);
			byte[] arrSize;
			if (this._arrRawPayload.Length < 126)
			{
				arrSize = new byte[] { (byte)this._arrRawPayload.Length };
			}
			else if (this._arrRawPayload.Length < 65535)
			{
				arrSize = new byte[]
				{
					126,
					(byte)(ulPayloadLen >> 8),
					(byte)(ulPayloadLen & 255UL)
				};
			}
			else
			{
				arrSize = new byte[]
				{
					127,
					(byte)(ulPayloadLen >> 56),
					(byte)((ulPayloadLen & 71776119061217280UL) >> 48),
					(byte)((ulPayloadLen & 280375465082880UL) >> 40),
					(byte)((ulPayloadLen & 1095216660480UL) >> 32),
					(byte)((ulPayloadLen & (ulong)(-16777216)) >> 24),
					(byte)((ulPayloadLen & 16711680UL) >> 16),
					(byte)((ulPayloadLen & 65280UL) >> 8),
					(byte)(ulPayloadLen & 255UL)
				};
			}
			if (this._arrMask != null)
			{
				byte[] array = arrSize;
				int num = 0;
				array[num] |= 128;
			}
			oMS.Write(arrSize, 0, arrSize.Length);
			if (this._arrMask != null)
			{
				oMS.Write(this._arrMask, 0, 4);
			}
			oMS.Write(this._arrRawPayload, 0, this._arrRawPayload.Length);
			return oMS.ToArray();
		}

		// Token: 0x0600058C RID: 1420 RVA: 0x000331C7 File Offset: 0x000313C7
		internal WebSocketMessage(WebSocket oWSOwner, int iID, bool bIsOutbound)
		{
			this._wsOwner = oWSOwner;
			this._iID = iID;
			this._bOutbound = bIsOutbound;
		}

		// Token: 0x0600058D RID: 1421 RVA: 0x000331F0 File Offset: 0x000313F0
		[CodeDescription("Returns all info about this message.")]
		public override string ToString()
		{
			return string.Format("WS{0}\nMessageID:\t{1}.{2}\nMessageType:\t{3}\nPayloadString:\t{4}\nMasking:\t{5}\n", new object[]
			{
				this._wsOwner.ToString(),
				this._bOutbound ? "Client" : "Server",
				this._iID,
				this._wsftType,
				this.PayloadAsString(),
				(this._arrMask == null) ? "<none>" : BitConverter.ToString(this._arrMask)
			});
		}

		/// <summary>
		/// Unmasks the first array into the third, using the second as a masking key.
		/// </summary>
		/// <param name="arrIn"></param>
		/// <param name="arrKey"></param>
		/// <param name="arrOut"></param>
		// Token: 0x0600058E RID: 1422 RVA: 0x00033274 File Offset: 0x00031474
		private static void UnmaskData(byte[] arrIn, byte[] arrKey, byte[] arrOut)
		{
			if (Utilities.IsNullOrEmpty(arrKey))
			{
				Buffer.BlockCopy(arrIn, 0, arrOut, 0, arrIn.Length);
				return;
			}
			for (int idx = 0; idx < arrIn.Length; idx++)
			{
				arrOut[idx] = arrIn[idx] ^ arrKey[idx % 4];
			}
		}

		/// <summary>
		/// Masks the first array's data using the key in the second
		/// </summary>
		/// <param name="arrInOut">The data to be masked</param>
		/// <param name="arrKey">A 4-byte obfuscation key, or null.</param>
		// Token: 0x0600058F RID: 1423 RVA: 0x000332B4 File Offset: 0x000314B4
		private static void MaskDataInPlace(byte[] arrInOut, byte[] arrKey)
		{
			if (arrKey == null)
			{
				return;
			}
			for (int idx = 0; idx < arrInOut.Length; idx++)
			{
				arrInOut[idx] ^= arrKey[idx % 4];
			}
		}

		/// <summary>
		/// Replaces the WebSocketMessage's payload with the specified string, masking if needed.
		/// </summary>
		/// <param name="sPayload"></param>
		// Token: 0x06000590 RID: 1424 RVA: 0x000332E0 File Offset: 0x000314E0
		[CodeDescription("Replaces the WebSocketMessage's payload with the specified string, masking if needed.")]
		public void SetPayload(string sPayload)
		{
			this._SetPayloadWithoutCopy(Encoding.UTF8.GetBytes(sPayload));
		}

		/// <summary>
		/// Copies the provided byte array over the WebSocketMessage's payload, masking if needed.
		/// </summary>
		/// <param name="arrNewPayload"></param>
		// Token: 0x06000591 RID: 1425 RVA: 0x000332F4 File Offset: 0x000314F4
		[CodeDescription("Replaces the WebSocketMessage's payload with the specified byte array, masking if needed.")]
		public void SetPayload(byte[] arrNewPayload)
		{
			byte[] arrCopy = new byte[arrNewPayload.Length];
			Buffer.BlockCopy(arrNewPayload, 0, arrCopy, 0, arrNewPayload.Length);
			this._SetPayloadWithoutCopy(arrCopy);
		}

		/// <summary>
		/// Masks the provided array (if necessary) and assigns it to the WebSocketMessage's payload.
		/// </summary>
		/// <param name="arrNewPayload">New array of data</param>
		// Token: 0x06000592 RID: 1426 RVA: 0x0003331D File Offset: 0x0003151D
		private void _SetPayloadWithoutCopy(byte[] arrNewPayload)
		{
			WebSocketMessage.MaskDataInPlace(arrNewPayload, this._arrMask);
			this._arrRawPayload = arrNewPayload;
		}

		/// <summary>
		/// Return the WebSocketMessage's payload as a string.
		/// </summary>
		/// <returns></returns>
		// Token: 0x06000593 RID: 1427 RVA: 0x00033334 File Offset: 0x00031534
		[CodeDescription("Returns the WebSocketMessage's payload as a string, unmasking if needed.")]
		public string PayloadAsString()
		{
			if (this._arrRawPayload == null)
			{
				return "<NoPayload>";
			}
			byte[] arrUnmaskedPayload;
			if (this._arrMask != null)
			{
				arrUnmaskedPayload = new byte[this._arrRawPayload.Length];
				WebSocketMessage.UnmaskData(this._arrRawPayload, this._arrMask, arrUnmaskedPayload);
			}
			else
			{
				arrUnmaskedPayload = this._arrRawPayload;
			}
			if (this._wsftType == WebSocketFrameTypes.Text)
			{
				return Encoding.UTF8.GetString(arrUnmaskedPayload);
			}
			return BitConverter.ToString(arrUnmaskedPayload);
		}

		/// <summary>
		/// Copy the WebSocketMessage's payload into a new Byte Array.
		/// </summary>
		/// <returns>A new byte array containing the (unmasked) payload.</returns>
		// Token: 0x06000594 RID: 1428 RVA: 0x0003339C File Offset: 0x0003159C
		[CodeDescription("Returns the WebSocketMessage's payload as byte[], unmasking if needed.")]
		public byte[] PayloadAsBytes()
		{
			if (this._arrRawPayload == null)
			{
				return Utilities.emptyByteArray;
			}
			byte[] arrUnmaskedPayload = new byte[this._arrRawPayload.Length];
			if (this._arrMask != null)
			{
				WebSocketMessage.UnmaskData(this._arrRawPayload, this._arrMask, arrUnmaskedPayload);
			}
			else
			{
				Buffer.BlockCopy(this._arrRawPayload, 0, arrUnmaskedPayload, 0, arrUnmaskedPayload.Length);
			}
			return arrUnmaskedPayload;
		}

		// Token: 0x170000E7 RID: 231
		// (get) Token: 0x06000595 RID: 1429 RVA: 0x000333F3 File Offset: 0x000315F3
		[CodeDescription("Returns TRUE if this is a Client->Server message, FALSE if this is a message from Server->Client.")]
		public bool IsOutbound
		{
			get
			{
				return this._bOutbound;
			}
		}

		// Token: 0x170000E8 RID: 232
		// (get) Token: 0x06000596 RID: 1430 RVA: 0x000333FB File Offset: 0x000315FB
		public int ID
		{
			get
			{
				return this._iID;
			}
		}

		// Token: 0x170000E9 RID: 233
		// (get) Token: 0x06000597 RID: 1431 RVA: 0x00033403 File Offset: 0x00031603
		public int PayloadLength
		{
			get
			{
				if (this._arrRawPayload == null)
				{
					return 0;
				}
				return this._arrRawPayload.Length;
			}
		}

		// Token: 0x170000EA RID: 234
		// (get) Token: 0x06000598 RID: 1432 RVA: 0x00033417 File Offset: 0x00031617
		// (set) Token: 0x06000599 RID: 1433 RVA: 0x0003341F File Offset: 0x0003161F
		[CodeDescription("Returns the raw payload data, which may be masked.")]
		public byte[] PayloadData
		{
			get
			{
				return this._arrRawPayload;
			}
			internal set
			{
				this._arrRawPayload = value;
			}
		}

		// Token: 0x170000EB RID: 235
		// (get) Token: 0x0600059A RID: 1434 RVA: 0x00033428 File Offset: 0x00031628
		// (set) Token: 0x0600059B RID: 1435 RVA: 0x00033430 File Offset: 0x00031630
		[CodeDescription("Returns the WebSocketMessage's masking key, if any.")]
		public byte[] MaskingKey
		{
			get
			{
				return this._arrMask;
			}
			internal set
			{
				this._arrMask = value;
			}
		}

		// Token: 0x170000EC RID: 236
		// (get) Token: 0x0600059C RID: 1436 RVA: 0x00033439 File Offset: 0x00031639
		// (set) Token: 0x0600059D RID: 1437 RVA: 0x00033441 File Offset: 0x00031641
		public WebSocketFrameTypes FrameType
		{
			get
			{
				return this._wsftType;
			}
			internal set
			{
				this._wsftType = value;
			}
		}

		// Token: 0x170000ED RID: 237
		// (get) Token: 0x0600059E RID: 1438 RVA: 0x0003344C File Offset: 0x0003164C
		[CodeDescription("If this is a Close frame, returns the close code. Otherwise, returns -1.")]
		public int iCloseReason
		{
			get
			{
				if (this.FrameType != WebSocketFrameTypes.Close || this._arrRawPayload == null || this._arrRawPayload.Length < 2)
				{
					return -1;
				}
				byte[] arrCloseReason = new byte[]
				{
					this._arrRawPayload[0],
					this._arrRawPayload[1]
				};
				WebSocketMessage.UnmaskData(arrCloseReason, this._arrMask, arrCloseReason);
				return ((int)arrCloseReason[0] << 8) + (int)arrCloseReason[1];
			}
		}

		/// <summary>
		/// Serialize this message to a stream
		/// </summary>
		/// <param name="oFS"></param>
		// Token: 0x0600059F RID: 1439 RVA: 0x000334AC File Offset: 0x000316AC
		internal void SerializeToStream(Stream oFS)
		{
			byte[] arrMessage = this.ToByteArray();
			string sTimers = this.Timers.ToHeaderString();
			string sHeaders = string.Format("{0}: {1}\r\nID: {2}\r\nBitFlags: {3}\r\n{4}\r\n", new object[]
			{
				this.IsOutbound ? "Request-Length" : "Response-Length",
				arrMessage.Length,
				this.ID,
				(int)this.BitFlags,
				sTimers
			});
			byte[] arrHeaders = Encoding.ASCII.GetBytes(sHeaders);
			oFS.Write(arrHeaders, 0, arrHeaders.Length);
			oFS.Write(arrMessage, 0, arrMessage.Length);
			oFS.WriteByte(13);
			oFS.WriteByte(10);
		}

		/// <summary>
		/// Add the content of the subequent continuation to me.
		/// </summary>
		/// <param name="oWSM"></param>
		// Token: 0x060005A0 RID: 1440 RVA: 0x00033554 File Offset: 0x00031754
		internal void Assemble(WebSocketMessage oWSM)
		{
			this.BitFlags |= WSMFlags.Assembled;
			MemoryStream oMS = new MemoryStream();
			byte[] arrMine = this.PayloadAsBytes();
			oMS.Write(arrMine, 0, arrMine.Length);
			byte[] arrNext = oWSM.PayloadAsBytes();
			oMS.Write(arrNext, 0, arrNext.Length);
			this.SetPayload(oMS.ToArray());
			if (oWSM.IsFinalFrame)
			{
				this._bIsFinalFragment = true;
			}
			this.Timers.dtDoneSend = oWSM.Timers.dtDoneSend;
		}

		// Token: 0x04000271 RID: 625
		private WSMFlags BitFlags;

		// Token: 0x04000272 RID: 626
		private WebSocket _wsOwner;

		// Token: 0x04000273 RID: 627
		private bool _bIsFinalFragment;

		/// <summary>
		/// 3 bits frame-rsv1,frame-rsv2,frame-rsv3
		/// </summary>
		// Token: 0x04000274 RID: 628
		private byte _byteReservedFlags;

		/// <summary>
		/// Is this a Request message?
		/// </summary>
		// Token: 0x04000275 RID: 629
		private bool _bOutbound;

		// Token: 0x04000276 RID: 630
		private int _iID;

		/// <summary>
		/// The WebSocketTimers collection tracks the timestamps for this message
		/// </summary>
		// Token: 0x04000277 RID: 631
		public WebSocketTimers Timers = new WebSocketTimers();

		/// <summary>
		/// The raw payload data, which may be masked.
		/// </summary>
		// Token: 0x04000278 RID: 632
		private byte[] _arrRawPayload;

		/// <summary>
		/// The four-byte payload masking key, if any
		/// </summary>
		// Token: 0x04000279 RID: 633
		private byte[] _arrMask;

		/// <summary>
		/// The type of the WebSocket Message's frame
		/// </summary>
		// Token: 0x0400027A RID: 634
		private WebSocketFrameTypes _wsftType;
	}
}
