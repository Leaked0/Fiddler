using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FiddlerCore.PlatformExtensions.Windows
{
	// Token: 0x0200009D RID: 157
	internal static class XpressCompressionHelperForWindows
	{
		// Token: 0x0600064F RID: 1615
		[DllImport("cabinet", SetLastError = true)]
		private static extern bool CreateDecompressor(XpressCompressionHelperForWindows.CompressAlgorithm Algorithm, IntPtr pAllocators, out IntPtr hDecompressor);

		// Token: 0x06000650 RID: 1616
		[DllImport("cabinet", SetLastError = true)]
		private static extern bool CloseCompressor(IntPtr hCompressor);

		// Token: 0x06000651 RID: 1617
		[DllImport("cabinet", SetLastError = true)]
		private static extern bool Decompress(IntPtr hDecompressor, byte[] arrCompressedData, UIntPtr cbCompressedDataSize, byte[] arrOutputBuffer, IntPtr cbUncompressedBufferSize, out UIntPtr cbUncompressedDataSize);

		// Token: 0x06000652 RID: 1618
		[DllImport("cabinet", SetLastError = true)]
		private static extern bool CloseDecompressor(IntPtr hDecompressor);

		/// <summary>
		/// Requires Win8+
		/// Decompress Xpress|Raw blocks used by WSUS, etc.
		/// Introduction to the API is at http://msdn.microsoft.com/en-us/library/windows/desktop/hh920921(v=vs.85).aspx
		/// </summary>
		/// <param name="compressedData"></param>
		/// <returns></returns>
		// Token: 0x06000653 RID: 1619 RVA: 0x000354BC File Offset: 0x000336BC
		public static byte[] Decompress(byte[] arrBlock)
		{
			if (arrBlock.Length < 9)
			{
				return new byte[0];
			}
			MemoryStream msResult = new MemoryStream();
			IntPtr hDecompressor;
			XpressCompressionHelperForWindows.CreateDecompressor(XpressCompressionHelperForWindows.CompressAlgorithm.Null | XpressCompressionHelperForWindows.CompressAlgorithm.MSZIP | XpressCompressionHelperForWindows.CompressAlgorithm.RAW, IntPtr.Zero, out hDecompressor);
			int ixOffset = 0;
			int iDecompressedSize;
			int iCompressedSize;
			for (;;)
			{
				iDecompressedSize = BitConverter.ToInt32(arrBlock, ixOffset);
				ixOffset += 4;
				if (iDecompressedSize < 0 || iDecompressedSize > 1000000000)
				{
					break;
				}
				iCompressedSize = BitConverter.ToInt32(arrBlock, ixOffset);
				ixOffset += 4;
				if (iCompressedSize + ixOffset > arrBlock.Length)
				{
					goto Block_3;
				}
				byte[] arrCompressed = new byte[iCompressedSize];
				Buffer.BlockCopy(arrBlock, ixOffset, arrCompressed, 0, arrCompressed.Length);
				byte[] bytesOut = new byte[iDecompressedSize];
				UIntPtr pOutDataSize;
				XpressCompressionHelperForWindows.Decompress(hDecompressor, arrCompressed, (UIntPtr)((ulong)((long)arrCompressed.Length)), bytesOut, (IntPtr)bytesOut.Length, out pOutDataSize);
				ulong num = (ulong)pOutDataSize.ToUInt32();
				long num2 = (long)iDecompressedSize;
				msResult.Write(bytesOut, 0, (int)pOutDataSize.ToUInt32());
				ixOffset += iCompressedSize;
				if (ixOffset >= arrBlock.Length)
				{
					goto Block_4;
				}
			}
			throw new InvalidDataException(string.Format("Uncompressed data was too large {0:N0} bytes", iDecompressedSize));
			Block_3:
			throw new InvalidDataException(string.Format("Expecting {0:N0} bytes of compressed data, but only {1:N0} bytes remain in this stream", iCompressedSize, arrBlock.Length - ixOffset));
			Block_4:
			XpressCompressionHelperForWindows.CloseDecompressor(hDecompressor);
			return msResult.ToArray();
		}

		// Token: 0x020000E7 RID: 231
		[Flags]
		private enum CompressAlgorithm : uint
		{
			// Token: 0x040003D0 RID: 976
			Invalid = 0U,
			// Token: 0x040003D1 RID: 977
			Null = 1U,
			// Token: 0x040003D2 RID: 978
			MSZIP = 2U,
			// Token: 0x040003D3 RID: 979
			XPRESS = 3U,
			// Token: 0x040003D4 RID: 980
			XPRESS_HUFF = 4U,
			// Token: 0x040003D5 RID: 981
			LZMS = 5U,
			// Token: 0x040003D6 RID: 982
			RAW = 536870912U
		}
	}
}
