using System;
using System.IO;

namespace Fiddler
{
	/// <summary>
	/// Class allows finding the end of a body sent using Transfer-Encoding: Chunked
	/// </summary>
	// Token: 0x0200004C RID: 76
	internal class ChunkReader
	{
		// Token: 0x060002FD RID: 765 RVA: 0x0001CDBA File Offset: 0x0001AFBA
		internal ChunkReader()
		{
			this._state = ChunkedTransferState.ReadStartOfSize;
		}

		// Token: 0x060002FE RID: 766 RVA: 0x0001CDC9 File Offset: 0x0001AFC9
		private int HexValue(byte b)
		{
			if (b >= 48 && b <= 57)
			{
				return (int)(b - 48);
			}
			if (b >= 65 && b <= 70)
			{
				return (int)(10 + (b - 65));
			}
			if (b >= 97 && b <= 102)
			{
				return (int)(10 + (b - 97));
			}
			return -1;
		}

		// Token: 0x060002FF RID: 767 RVA: 0x0001CE00 File Offset: 0x0001B000
		internal ChunkedTransferState pushBytes(byte[] arrData, int iOffset, int iLen)
		{
			while (iLen > 0)
			{
				switch (this._state)
				{
				case ChunkedTransferState.ReadStartOfSize:
				{
					iLen--;
					int iFirstVal = this.HexValue(arrData[iOffset++]);
					if (iFirstVal < 0)
					{
						return this._state = ChunkedTransferState.Malformed;
					}
					if (this._cbRemainingInBlock != 0)
					{
						throw new InvalidDataException("?");
					}
					this._cbRemainingInBlock = iFirstVal;
					this._state = ChunkedTransferState.ReadingSize;
					break;
				}
				case ChunkedTransferState.ReadingSize:
				{
					iLen--;
					byte c = arrData[iOffset++];
					int iVal = this.HexValue(c);
					if (iVal > -1)
					{
						this._cbRemainingInBlock = this._cbRemainingInBlock * 16 + iVal;
					}
					else
					{
						FiddlerApplication.DebugSpew("Reached Non-Size character '0x{0:X}'; block size is {1}", new object[] { c, this._cbRemainingInBlock });
						if (c != 13)
						{
							if (c != 59)
							{
								return this._state = ChunkedTransferState.Malformed;
							}
							this._state = ChunkedTransferState.ReadingChunkExtToCR;
						}
						else
						{
							this._state = ChunkedTransferState.ReadLFAfterChunkHeader;
						}
					}
					break;
				}
				case ChunkedTransferState.ReadingChunkExtToCR:
					for (;;)
					{
						iLen--;
						if (arrData[iOffset++] == 13)
						{
							break;
						}
						if (iLen <= 0)
						{
							goto Block_8;
						}
					}
					this._state = ChunkedTransferState.ReadLFAfterChunkHeader;
					Block_8:
					break;
				case ChunkedTransferState.ReadLFAfterChunkHeader:
					iLen--;
					if (arrData[iOffset++] != 10)
					{
						return this._state = ChunkedTransferState.Malformed;
					}
					this._state = ((this._cbRemainingInBlock == 0) ? ChunkedTransferState.ReadStartOfTrailer : ChunkedTransferState.ReadingBlock);
					break;
				case ChunkedTransferState.ReadingBlock:
					if (this._cbRemainingInBlock > iLen)
					{
						this._cbRemainingInBlock -= iLen;
						this._iEntityLength += iLen;
						return ChunkedTransferState.ReadingBlock;
					}
					if (this._cbRemainingInBlock == iLen)
					{
						this._cbRemainingInBlock = 0;
						this._iEntityLength += iLen;
						return this._state = ChunkedTransferState.ReadCRAfterBlock;
					}
					this._iEntityLength += this._cbRemainingInBlock;
					iLen -= this._cbRemainingInBlock;
					iOffset += this._cbRemainingInBlock;
					this._cbRemainingInBlock = 0;
					this._state = ChunkedTransferState.ReadCRAfterBlock;
					break;
				case ChunkedTransferState.ReadCRAfterBlock:
					iLen--;
					if (arrData[iOffset++] != 13)
					{
						return this._state = ChunkedTransferState.Malformed;
					}
					this._state = ChunkedTransferState.ReadLFAfterBlock;
					break;
				case ChunkedTransferState.ReadLFAfterBlock:
					iLen--;
					if (arrData[iOffset++] != 10)
					{
						return this._state = ChunkedTransferState.Malformed;
					}
					this._state = ChunkedTransferState.ReadStartOfSize;
					if (this._cbRemainingInBlock != 0)
					{
						FiddlerApplication.Log.LogFormat("! BUG BUG BUG Expecting {0} more", new object[] { this._cbRemainingInBlock });
					}
					break;
				case ChunkedTransferState.ReadStartOfTrailer:
					iLen--;
					this._state = ((arrData[iOffset++] == 13) ? ChunkedTransferState.ReadFinalLF : ChunkedTransferState.ReadToTrailerCR);
					break;
				case ChunkedTransferState.ReadToTrailerCR:
					iLen--;
					if (arrData[iOffset++] == 13)
					{
						this._state = ChunkedTransferState.ReadTrailerLF;
					}
					break;
				case ChunkedTransferState.ReadTrailerLF:
					iLen--;
					this._state = ((arrData[iOffset++] == 10) ? ChunkedTransferState.ReadStartOfTrailer : ChunkedTransferState.Malformed);
					break;
				case ChunkedTransferState.ReadFinalLF:
					iLen--;
					this._state = ((arrData[iOffset++] == 10) ? ChunkedTransferState.Completed : ChunkedTransferState.Malformed);
					break;
				case ChunkedTransferState.Completed:
					this._iOverage = iLen;
					return this._state = ChunkedTransferState.Overread;
				default:
					throw new InvalidDataException("We should never get called in state: " + this._state.ToString());
				}
			}
			return this._state;
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x06000300 RID: 768 RVA: 0x0001D147 File Offset: 0x0001B347
		internal ChunkedTransferState state
		{
			get
			{
				return this._state;
			}
		}

		// Token: 0x06000301 RID: 769 RVA: 0x0001D14F File Offset: 0x0001B34F
		internal int getOverage()
		{
			return this._iOverage;
		}

		/// <summary>
		/// Number of bytes in the body (sans chunk headers, CRLFs, and trailers)
		/// </summary>
		/// <returns></returns>
		// Token: 0x06000302 RID: 770 RVA: 0x0001D157 File Offset: 0x0001B357
		internal int getEntityLength()
		{
			return this._iEntityLength;
		}

		// Token: 0x0400014F RID: 335
		private ChunkedTransferState _state;

		// Token: 0x04000150 RID: 336
		private int _cbRemainingInBlock;

		// Token: 0x04000151 RID: 337
		private int _iOverage;

		// Token: 0x04000152 RID: 338
		private int _iEntityLength;
	}
}
