using System;

namespace Fiddler
{
	// Token: 0x0200004D RID: 77
	internal enum ChunkedTransferState
	{
		// Token: 0x04000154 RID: 340
		Unknown,
		/// <summary>
		/// Read the first character of the hexadecimal size
		/// </summary>
		// Token: 0x04000155 RID: 341
		ReadStartOfSize,
		// Token: 0x04000156 RID: 342
		ReadingSize,
		// Token: 0x04000157 RID: 343
		ReadingChunkExtToCR,
		// Token: 0x04000158 RID: 344
		ReadLFAfterChunkHeader,
		// Token: 0x04000159 RID: 345
		ReadingBlock,
		// Token: 0x0400015A RID: 346
		ReadCRAfterBlock,
		// Token: 0x0400015B RID: 347
		ReadLFAfterBlock,
		/// <summary>
		/// Read the first character of the next Trailer header (if any)
		/// </summary>
		// Token: 0x0400015C RID: 348
		ReadStartOfTrailer,
		/// <summary>
		/// We're in a trailer. Read up to the next \r
		/// </summary>
		// Token: 0x0400015D RID: 349
		ReadToTrailerCR,
		/// <summary>
		/// We've just read a trailer CR, now read its LF
		/// </summary>
		// Token: 0x0400015E RID: 350
		ReadTrailerLF,
		/// <summary>
		/// We read a CR on an "empty" Trailer line, so now we just need the final LF
		/// </summary>
		// Token: 0x0400015F RID: 351
		ReadFinalLF,
		/// <summary>
		/// The chunked body was successfully read with no excess
		/// </summary>
		// Token: 0x04000160 RID: 352
		Completed,
		/// <summary>
		/// Completed, but we read too many bytes. Call getOverage to return how many bytes to put back
		/// </summary>
		// Token: 0x04000161 RID: 353
		Overread,
		/// <summary>
		/// The body was malformed
		/// </summary>
		// Token: 0x04000162 RID: 354
		Malformed
	}
}
