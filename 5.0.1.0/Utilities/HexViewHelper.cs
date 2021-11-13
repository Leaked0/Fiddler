using System;
using System.Text;

namespace FiddlerCore.Utilities
{
	// Token: 0x02000076 RID: 118
	internal static class HexViewHelper
	{
		/// <summary>
		/// Pretty-print a Hex view of a byte array. Slow.
		/// </summary>
		/// <param name="inArr">The byte array</param>
		/// <param name="iBytesPerLine">Number of bytes per line</param>
		/// <returns>String containing a pretty-printed array</returns>
		// Token: 0x060005B2 RID: 1458 RVA: 0x00033C8A File Offset: 0x00031E8A
		public static string ByteArrayToHexView(byte[] inArr, int iBytesPerLine)
		{
			return HexViewHelper.ByteArrayToHexView(inArr, iBytesPerLine, inArr.Length, true);
		}

		/// <summary>
		/// Pretty-print a Hex view of a byte array. Slow.
		/// </summary>
		/// <param name="inArr">The byte array</param>
		/// <param name="iBytesPerLine">Number of bytes per line</param>
		/// <param name="iMaxByteCount">The maximum number of bytes to pretty-print</param>
		/// <returns>String containing a pretty-printed array</returns>
		// Token: 0x060005B3 RID: 1459 RVA: 0x00033C97 File Offset: 0x00031E97
		public static string ByteArrayToHexView(byte[] inArr, int iBytesPerLine, int iMaxByteCount)
		{
			return HexViewHelper.ByteArrayToHexView(inArr, iBytesPerLine, iMaxByteCount, true);
		}

		/// <summary>
		/// Pretty-print a Hex view of a byte array. Slow.
		/// </summary>
		/// <param name="inArr">The byte array</param>
		/// <param name="iBytesPerLine">Number of bytes per line</param>
		/// <param name="iMaxByteCount">The maximum number of bytes to pretty-print</param>
		/// <param name="bShowASCII">Show ASCII text at the end of each line</param>
		/// <returns>String containing a pretty-printed array</returns>
		// Token: 0x060005B4 RID: 1460 RVA: 0x00033CA2 File Offset: 0x00031EA2
		public static string ByteArrayToHexView(byte[] inArr, int iBytesPerLine, int iMaxByteCount, bool bShowASCII)
		{
			return HexViewHelper.ByteArrayToHexView(inArr, 0, iBytesPerLine, iMaxByteCount, bShowASCII);
		}

		// Token: 0x060005B5 RID: 1461 RVA: 0x00033CB0 File Offset: 0x00031EB0
		public static string ByteArrayToHexView(byte[] inArr, int iStartAt, int iBytesPerLine, int iMaxByteCount, bool bShowASCII)
		{
			if (inArr == null || inArr.Length == 0)
			{
				return string.Empty;
			}
			if (iBytesPerLine < 1 || iMaxByteCount < 1)
			{
				return string.Empty;
			}
			int iMaxOffset = Math.Min(iMaxByteCount + iStartAt, inArr.Length);
			StringBuilder sbOutput = new StringBuilder(iMaxByteCount * 5);
			for (int iPtr = iStartAt; iPtr < iMaxOffset; iPtr += iBytesPerLine)
			{
				int iLineLen = Math.Min(iBytesPerLine, iMaxOffset - iPtr);
				bool bLastLine = iLineLen < iBytesPerLine;
				for (int i = 0; i < iLineLen; i++)
				{
					sbOutput.Append(inArr[iPtr + i].ToString("X2"));
					sbOutput.Append(" ");
				}
				if (bLastLine)
				{
					sbOutput.Append(new string(' ', 3 * (iBytesPerLine - iLineLen)));
				}
				if (bShowASCII)
				{
					sbOutput.Append(" ");
					for (int j = 0; j < iLineLen; j++)
					{
						if (inArr[iPtr + j] < 32)
						{
							sbOutput.Append(".");
						}
						else
						{
							sbOutput.Append((char)inArr[iPtr + j]);
						}
					}
					if (bLastLine)
					{
						sbOutput.Append(new string(' ', iBytesPerLine - iLineLen));
					}
				}
				sbOutput.Append("\r\n");
			}
			return sbOutput.ToString();
		}
	}
}
