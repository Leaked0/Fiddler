using System;
using System.Globalization;
using System.IO;

namespace Fiddler
{
	/// <summary>
	/// The Parser class exposes static methods used to parse strings or byte arrays into HTTP messages.
	/// </summary>
	// Token: 0x0200004B RID: 75
	public class Parser
	{
		/// <summary>
		/// Given a byte[] representing a request, determines the offsets of the components of the line. WARNING: Input MUST contain a LF or an exception will be thrown
		/// </summary>
		/// <param name="arrRequest">Byte array of the request</param>
		/// <param name="ixURIOffset">Returns the index of the byte of the URI in the Request line</param>
		/// <param name="iURILen">Returns the length of the URI in the Request line</param>
		/// <param name="ixHeaderNVPOffset">Returns the index of the first byte of the name/value header pairs</param>
		// Token: 0x060002F1 RID: 753 RVA: 0x0001C554 File Offset: 0x0001A754
		internal static void CrackRequestLine(byte[] arrRequest, out int ixURIOffset, out int iURILen, out int ixHeaderNVPOffset, out string sMalformedURI)
		{
			ixURIOffset = (iURILen = (ixHeaderNVPOffset = 0));
			int ixPtr = 0;
			sMalformedURI = null;
			do
			{
				if (32 == arrRequest[ixPtr])
				{
					if (ixURIOffset == 0)
					{
						ixURIOffset = ixPtr + 1;
					}
					else if (iURILen == 0)
					{
						iURILen = ixPtr - ixURIOffset;
					}
					else
					{
						sMalformedURI = "Extra whitespace found in Request Line";
					}
				}
				else if (arrRequest[ixPtr] == 10)
				{
					ixHeaderNVPOffset = ixPtr + 1;
				}
				ixPtr++;
			}
			while (ixHeaderNVPOffset == 0);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="arrData"></param>
		/// <param name="iBodySeekProgress">Index of final byte of headers, if found, or location that search should resume next time</param>
		/// <param name="lngDataLen"></param>
		/// <param name="oWarnings"></param>
		/// <returns></returns>
		// Token: 0x060002F2 RID: 754 RVA: 0x0001C5B0 File Offset: 0x0001A7B0
		internal static bool FindEndOfHeaders(byte[] arrData, ref int iBodySeekProgress, long lngDataLen, out HTTPHeaderParseWarnings oWarnings)
		{
			oWarnings = HTTPHeaderParseWarnings.None;
			for (;;)
			{
				bool bFoundNewline = false;
				while ((long)iBodySeekProgress < lngDataLen - 1L)
				{
					byte b = 10;
					int num = iBodySeekProgress;
					iBodySeekProgress = num + 1;
					if (b == arrData[num])
					{
						bFoundNewline = true;
						break;
					}
				}
				if (!bFoundNewline)
				{
					break;
				}
				if (10 == arrData[iBodySeekProgress])
				{
					goto Block_3;
				}
				if (13 == arrData[iBodySeekProgress])
				{
					goto IL_45;
				}
				iBodySeekProgress++;
			}
			return false;
			Block_3:
			oWarnings = HTTPHeaderParseWarnings.EndedWithLFLF;
			return true;
			IL_45:
			iBodySeekProgress++;
			if ((long)iBodySeekProgress >= lngDataLen)
			{
				if (iBodySeekProgress > 3)
				{
					iBodySeekProgress -= 4;
				}
				else
				{
					iBodySeekProgress = 0;
				}
				return false;
			}
			if (10 == arrData[iBodySeekProgress])
			{
				if (13 != arrData[iBodySeekProgress - 3])
				{
					oWarnings = HTTPHeaderParseWarnings.EndedWithLFCRLF;
				}
				return true;
			}
			oWarnings = HTTPHeaderParseWarnings.Malformed;
			return false;
		}

		// Token: 0x060002F3 RID: 755 RVA: 0x0001C63B File Offset: 0x0001A83B
		private static bool IsPrefixedWithWhitespace(string s)
		{
			return s.Length > 0 && char.IsWhiteSpace(s[0]);
		}

		/// <summary>
		/// Parse out HTTP Header lines.
		/// </summary>
		/// <param name="oHeaders">Header collection to *append* headers to</param>
		/// <param name="sHeaderLines">Array of Strings</param>
		/// <param name="iStartAt">Index into array at which parsing should start</param>
		/// <param name="sErrors">String containing any errors encountered</param>
		/// <returns>TRUE if there were no errors, false otherwise</returns>
		// Token: 0x060002F4 RID: 756 RVA: 0x0001C658 File Offset: 0x0001A858
		internal static bool ParseNVPHeaders(HTTPHeaders oHeaders, string[] sHeaderLines, int iStartAt, ref string sErrors)
		{
			bool bResult = true;
			int ixHeader = iStartAt;
			while (ixHeader < sHeaderLines.Length)
			{
				int ixToken = sHeaderLines[ixHeader].IndexOf(':');
				HTTPHeaderItem oNewHeader;
				if (ixToken > 0)
				{
					oNewHeader = oHeaders.Add(sHeaderLines[ixHeader].Substring(0, ixToken), sHeaderLines[ixHeader].Substring(ixToken + 1).TrimStart(new char[] { ' ', '\t' }));
				}
				else if (ixToken == 0)
				{
					oNewHeader = null;
					sErrors += string.Format("Missing Header name #{0}, {1}\n", 1 + ixHeader - iStartAt, sHeaderLines[ixHeader]);
					bResult = false;
				}
				else
				{
					oNewHeader = oHeaders.Add(sHeaderLines[ixHeader], string.Empty);
					sErrors += string.Format("Missing colon in header #{0}, {1}\n", 1 + ixHeader - iStartAt, sHeaderLines[ixHeader]);
					bResult = false;
				}
				ixHeader++;
				bool bIsContinuation = oNewHeader != null && ixHeader < sHeaderLines.Length && Parser.IsPrefixedWithWhitespace(sHeaderLines[ixHeader]);
				while (bIsContinuation)
				{
					FiddlerApplication.Log.LogString("[HTTPWarning] Header folding detected. Not all clients properly handle folded headers.");
					oNewHeader.Value = oNewHeader.Value + " " + sHeaderLines[ixHeader].TrimStart(new char[] { ' ', '\t' });
					ixHeader++;
					bIsContinuation = ixHeader < sHeaderLines.Length && Parser.IsPrefixedWithWhitespace(sHeaderLines[ixHeader]);
				}
			}
			return bResult;
		}

		/// <summary>
		/// Given a byte array, determines the Headers length
		/// </summary>
		/// <param name="arrData">Input array of data</param>
		/// <param name="iHeadersLen">Returns the calculated length of the headers.</param>
		/// <param name="iEntityBodyOffset">Returns the calculated start of the response body.</param>
		/// <param name="outWarnings">Any HTTPHeaderParseWarnings discovered during parsing.</param>
		/// <returns>True, if the parsing was successful.</returns>
		// Token: 0x060002F5 RID: 757 RVA: 0x0001C78C File Offset: 0x0001A98C
		public static bool FindEntityBodyOffsetFromArray(byte[] arrData, out int iHeadersLen, out int iEntityBodyOffset, out HTTPHeaderParseWarnings outWarnings)
		{
			if (arrData != null && arrData.Length >= 2)
			{
				int iBodySeekProgress = 0;
				long lngDataLen = (long)arrData.Length;
				bool bFoundEndOfHeaders = Parser.FindEndOfHeaders(arrData, ref iBodySeekProgress, lngDataLen, out outWarnings);
				if (bFoundEndOfHeaders)
				{
					iEntityBodyOffset = iBodySeekProgress + 1;
					switch (outWarnings)
					{
					case HTTPHeaderParseWarnings.None:
						iHeadersLen = iBodySeekProgress - 3;
						return true;
					case HTTPHeaderParseWarnings.EndedWithLFLF:
						iHeadersLen = iBodySeekProgress - 1;
						return true;
					case HTTPHeaderParseWarnings.EndedWithLFCRLF:
						iHeadersLen = iBodySeekProgress - 2;
						return true;
					}
				}
			}
			iHeadersLen = (iEntityBodyOffset = -1);
			outWarnings = HTTPHeaderParseWarnings.Malformed;
			return false;
		}

		// Token: 0x060002F6 RID: 758 RVA: 0x0001C800 File Offset: 0x0001AA00
		private static int _GetEntityLengthFromHeaders(HTTPHeaders oHeaders, MemoryStream strmData)
		{
			if (oHeaders.ExistsAndEquals("Transfer-Encoding", "chunked", false))
			{
				long lngDontCare;
				long lngEndOfData;
				if (Utilities.IsChunkedBodyComplete(null, strmData, strmData.Position, out lngDontCare, out lngEndOfData))
				{
					return (int)(lngEndOfData - strmData.Position);
				}
				return (int)(strmData.Length - strmData.Position);
			}
			else
			{
				string sCL = oHeaders["Content-Length"];
				if (!string.IsNullOrEmpty(sCL))
				{
					long iEntityLength = 0L;
					if (!long.TryParse(sCL, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out iEntityLength) || iEntityLength < 0L)
					{
						return (int)(strmData.Length - strmData.Position);
					}
					return (int)iEntityLength;
				}
				else
				{
					if (oHeaders.ExistsAndContains("Connection", "close"))
					{
						return (int)(strmData.Length - strmData.Position);
					}
					return 0;
				}
			}
		}

		/// <summary>
		/// Given a MemoryStream, attempts to parse a HTTP Request starting at the current position.
		/// </summary>
		/// <returns>TRUE if a request could be parsed, FALSE otherwise</returns>
		// Token: 0x060002F7 RID: 759 RVA: 0x0001C8AC File Offset: 0x0001AAAC
		public static bool TakeRequest(MemoryStream strmClient, out HTTPRequestHeaders headersRequest, out byte[] arrRequestBody)
		{
			headersRequest = null;
			arrRequestBody = Utilities.emptyByteArray;
			if (strmClient.Length - strmClient.Position < 16L)
			{
				return false;
			}
			byte[] arrData = strmClient.GetBuffer();
			long lngDataLen = strmClient.Length;
			int iBodySeekProgress = (int)strmClient.Position;
			HTTPHeaderParseWarnings oHPW;
			if (!Parser.FindEndOfHeaders(arrData, ref iBodySeekProgress, lngDataLen, out oHPW))
			{
				return false;
			}
			byte[] arrHeaders = new byte[(long)(1 + iBodySeekProgress) - strmClient.Position];
			strmClient.Read(arrHeaders, 0, arrHeaders.Length);
			string sHeaders = CONFIG.oHeaderEncoding.GetString(arrHeaders);
			headersRequest = Parser.ParseRequest(sHeaders);
			if (headersRequest != null)
			{
				int iBodyLen = Parser._GetEntityLengthFromHeaders(headersRequest, strmClient);
				arrRequestBody = new byte[iBodyLen];
				strmClient.Read(arrRequestBody, 0, arrRequestBody.Length);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Given a MemoryStream, attempts to parse a HTTP Response starting at the current position
		/// </summary>
		/// <returns>TRUE if a response could be parsed, FALSE otherwise</returns>
		// Token: 0x060002F8 RID: 760 RVA: 0x0001C960 File Offset: 0x0001AB60
		public static bool TakeResponse(MemoryStream strmServer, string sRequestMethod, out HTTPResponseHeaders headersResponse, out byte[] arrResponseBody)
		{
			headersResponse = null;
			arrResponseBody = Utilities.emptyByteArray;
			if (strmServer.Length - strmServer.Position < 16L)
			{
				return false;
			}
			byte[] arrData = strmServer.GetBuffer();
			long lngDataLen = strmServer.Length;
			int iBodySeekProgress = (int)strmServer.Position;
			HTTPHeaderParseWarnings oHPW;
			if (!Parser.FindEndOfHeaders(arrData, ref iBodySeekProgress, lngDataLen, out oHPW))
			{
				return false;
			}
			byte[] arrHeaders = new byte[(long)(1 + iBodySeekProgress) - strmServer.Position];
			strmServer.Read(arrHeaders, 0, arrHeaders.Length);
			string sHeaders = CONFIG.oHeaderEncoding.GetString(arrHeaders);
			headersResponse = Parser.ParseResponse(sHeaders);
			if (headersResponse == null)
			{
				return false;
			}
			if (sRequestMethod == "HEAD")
			{
				return true;
			}
			int iBodyLen = Parser._GetEntityLengthFromHeaders(headersResponse, strmServer);
			if (sRequestMethod == "CONNECT")
			{
				int httpresponseCode = headersResponse.HTTPResponseCode;
			}
			arrResponseBody = new byte[iBodyLen];
			strmServer.Read(arrResponseBody, 0, arrResponseBody.Length);
			return true;
		}

		/// <summary>
		/// Parse the HTTP Request into a headers object.
		/// </summary>
		/// <param name="sRequest">The HTTP Request string, including *at least the headers* with a trailing CRLFCRLF</param>
		/// <returns>HTTPRequestHeaders parsed from the string.</returns>
		// Token: 0x060002F9 RID: 761 RVA: 0x0001CA40 File Offset: 0x0001AC40
		public static HTTPRequestHeaders ParseRequest(string sRequest)
		{
			string[] Lines = Parser._GetHeaderLines(sRequest);
			if (Lines == null)
			{
				return null;
			}
			HTTPRequestHeaders oRequestHeaders = new HTTPRequestHeaders(CONFIG.oHeaderEncoding);
			int ixToken = Lines[0].IndexOf(' ');
			if (ixToken > 0)
			{
				oRequestHeaders.HTTPMethod = Lines[0].Substring(0, ixToken).ToUpperInvariant();
				Lines[0] = Lines[0].Substring(ixToken).Trim();
			}
			ixToken = Lines[0].LastIndexOf(' ');
			if (ixToken > 0)
			{
				string sRequestPath = Lines[0].Substring(0, ixToken);
				oRequestHeaders.HTTPVersion = Lines[0].Substring(ixToken).Trim().ToUpperInvariant();
				string sHostAndUserInfo = null;
				if (sRequestPath.OICStartsWith("http://"))
				{
					oRequestHeaders.UriScheme = "http";
					ixToken = sRequestPath.IndexOfAny(new char[] { '/', '?' }, 7);
					if (ixToken == -1)
					{
						sHostAndUserInfo = sRequestPath.Substring(7);
						oRequestHeaders.RequestPath = "/";
					}
					else
					{
						sHostAndUserInfo = sRequestPath.Substring(7, ixToken - 7);
						oRequestHeaders.RequestPath = sRequestPath.Substring(ixToken);
					}
				}
				else if (sRequestPath.OICStartsWith("https://"))
				{
					oRequestHeaders.UriScheme = "https";
					ixToken = sRequestPath.IndexOfAny(new char[] { '/', '?' }, 8);
					if (ixToken == -1)
					{
						sHostAndUserInfo = sRequestPath.Substring(8);
						oRequestHeaders.RequestPath = "/";
					}
					else
					{
						sHostAndUserInfo = sRequestPath.Substring(8, ixToken - 8);
						oRequestHeaders.RequestPath = sRequestPath.Substring(ixToken);
					}
				}
				else if (sRequestPath.OICStartsWith("ftp://"))
				{
					oRequestHeaders.UriScheme = "ftp";
					ixToken = sRequestPath.IndexOf('/', 6);
					if (ixToken == -1)
					{
						sHostAndUserInfo = sRequestPath.Substring(6);
						oRequestHeaders.RequestPath = "/";
					}
					else
					{
						sHostAndUserInfo = sRequestPath.Substring(6, ixToken - 6);
						oRequestHeaders.RequestPath = sRequestPath.Substring(ixToken);
					}
				}
				else
				{
					oRequestHeaders.RequestPath = sRequestPath;
				}
				if (sHostAndUserInfo != null)
				{
					int ixAt = sHostAndUserInfo.IndexOf("@");
					if (ixAt > -1)
					{
						oRequestHeaders.UriUserInfo = Utilities.TrimTo(sHostAndUserInfo, ixAt + 1);
						sHostAndUserInfo = sHostAndUserInfo.Substring(ixAt + 1);
					}
				}
				string sErrors = string.Empty;
				Parser.ParseNVPHeaders(oRequestHeaders, Lines, 1, ref sErrors);
				if (!string.IsNullOrEmpty(sHostAndUserInfo) && !oRequestHeaders.Exists("Host"))
				{
					oRequestHeaders["Host"] = sHostAndUserInfo.ToLower();
				}
				return oRequestHeaders;
			}
			return null;
		}

		/// <summary>
		/// Break headers off, then convert CRLFs into LFs
		/// </summary>
		/// <param name="sInput"></param>
		/// <returns></returns>
		// Token: 0x060002FA RID: 762 RVA: 0x0001CC74 File Offset: 0x0001AE74
		private static string[] _GetHeaderLines(string sInput)
		{
			if (sInput.Length < 2)
			{
				return null;
			}
			int ixEndofHeaders = sInput.IndexOf("\r\n\r\n", StringComparison.Ordinal);
			if (ixEndofHeaders < 1)
			{
				ixEndofHeaders = sInput.Length;
			}
			string[] Lines = sInput.Substring(0, ixEndofHeaders).Replace("\r\n", "\n").Split('\n', StringSplitOptions.None);
			if (Lines == null || Lines.Length < 1)
			{
				return null;
			}
			return Lines;
		}

		/// <summary>
		/// Parse the HTTP Response into a headers object.
		/// </summary>
		/// <param name="sResponse">The HTTP response as a string, including at least the headers.</param>
		/// <returns>HTTPResponseHeaders parsed from the string.</returns>
		// Token: 0x060002FB RID: 763 RVA: 0x0001CCD0 File Offset: 0x0001AED0
		public static HTTPResponseHeaders ParseResponse(string sResponse)
		{
			string[] Lines = Parser._GetHeaderLines(sResponse);
			if (Lines == null)
			{
				return null;
			}
			HTTPResponseHeaders oResponseHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
			int ixToken = Lines[0].IndexOf(' ');
			if (ixToken <= 0)
			{
				return null;
			}
			oResponseHeaders.HTTPVersion = Lines[0].Substring(0, ixToken).ToUpperInvariant();
			Lines[0] = Lines[0].Substring(ixToken + 1).Trim();
			if (!oResponseHeaders.HTTPVersion.OICStartsWith("HTTP/"))
			{
				return null;
			}
			oResponseHeaders.HTTPResponseStatus = Lines[0];
			ixToken = Lines[0].IndexOf(' ');
			bool bGotStatusCode;
			if (ixToken > 0)
			{
				bGotStatusCode = int.TryParse(Lines[0].Substring(0, ixToken).Trim(), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out oResponseHeaders.HTTPResponseCode);
			}
			else
			{
				bGotStatusCode = int.TryParse(Lines[0].Trim(), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out oResponseHeaders.HTTPResponseCode);
			}
			if (!bGotStatusCode)
			{
				return null;
			}
			string sErrors = string.Empty;
			Parser.ParseNVPHeaders(oResponseHeaders, Lines, 1, ref sErrors);
			return oResponseHeaders;
		}
	}
}
