using System;

namespace Fiddler
{
	/// <summary>
	/// When the FiddlerApplication.OnReadResponseBuffer event fires, the raw bytes are available via this object.
	/// </summary>
	// Token: 0x0200003C RID: 60
	public class RawReadEventArgs : EventArgs
	{
		/// <summary>
		/// Set to TRUE to request that upload or download process be aborted as soon as convenient
		/// </summary>
		// Token: 0x17000072 RID: 114
		// (get) Token: 0x0600025C RID: 604 RVA: 0x000164B5 File Offset: 0x000146B5
		// (set) Token: 0x0600025D RID: 605 RVA: 0x000164BD File Offset: 0x000146BD
		public bool AbortReading { get; set; }

		// Token: 0x0600025E RID: 606 RVA: 0x000164C6 File Offset: 0x000146C6
		internal RawReadEventArgs(Session oS, byte[] arrData, int iCountBytes)
		{
			this._arrData = arrData;
			this._iCountBytes = iCountBytes;
			this._oS = oS;
		}

		/// <summary>
		/// Session for which this responseRead is occurring
		/// </summary>
		// Token: 0x17000073 RID: 115
		// (get) Token: 0x0600025F RID: 607 RVA: 0x000164E3 File Offset: 0x000146E3
		public Session sessionOwner
		{
			get
			{
				return this._oS;
			}
		}

		/// <summary>
		/// Byte buffer returned from read. Note: Always of fixed size, check iCountOfBytes to see which bytes were set
		/// </summary>
		// Token: 0x17000074 RID: 116
		// (get) Token: 0x06000260 RID: 608 RVA: 0x000164EB File Offset: 0x000146EB
		public byte[] arrDataBuffer
		{
			get
			{
				return this._arrData;
			}
		}

		/// <summary>
		/// Count of latest read from Socket. If less than 1, response was ended.
		/// </summary>
		// Token: 0x17000075 RID: 117
		// (get) Token: 0x06000261 RID: 609 RVA: 0x000164F3 File Offset: 0x000146F3
		public int iCountOfBytes
		{
			get
			{
				return this._iCountBytes;
			}
		}

		// Token: 0x0400010D RID: 269
		private readonly byte[] _arrData;

		// Token: 0x0400010E RID: 270
		private readonly int _iCountBytes;

		// Token: 0x0400010F RID: 271
		private readonly Session _oS;
	}
}
