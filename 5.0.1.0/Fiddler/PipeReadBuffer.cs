using System;
using System.IO;

namespace Fiddler
{
	/// <summary>
	/// This class holds a specialized memory stream with growth characteristics more suitable for reading from a HTTP Stream.
	/// The default MemoryStream's Capacity will always grow to 256 bytes, then at least ~2x current capacity up to 1gb (2gb on .NET 4.6), then to the exact length after that.
	/// That has three problems:
	///
	///     The capacity may unnecessarily grow to &gt;85kb, putting the object on the LargeObjectHeap even if we didn't really need 85kb.
	///     On 32bit, we may hit a Address Space exhaustion ("Out of memory" exception) prematurely and unnecessarily due to size-doubling
	///     After the capacity reaches 1gb in length, the capacity growth never exceeds the length, leading to huge reallocations and copies on every write (fixed in .NET 4.6)
	///
	/// This class addresses those issues. http://textslashplain.com/2015/08/06/tuning-memorystream/
	/// </summary>
	// Token: 0x02000053 RID: 83
	internal class PipeReadBuffer : MemoryStream
	{
		// Token: 0x06000335 RID: 821 RVA: 0x0001E736 File Offset: 0x0001C936
		static PipeReadBuffer()
		{
			if (IntPtr.Size == 4)
			{
				PipeReadBuffer.LARGE_BUFFER = 67108864U;
				PipeReadBuffer.GROWTH_RATE = 16777216U;
			}
		}

		// Token: 0x06000336 RID: 822 RVA: 0x0001E768 File Offset: 0x0001C968
		public PipeReadBuffer(bool bIsRequest)
			: base(bIsRequest ? 0 : 4096)
		{
		}

		// Token: 0x06000337 RID: 823 RVA: 0x0001E786 File Offset: 0x0001C986
		public PipeReadBuffer(int iDefaultCapacity)
			: base(iDefaultCapacity)
		{
		}

		// Token: 0x06000338 RID: 824 RVA: 0x0001E79C File Offset: 0x0001C99C
		public override void Write(byte[] buffer, int offset, int count)
		{
			int iOrigCapacity = base.Capacity;
			uint iRequiredCapacity = (uint)(base.Position + (long)count);
			if ((ulong)iRequiredCapacity > (ulong)((long)iOrigCapacity))
			{
				if (iRequiredCapacity > 2147483591U)
				{
					throw new InsufficientMemoryException(string.Format("Sorry, the .NET Framework (and Fiddler) cannot handle streams larger than 2 gigabytes. This stream requires {0:N0} bytes", iRequiredCapacity));
				}
				if (iRequiredCapacity < 81920U)
				{
					if (this._HintedSize < 81920U && this._HintedSize >= iRequiredCapacity)
					{
						this.Capacity = (int)this._HintedSize;
					}
					else if ((long)(iOrigCapacity * 2) > 81920L || (this._HintedSize < 2147483591U && this._HintedSize >= iRequiredCapacity))
					{
						this.Capacity = 81920;
					}
				}
				else if (this._HintedSize < 2147483591U && this._HintedSize >= iRequiredCapacity && (ulong)this._HintedSize < (ulong)((long)(2097152 + iOrigCapacity * 2)))
				{
					this.Capacity = (int)this._HintedSize;
				}
				else if (iRequiredCapacity > PipeReadBuffer.LARGE_BUFFER)
				{
					uint iNewSize = iRequiredCapacity + PipeReadBuffer.GROWTH_RATE;
					if (iNewSize < 2147483591U)
					{
						this.Capacity = (int)iNewSize;
					}
					else
					{
						this.Capacity = (int)iRequiredCapacity;
					}
				}
			}
			base.Write(buffer, offset, count);
		}

		/// <summary>
		/// Used by the caller to supply a hint on the expected total size of reads from the pipe.
		/// We cannot blindly trust this value because sometimes the client or server will lie and provide a
		/// huge value that it will never use. This is common for RPC-over-HTTPS tunnels like that used by 
		/// Outlook, for instance.
		///
		/// The Content-Length can also lie by underreporting the size.
		/// </summary>
		/// <param name="iHint">Suggested total buffer size in bytes</param>
		// Token: 0x06000339 RID: 825 RVA: 0x0001E8A8 File Offset: 0x0001CAA8
		internal void HintTotalSize(uint iHint)
		{
			if (iHint < 0U)
			{
				return;
			}
			this._HintedSize = iHint;
		}

		// Token: 0x0400017D RID: 381
		private static readonly uint LARGE_BUFFER = 536870911U;

		// Token: 0x0400017E RID: 382
		private static readonly uint GROWTH_RATE = 67108864U;

		// Token: 0x0400017F RID: 383
		private const uint LARGE_OBJECT_HEAP_SIZE = 81920U;

		// Token: 0x04000180 RID: 384
		private const uint MAX_ARRAY_INDEX = 2147483591U;

		/// <summary>
		/// A client may submit a "hint" of the expected size. We use that if present.
		/// </summary>
		// Token: 0x04000181 RID: 385
		private uint _HintedSize = 2147483591U;
	}
}
