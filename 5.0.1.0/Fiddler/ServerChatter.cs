using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using FiddlerCore.Utilities;

namespace Fiddler
{
	/// <summary>
	/// The ServerChatter object is responsible for transmitting the Request to the destination server and retrieving its Response.
	/// </summary>
	/// <remarks>
	/// This class maintains its own PipeReadBuffer that it fills from the created or reused ServerPipe. After it determines that
	/// a complete response is present, it allows the caller to grab that array using the TakeEntity method. If
	/// unsatisfied with the result (e.g. a network error), the caller can call Initialize() and SendRequest() again.
	/// </remarks>
	// Token: 0x02000059 RID: 89
	public class ServerChatter
	{
		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x0600039A RID: 922 RVA: 0x00021F9C File Offset: 0x0002019C
		internal bool bLeakedHeaders
		{
			get
			{
				return this.m_bLeakedHeaders;
			}
		}

		/// <summary>
		/// Peek at number of bytes downloaded thus far.
		/// </summary>
		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x0600039B RID: 923 RVA: 0x00021FA4 File Offset: 0x000201A4
		internal long _PeekDownloadProgress
		{
			get
			{
				if (this.m_responseData != null)
				{
					return this.m_responseTotalDataCount;
				}
				return -1L;
			}
		}

		/// <summary>
		/// Get the MIME type (sans Character set or other attributes) from the HTTP Content-Type response header, or String.Empty if missing.
		/// </summary>
		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x0600039C RID: 924 RVA: 0x00021FB8 File Offset: 0x000201B8
		public string MIMEType
		{
			get
			{
				if (this.headers == null)
				{
					return string.Empty;
				}
				string sMIME = this.headers["Content-Type"];
				if (sMIME.Length > 0)
				{
					sMIME = Utilities.TrimAfter(sMIME, ';').Trim();
				}
				return sMIME;
			}
		}

		/// <summary>
		/// DEPRECATED: You should use the Timers object on the Session object instead.
		/// The number of milliseconds between the start of sending the request to the server to the first byte of the server's response
		/// </summary>
		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x0600039D RID: 925 RVA: 0x00021FFC File Offset: 0x000201FC
		public int iTTFB
		{
			get
			{
				int i = (int)(this.m_session.Timers.ServerBeginResponse - this.m_session.Timers.FiddlerBeginRequest).TotalMilliseconds;
				if (i <= 0)
				{
					return 0;
				}
				return i;
			}
		}

		/// <summary>
		/// DEPRECATED: You should use the Timers object on the Session object instead.
		/// The number of milliseconds between the start of sending the request to the server to the last byte of the server's response.
		/// </summary>
		// Token: 0x170000A8 RID: 168
		// (get) Token: 0x0600039E RID: 926 RVA: 0x00022040 File Offset: 0x00020240
		public int iTTLB
		{
			get
			{
				int i = (int)(this.m_session.Timers.ServerDoneResponse - this.m_session.Timers.FiddlerBeginRequest).TotalMilliseconds;
				if (i <= 0)
				{
					return 0;
				}
				return i;
			}
		}

		/// <summary>
		/// Was this request forwarded to a gateway?
		/// </summary>
		// Token: 0x170000A9 RID: 169
		// (get) Token: 0x0600039F RID: 927 RVA: 0x00022083 File Offset: 0x00020283
		public bool bWasForwarded
		{
			get
			{
				return this.m_bWasForwarded;
			}
		}

		/// <summary>
		/// Was this request serviced from a reused server connection?
		/// </summary>
		// Token: 0x170000AA RID: 170
		// (get) Token: 0x060003A0 RID: 928 RVA: 0x0002208B File Offset: 0x0002028B
		public bool bServerSocketReused
		{
			get
			{
				return this.m_session.isFlagSet(SessionFlags.ServerPipeReused);
			}
		}

		/// <summary>
		/// The HTTP headers of the server's response
		/// </summary>
		// Token: 0x170000AB RID: 171
		// (get) Token: 0x060003A1 RID: 929 RVA: 0x0002209A File Offset: 0x0002029A
		// (set) Token: 0x060003A2 RID: 930 RVA: 0x000220A2 File Offset: 0x000202A2
		public HTTPResponseHeaders headers
		{
			get
			{
				return this.m_inHeaders;
			}
			set
			{
				if (value != null)
				{
					this.m_inHeaders = value;
				}
			}
		}

		/// <summary>
		/// Simple indexer into the Response Headers object
		/// </summary>
		// Token: 0x170000AC RID: 172
		public string this[string sHeader]
		{
			get
			{
				if (this.m_inHeaders != null)
				{
					return this.m_inHeaders[sHeader];
				}
				return string.Empty;
			}
			set
			{
				if (this.m_inHeaders != null)
				{
					this.m_inHeaders[sHeader] = value;
					return;
				}
				throw new InvalidDataException("Response Headers object does not exist");
			}
		}

		// Token: 0x060003A5 RID: 933 RVA: 0x000220EC File Offset: 0x000202EC
		internal ServerChatter(Session oSession)
		{
			this.m_session = oSession;
			this.m_responseData = new PipeReadBuffer(false);
		}

		/// <summary>
		/// Create a ServerChatter object and initialize its headers from the specified string
		/// </summary>
		/// <param name="oSession"></param>
		/// <param name="sHeaders"></param>
		// Token: 0x060003A6 RID: 934 RVA: 0x0002211A File Offset: 0x0002031A
		internal ServerChatter(Session oSession, string sHeaders)
		{
			this.m_session = oSession;
			this.m_inHeaders = Parser.ParseResponse(sHeaders);
		}

		/// <summary>
		/// Reset the response-reading fields on the object. Also used on a retry.
		/// </summary>
		/// <param name="bAllocatePipeReadBuffer">If TRUE, allocates a buffer (m_responseData) to read from a pipe. If FALSE, nulls m_responseData.</param>
		// Token: 0x060003A7 RID: 935 RVA: 0x00022148 File Offset: 0x00020348
		internal void Initialize(bool bAllocatePipeReadBuffer)
		{
			this.m_responseData = (bAllocatePipeReadBuffer ? new PipeReadBuffer(false) : null);
			this.m_responseTotalDataCount = (this.m_lngLeakedOffset = (long)(this.m_iBodySeekProgress = (this.m_iEntityBodyOffset = 0)));
			this.m_lngLastChunkInfoOffset = -1L;
			this.m_inHeaders = null;
			this.m_bLeakedHeaders = false;
			if (this.pipeServer != null)
			{
				FiddlerApplication.DebugSpew("Reinitializing ServerChatter; detaching ServerPipe.");
				this.pipeServer.End();
				this.pipeServer = null;
			}
			this.m_bWasForwarded = false;
			this.m_session.SetBitFlag(SessionFlags.ServerPipeReused, false);
		}

		/// <summary>
		/// Peek at the current response body and return it as an array
		/// </summary>
		/// <returns>The response body as an array, or byte[0]</returns>
		// Token: 0x060003A8 RID: 936 RVA: 0x000221DC File Offset: 0x000203DC
		internal byte[] _PeekAtBody()
		{
			if (this.m_iEntityBodyOffset < 1 || this.m_responseData == null || this.m_responseData.Length < 1L)
			{
				return Utilities.emptyByteArray;
			}
			int lngSize = (int)this.m_responseData.Length - this.m_iEntityBodyOffset;
			if (lngSize < 1)
			{
				return Utilities.emptyByteArray;
			}
			byte[] arrBody = new byte[lngSize];
			Buffer.BlockCopy(this.m_responseData.GetBuffer(), this.m_iEntityBodyOffset, arrBody, 0, lngSize);
			return arrBody;
		}

		/// <summary>
		/// Get the response body byte array from the PipeReadBuffer, then dispose of it.
		///
		/// WARNING: This eats all of the bytes in the Pipe, even if that includes bytes of a 
		/// future, as-yet-unrequested response. Fiddler does not pipeline requests, so that works okay for now.
		/// For now, the caller should validate that the returned entity is of the expected size (e.g. based on Content-Length)
		/// </summary>
		// Token: 0x060003A9 RID: 937 RVA: 0x00022250 File Offset: 0x00020450
		internal byte[] TakeEntity()
		{
			long iSize = this.m_responseData.Length - (long)this.m_iEntityBodyOffset;
			if (iSize < 1L)
			{
				this.FreeResponseDataBuffer();
				return Utilities.emptyByteArray;
			}
			byte[] arrResult;
			try
			{
				arrResult = new byte[iSize];
				Buffer.BlockCopy(this.m_responseData.GetBuffer(), this.m_iEntityBodyOffset, arrResult, 0, arrResult.Length);
			}
			catch (OutOfMemoryException oOOM)
			{
				string title = "HTTP Response Too Large";
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[]
				{
					title,
					oOOM.ToString()
				});
				arrResult = Encoding.ASCII.GetBytes("Fiddler: Out-of-memory/contiguous-address-space");
				this.m_session.PoisonServerPipe();
			}
			this.FreeResponseDataBuffer();
			return arrResult;
		}

		// Token: 0x060003AA RID: 938 RVA: 0x00022308 File Offset: 0x00020508
		internal void FreeResponseDataBuffer()
		{
			if (this.m_responseData != null)
			{
				this.m_responseData.Dispose();
				this.m_responseData = null;
			}
		}

		/// <summary>
		/// Scans responseData stream for the \r\n\r\n (or variants) sequence
		/// which indicates that the header block is complete.
		///
		/// SIDE EFFECTS:
		///     iBodySeekProgress is updated and maintained across calls to this function
		///     iEntityBodyOffset is updated if the end of headers is found
		/// </summary>
		/// <returns>True, if responseData contains a full set of headers</returns>
		// Token: 0x060003AB RID: 939 RVA: 0x00022324 File Offset: 0x00020524
		private bool HeadersAvailable()
		{
			if (this.m_iEntityBodyOffset > 0)
			{
				return true;
			}
			if (this.m_responseData == null)
			{
				return false;
			}
			byte[] arrData = this.m_responseData.GetBuffer();
			HTTPHeaderParseWarnings oHPW;
			bool bFoundEndOfHeaders = Parser.FindEndOfHeaders(arrData, ref this.m_iBodySeekProgress, this.m_responseData.Length, out oHPW);
			if (bFoundEndOfHeaders)
			{
				this.m_iEntityBodyOffset = this.m_iBodySeekProgress + 1;
				if (oHPW != HTTPHeaderParseWarnings.EndedWithLFLF)
				{
					if (oHPW == HTTPHeaderParseWarnings.EndedWithLFCRLF)
					{
						FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "The Server did not return properly formatted HTTP Headers. HTTP headers\nshould be terminated with CRLFCRLF. These were terminated with LFCRLF.");
					}
				}
				else
				{
					FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "The Server did not return properly formatted HTTP Headers. HTTP headers\nshould be terminated with CRLFCRLF. These were terminated with LFLF.");
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Parse the HTTP Response into Headers and Body.
		/// </summary>
		/// <returns></returns>
		// Token: 0x060003AC RID: 940 RVA: 0x000223BC File Offset: 0x000205BC
		private bool ParseResponseForHeaders()
		{
			if (this.m_responseData == null || this.m_iEntityBodyOffset < 4)
			{
				return false;
			}
			this.m_inHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
			byte[] arrResponse = this.m_responseData.GetBuffer();
			string sResponseHeaders = CONFIG.oHeaderEncoding.GetString(arrResponse, 0, this.m_iEntityBodyOffset).Trim();
			if (sResponseHeaders == null || sResponseHeaders.Length < 1)
			{
				this.m_inHeaders = null;
				return false;
			}
			string[] arrHeaderLines = sResponseHeaders.Replace("\r\n", "\n").Split('\n', StringSplitOptions.None);
			if (arrHeaderLines.Length < 1)
			{
				return false;
			}
			int ixToken = arrHeaderLines[0].IndexOf(' ');
			if (ixToken <= 0)
			{
				FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "Cannot parse HTTP response; Status line contains no spaces. Data:\n\n\t" + arrHeaderLines[0]);
				return false;
			}
			this.m_inHeaders.HTTPVersion = arrHeaderLines[0].Substring(0, ixToken).ToUpperInvariant();
			arrHeaderLines[0] = arrHeaderLines[0].Substring(ixToken + 1).Trim();
			if (!this.m_inHeaders.HTTPVersion.OICStartsWith("HTTP/"))
			{
				if (!this.m_inHeaders.HTTPVersion.OICStartsWith("ICY"))
				{
					FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "Response does not start with HTTP. Data:\n\n\t" + arrHeaderLines[0]);
					return false;
				}
				this.m_session.bBufferResponse = false;
				this.m_session.oFlags["log-drop-response-body"] = "ICY";
			}
			this.m_inHeaders.HTTPResponseStatus = arrHeaderLines[0];
			ixToken = arrHeaderLines[0].IndexOf(' ');
			bool bGotStatusCode;
			if (ixToken > 0)
			{
				bGotStatusCode = int.TryParse(arrHeaderLines[0].Substring(0, ixToken).Trim(), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out this.m_inHeaders.HTTPResponseCode);
			}
			else
			{
				string sRestOfLine = arrHeaderLines[0].Trim();
				bGotStatusCode = int.TryParse(sRestOfLine, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out this.m_inHeaders.HTTPResponseCode);
				if (!bGotStatusCode)
				{
					int iXFirstChar = 0;
					while (iXFirstChar < sRestOfLine.Length)
					{
						if (!char.IsDigit(sRestOfLine[iXFirstChar]))
						{
							bGotStatusCode = int.TryParse(sRestOfLine.Substring(0, iXFirstChar), NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out this.m_inHeaders.HTTPResponseCode);
							if (bGotStatusCode)
							{
								FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, false, "The response's status line was missing a space between ResponseCode and ResponseStatus. Data:\n\n\t" + sRestOfLine);
								break;
							}
							break;
						}
						else
						{
							iXFirstChar++;
						}
					}
				}
			}
			if (!bGotStatusCode)
			{
				FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "The response's status line did not contain a ResponseCode. Data:\n\n\t" + arrHeaderLines[0]);
				return false;
			}
			string sErrs = string.Empty;
			if (!Parser.ParseNVPHeaders(this.m_inHeaders, arrHeaderLines, 1, ref sErrs))
			{
				FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "Incorrectly formed response headers.\n" + sErrs);
			}
			if (this.m_inHeaders.Exists("Content-Length") && this.m_inHeaders.ExistsAndContains("Transfer-Encoding", "chunked"))
			{
				FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, false, false, "Content-Length response header MUST NOT be present when Transfer-Encoding is used (RFC2616 Section 4.4)");
			}
			return true;
		}

		/// <summary>
		/// Attempt to pull the final (non-1xx) Headers from the stream. If HTTP/100 messages are found, the method
		/// will recurse into itself to find the next set of headers.
		/// </summary>
		// Token: 0x060003AD RID: 941 RVA: 0x00022698 File Offset: 0x00020898
		private bool GetHeaders()
		{
			if (!this.HeadersAvailable())
			{
				return false;
			}
			if (!this.ParseResponseForHeaders())
			{
				this.m_session.SetBitFlag(SessionFlags.ProtocolViolationInResponse, true);
				this._PoisonPipe();
				string sDetailedError;
				if (this.m_responseData != null)
				{
					sDetailedError = "<plaintext>\n" + Utilities.ByteArrayToHexView(this.m_responseData.GetBuffer(), 24, (int)Math.Min(this.m_responseData.Length, 2048L));
				}
				else
				{
					sDetailedError = "{Fiddler:no data}";
				}
				this.m_session.oRequest.FailSession(500, "Fiddler - Bad Response", string.Format("[Fiddler] Response Header parsing failed.\n{0}Response Data:\n{1}", this.m_session.isFlagSet(SessionFlags.ServerPipeReused) ? "This can be caused by an illegal HTTP response earlier on this reused server socket-- for instance, a HTTP/304 response which illegally contains a body.\n" : string.Empty, sDetailedError));
				return true;
			}
			if (this.m_inHeaders.HTTPResponseCode > 99 && this.m_inHeaders.HTTPResponseCode < 200)
			{
				if (this.m_inHeaders.Exists("Content-Length") && "0" != this.m_inHeaders["Content-Length"].Trim())
				{
					FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "HTTP/1xx responses MUST NOT contain a body, but a non-zero content-length was returned.");
				}
				if (this.m_inHeaders.HTTPResponseCode != 101 || !this.m_inHeaders.ExistsAndContains("Upgrade", "WebSocket"))
				{
					StringDictionary oFlags;
					if (FiddlerApplication.Prefs.GetBoolPref("fiddler.network.leakhttp1xx", true) && this.m_session.oRequest.pipeClient != null)
					{
						try
						{
							this.m_session.oRequest.pipeClient.Send(this.m_inHeaders.ToByteArray(true, true));
							oFlags = this.m_session.oFlags;
							oFlags["x-fiddler-Stream1xx"] = oFlags["x-fiddler-Stream1xx"] + "Returned a HTTP/" + this.m_inHeaders.HTTPResponseCode.ToString() + " message from the server.";
							goto IL_276;
						}
						catch (Exception eXInner)
						{
							if (FiddlerApplication.Prefs.GetBoolPref("fiddler.network.streaming.abortifclientaborts", false))
							{
								throw new Exception("Leaking HTTP/1xx response to client failed", eXInner);
							}
							FiddlerApplication.Log.LogFormat("fiddler.network.streaming> Streaming of HTTP/1xx headers from #{0} to client failed: {1}", new object[]
							{
								this.m_session.id,
								eXInner.Message
							});
							goto IL_276;
						}
					}
					oFlags = this.m_session.oFlags;
					oFlags["x-fiddler-streaming"] = oFlags["x-fiddler-streaming"] + "Eating a HTTP/" + this.m_inHeaders.HTTPResponseCode.ToString() + " message from the stream.";
					IL_276:
					this._deleteInformationalMessage();
					return this.GetHeaders();
				}
			}
			return true;
		}

		// Token: 0x060003AE RID: 942 RVA: 0x0002293C File Offset: 0x00020B3C
		private bool isResponseBodyComplete()
		{
			if (this.m_session.HTTPMethodIs("HEAD"))
			{
				return true;
			}
			if (this.m_session.HTTPMethodIs("CONNECT") && this.m_inHeaders.HTTPResponseCode == 200)
			{
				return true;
			}
			if (this.m_inHeaders.HTTPResponseCode == 200 && this.m_session.isFlagSet(SessionFlags.IsRPCTunnel))
			{
				this.m_session.bBufferResponse = true;
				return true;
			}
			if (this.m_inHeaders.HTTPResponseCode == 204 || this.m_inHeaders.HTTPResponseCode == 205 || this.m_inHeaders.HTTPResponseCode == 304 || (this.m_inHeaders.HTTPResponseCode > 99 && this.m_inHeaders.HTTPResponseCode < 200))
			{
				if (this.m_inHeaders.Exists("Content-Length") && "0" != this.m_inHeaders["Content-Length"].Trim())
				{
					FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, false, true, "This type of HTTP response MUST NOT contain a body, but a non-zero content-length was returned.");
					return true;
				}
				return true;
			}
			else
			{
				if (this.m_inHeaders.ExistsAndEquals("Transfer-Encoding", "chunked", false))
				{
					if (this.m_lngLastChunkInfoOffset < (long)this.m_iEntityBodyOffset)
					{
						this.m_lngLastChunkInfoOffset = (long)this.m_iEntityBodyOffset;
					}
					long lngEndOfEntity;
					return Utilities.IsChunkedBodyComplete(this.m_session, this.m_responseData, this.m_lngLastChunkInfoOffset, out this.m_lngLastChunkInfoOffset, out lngEndOfEntity);
				}
				if (this.m_inHeaders.Exists("Content-Length"))
				{
					long iEntityLength;
					if (!long.TryParse(this.m_inHeaders["Content-Length"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out iEntityLength) || iEntityLength < 0L)
					{
						FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, true, true, "Content-Length response header is not a valid unsigned integer.\nContent-Length: " + this.m_inHeaders["Content-Length"]);
						return true;
					}
					return this.m_responseTotalDataCount >= (long)this.m_iEntityBodyOffset + iEntityLength;
				}
				else
				{
					if (this.m_inHeaders.ExistsAndEquals("Connection", "close", false) || this.m_inHeaders.ExistsAndEquals("Proxy-Connection", "close", false) || (this.m_inHeaders.HTTPVersion != "HTTP/1.1" && !this.m_inHeaders.ExistsAndContains("Connection", "Keep-Alive")))
					{
						return false;
					}
					FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, true, true, "No Connection: close, no Content-Length. No way to tell if the response is complete.");
					return false;
				}
			}
		}

		/// <summary>
		/// Deletes a single HTTP/1xx header block from the Response stream
		/// and adjusts all header-reading state to start over from the top of the stream. 
		/// Note: If 'fiddler.network.leakhttp1xx' is TRUE, then the 1xx message will have been leaked before calling this method.
		/// </summary>
		// Token: 0x060003AF RID: 943 RVA: 0x00022BA4 File Offset: 0x00020DA4
		private void _deleteInformationalMessage()
		{
			this.m_inHeaders = null;
			int cbNextResponse = (int)this.m_responseData.Length - this.m_iEntityBodyOffset;
			PipeReadBuffer newResponse = new PipeReadBuffer(cbNextResponse);
			newResponse.Write(this.m_responseData.GetBuffer(), this.m_iEntityBodyOffset, cbNextResponse);
			this.m_responseData = newResponse;
			this.m_responseTotalDataCount = this.m_responseData.Length;
			this.m_iEntityBodyOffset = (this.m_iBodySeekProgress = 0);
		}

		/// <summary>
		/// Adjusts PipeServer's ReusePolicy if response headers require closure. Then calls _detachServerPipe()
		/// </summary>
		// Token: 0x060003B0 RID: 944 RVA: 0x00022C14 File Offset: 0x00020E14
		internal void releaseServerPipe()
		{
			if (this.pipeServer == null)
			{
				return;
			}
			if (this.headers.ExistsAndEquals("Connection", "close", false) || this.headers.ExistsAndEquals("Proxy-Connection", "close", false) || (this.headers.HTTPVersion != "HTTP/1.1" && !this.headers.ExistsAndContains("Connection", "Keep-Alive")) || !this.pipeServer.Connected)
			{
				this.pipeServer.ReusePolicy = PipeReusePolicy.NoReuse;
			}
			this._detachServerPipe();
		}

		/// <summary>
		/// Queues or End()s the ServerPipe, depending on its ReusePolicy
		/// </summary>
		// Token: 0x060003B1 RID: 945 RVA: 0x00022CA8 File Offset: 0x00020EA8
		internal void _detachServerPipe()
		{
			if (this.pipeServer == null)
			{
				return;
			}
			if (this.pipeServer.ReusePolicy != PipeReusePolicy.NoReuse && this.pipeServer.ReusePolicy != PipeReusePolicy.MarriedToClientPipe && this.pipeServer.isClientCertAttached && !this.pipeServer.isAuthenticated)
			{
				this.pipeServer.MarkAsAuthenticated(this.m_session.LocalProcessID);
			}
			Proxy.htServerPipePool.PoolOrClosePipe(this.pipeServer);
			this.pipeServer = null;
		}

		/// <summary>
		/// Determines whether a given PIPE is suitable for a given Session, based on that Session's SID
		/// </summary>
		/// <param name="iPID">The Client Process ID, if any</param>
		/// <param name="sIDSession">The base (no PID) PoolKey expected by the session</param>
		/// <param name="sIDPipe">The pipe's pool key</param>
		/// <returns>TRUE if the connection should be used, FALSE otherwise</returns>
		// Token: 0x060003B2 RID: 946 RVA: 0x00022D21 File Offset: 0x00020F21
		private static bool SIDsMatch(int iPID, string sIDSession, string sIDPipe)
		{
			return sIDSession.OICEquals(sIDPipe) || (iPID != 0 && sIDPipe.OICEquals(string.Format("pid{0}*{1}", iPID, sIDSession)));
		}

		// Token: 0x060003B3 RID: 947 RVA: 0x00022D50 File Offset: 0x00020F50
		internal void BeginAsyncConnectToHost(AsyncCallback OnDone)
		{
			if (this.m_session.isFTP && !this.m_session.isFlagSet(SessionFlags.SentToGateway))
			{
				OnDone(null);
				return;
			}
			this._esState = new ServerChatter.MakeConnectionExecutionState();
			this._esState.OnDone = OnDone;
			this._esState.CurrentState = ServerChatter.StateConnecting.BeginFindGateway;
			this.RunConnectionStateMachine();
		}

		// Token: 0x060003B4 RID: 948 RVA: 0x00022DB0 File Offset: 0x00020FB0
		internal void RunConnectionStateMachine()
		{
			bool bAsyncExit = false;
			while (this._esState != null)
			{
				switch (this._esState.CurrentState)
				{
				case ServerChatter.StateConnecting.BeginFindGateway:
				{
					this._esState.sTarget = this.m_session.oFlags["x-overrideHostName"];
					if (this._esState.sTarget != null)
					{
						this.m_session.oFlags["x-overrideHost"] = string.Format("{0}:{1}", this._esState.sTarget, this.m_session.port);
					}
					this._esState.sTarget = this.m_session.oFlags["x-overrideHost"];
					if (this._esState.sTarget == null)
					{
						if (this.m_session.HTTPMethodIs("CONNECT"))
						{
							this._esState.sTarget = this.m_session.PathAndQuery;
						}
						else
						{
							this._esState.sTarget = this.m_session.host;
						}
					}
					else
					{
						this._esState.sPoolKeyContext = string.Format("-for-{0}", this.m_session.host);
					}
					if (this.m_session.oFlags["x-overrideGateway"] != null)
					{
						if ("DIRECT".OICEquals(this.m_session.oFlags["x-overrideGateway"]))
						{
							this.m_session.bypassGateway = true;
						}
						else
						{
							string sGatewayOverride = this.m_session.oFlags["x-overrideGateway"];
							if (sGatewayOverride.OICStartsWith("socks="))
							{
								this._esState.bUseSOCKSGateway = true;
								sGatewayOverride = sGatewayOverride.Substring(6);
							}
							this._esState.ipepGateways = Utilities.IPEndPointListFromHostPortString(sGatewayOverride);
							if (this._esState.ipepGateways == null)
							{
								FiddlerApplication.DebugSpew("DNS lookup failed for X-OverrideGateway: '{0}'", new object[] { sGatewayOverride });
								if (this._esState.bUseSOCKSGateway)
								{
									this.m_session.oRequest.FailSession(502, "Fiddler - SOCKS Proxy DNS Lookup Failed", string.Format("[Fiddler] DNS Lookup for SOCKS Proxy \"{0}\" failed. {1}", Utilities.HtmlEncode(sGatewayOverride), NetworkInterface.GetIsNetworkAvailable() ? string.Empty : "The system reports that no network connection is available. \n"));
									this._esState.CurrentState = ServerChatter.StateConnecting.Failed;
									break;
								}
								if (!FiddlerApplication.Prefs.GetBoolPref("fiddler.proxy.IgnoreGatewayOverrideIfUnreachable", false))
								{
									this.m_session.oRequest.FailSession(502, "Fiddler - Proxy DNS Lookup Failed", string.Format("[Fiddler] DNS Lookup for Proxy \"{0}\" failed. {1}", Utilities.HtmlEncode(sGatewayOverride), NetworkInterface.GetIsNetworkAvailable() ? string.Empty : "The system reports that no network connection is available. \n"));
									this._esState.CurrentState = ServerChatter.StateConnecting.Failed;
									break;
								}
							}
						}
					}
					else if (!this.m_session.bypassGateway)
					{
						int iGWDTC = Environment.TickCount;
						string sScheme = this.m_session.oRequest.headers.UriScheme;
						if (sScheme == "http" && this.m_session.HTTPMethodIs("CONNECT"))
						{
							sScheme = "https";
						}
						IPEndPoint ipepMyGateway = FiddlerApplication.oProxy.FindGatewayForOrigin(sScheme, this._esState.sTarget);
						if (ipepMyGateway != null)
						{
							if (CONFIG.bDebugSpew)
							{
								FiddlerApplication.DebugSpew("Using Gateway: '{0}' for request to '{1}'", new object[]
								{
									ipepMyGateway.ToString(),
									this.m_session.fullUrl
								});
							}
							this._esState.ipepGateways = new IPEndPoint[1];
							this._esState.ipepGateways[0] = ipepMyGateway;
						}
						this.m_session.Timers.GatewayDeterminationTime = Environment.TickCount - iGWDTC;
					}
					ServerChatter.MakeConnectionExecutionState esState = this._esState;
					esState.CurrentState += 1;
					break;
				}
				case ServerChatter.StateConnecting.EndFindGateway:
					if (this._esState.ipepGateways != null)
					{
						this.m_bWasForwarded = true;
					}
					else if (this.m_session.isFTP)
					{
						this._esState.CurrentState = ServerChatter.StateConnecting.Established;
						break;
					}
					this._esState.iServerPort = (this.m_session.isHTTPS ? 443 : (this.m_session.isFTP ? 21 : 80));
					Utilities.CrackHostAndPort(this._esState.sTarget, out this._esState.sServerHostname, ref this._esState.iServerPort);
					if (this._esState.ipepGateways != null)
					{
						if (this.m_session.isHTTPS || this._esState.bUseSOCKSGateway)
						{
							this._esState.sSuitableConnectionID = string.Format("{0}:{1}->{2}/{3}:{4}", new object[]
							{
								this._esState.bUseSOCKSGateway ? "socks" : "gw",
								this._esState.ipepGateways[0],
								this.m_session.isHTTPS ? "https" : "http",
								this._esState.sServerHostname,
								this._esState.iServerPort
							});
						}
						else
						{
							this._esState.sSuitableConnectionID = string.Format("gw:{0}->*", this._esState.ipepGateways[0]);
						}
					}
					else
					{
						this._esState.sSuitableConnectionID = string.Format("direct->http{0}/{1}:{2}{3}", new object[]
						{
							this.m_session.isHTTPS ? "s" : string.Empty,
							this._esState.sServerHostname,
							this._esState.iServerPort,
							this._esState.sPoolKeyContext
						});
					}
					if (this.pipeServer != null && !this.m_session.oFlags.ContainsKey("X-ServerPipe-Marriage-Trumps-All") && !ServerChatter.SIDsMatch(this.m_session.LocalProcessID, this._esState.sSuitableConnectionID, this.pipeServer.sPoolKey))
					{
						FiddlerApplication.Log.LogFormat("Session #{0} detaching ServerPipe. Had: '{1}' but needs: '{2}'", new object[]
						{
							this.m_session.id,
							this.pipeServer.sPoolKey,
							this._esState.sSuitableConnectionID
						});
						this.m_session.oFlags["X-Divorced-ServerPipe"] = string.Format("Had: '{0}' but needs: '{1}'", this.pipeServer.sPoolKey, this._esState.sSuitableConnectionID);
						this._detachServerPipe();
					}
					if (this.pipeServer == null && !this.m_session.oFlags.ContainsKey("X-Bypass-ServerPipe-Reuse-Pool"))
					{
						this.pipeServer = Proxy.htServerPipePool.TakePipe(this._esState.sSuitableConnectionID, this.m_session.LocalProcessID, this.m_session.id);
					}
					if (this.pipeServer != null)
					{
						this.m_session.Timers.ServerConnected = this.pipeServer.dtConnected;
						StringDictionary oFlags = this.m_session.oFlags;
						oFlags["x-serversocket"] = oFlags["x-serversocket"] + "REUSE " + this.pipeServer._sPipeName;
						if (this.pipeServer.Address != null && !this.pipeServer.isConnectedToGateway)
						{
							this.m_session.m_hostIP = this.pipeServer.Address.ToString();
							this.m_session.oFlags["x-hostIP"] = this.m_session.m_hostIP;
						}
						if (CONFIG.bDebugSpew)
						{
							FiddlerApplication.DebugSpew("Session #{0} ({1} {2}): Reusing {3}\r\n", new object[]
							{
								this.m_session.id,
								this.m_session.RequestMethod,
								this.m_session.fullUrl,
								this.pipeServer.ToString()
							});
						}
						this._esState.CurrentState = ServerChatter.StateConnecting.Established;
					}
					else
					{
						if (this.m_session.oFlags.ContainsKey("x-serversocket"))
						{
							StringDictionary oFlags = this.m_session.oFlags;
							oFlags["x-serversocket"] = oFlags["x-serversocket"] + "*NEW*";
						}
						ServerChatter.MakeConnectionExecutionState esState2 = this._esState;
						esState2.CurrentState += 1;
					}
					break;
				case ServerChatter.StateConnecting.BeginGenerateIPEndPoint:
					if (this._esState.ipepGateways != null)
					{
						this._esState.arrIPEPDest = this._esState.ipepGateways;
						this._esState.CurrentState = ServerChatter.StateConnecting.BeginConnectSocket;
					}
					else if (this._esState.iServerPort < 0 || this._esState.iServerPort > 65535)
					{
						this.m_session.oRequest.FailSession(400, "Fiddler - Bad Request", "[Fiddler] HTTP Request specified an invalid port number.");
						this._esState.CurrentState = ServerChatter.StateConnecting.Failed;
					}
					else
					{
						try
						{
							bool bAsyncDNS = DNSResolver.ResolveWentAsync(this._esState, this.m_session.Timers, delegate(IAsyncResult iar)
							{
								if (this._esState == null)
								{
									this._esState = new ServerChatter.MakeConnectionExecutionState();
									this._esState.CurrentState = ServerChatter.StateConnecting.Failed;
								}
								else
								{
									this._esState.CurrentState = ServerChatter.StateConnecting.EndGenerateIPEndPoint;
								}
								this.RunConnectionStateMachine();
							});
							if (bAsyncDNS)
							{
								bAsyncExit = true;
								break;
							}
						}
						catch (Exception eX)
						{
							this._esState.lastException = eX;
							this._esState.CurrentState = ServerChatter.StateConnecting.EndGenerateIPEndPoint;
							break;
						}
						this._esState.CurrentState = ServerChatter.StateConnecting.EndGenerateIPEndPoint;
					}
					break;
				case ServerChatter.StateConnecting.EndGenerateIPEndPoint:
					if (this._esState.lastException != null)
					{
						this.m_session.oRequest.FailSession(502, "Fiddler - DNS Lookup Failed", string.Format("[Fiddler] DNS Lookup for \"{0}\" failed. {1}{2}", Utilities.HtmlEncode(this._esState.sServerHostname), NetworkInterface.GetIsNetworkAvailable() ? string.Empty : "The system reports that no network connection is available. \n", Utilities.DescribeException(this._esState.lastException)));
						this._esState.CurrentState = ServerChatter.StateConnecting.Failed;
					}
					else
					{
						ServerChatter.MakeConnectionExecutionState esState3 = this._esState;
						esState3.CurrentState += 1;
					}
					break;
				case ServerChatter.StateConnecting.BeginConnectSocket:
					try
					{
						if (this.m_session.isHTTPS && this.m_bWasForwarded)
						{
							ManualResetEvent oWaitForTunnel = new ManualResetEvent(false);
							string sUA = this.m_session.oRequest["User-Agent"];
							string sProxyCreds = FiddlerApplication.Prefs.GetStringPref("fiddler.composer.HTTPSProxyBasicCreds", null);
							if (!string.IsNullOrEmpty(sProxyCreds))
							{
								sProxyCreds = Convert.ToBase64String(Encoding.UTF8.GetBytes(sProxyCreds));
							}
							HTTPRequestHeaders oRH = new HTTPRequestHeaders();
							oRH.HTTPMethod = "CONNECT";
							string sHostname = this._esState.sServerHostname;
							if (sHostname.Contains(":") && !sHostname.Contains("["))
							{
								sHostname = "[" + sHostname + "]";
							}
							oRH.RequestPath = sHostname + ":" + this._esState.iServerPort.ToString();
							oRH["Host"] = sHostname + ":" + this._esState.iServerPort.ToString();
							if (!string.IsNullOrEmpty(sUA))
							{
								oRH["User-Agent"] = sUA;
							}
							if (!string.IsNullOrEmpty(sProxyCreds))
							{
								oRH["Proxy-Authorization"] = "Basic " + sProxyCreds;
							}
							Session oTunnel = new Session(oRH, null, false);
							oTunnel.SetBitFlag(SessionFlags.RequestGeneratedByFiddler, true);
							oTunnel.oFlags["X-AutoAuth"] = this.m_session["X-AutoAuth"];
							oTunnel.oFlags["x-CreatedTunnel"] = "Fiddler-Created-This-CONNECT-Tunnel";
							int iFinalResultCode = 0;
							oTunnel.OnCompleteTransaction += delegate(object s, EventArgs oEA)
							{
								Session sessFinal = s as Session;
								if (sessFinal == null)
								{
									throw new InvalidDataException("Session must not be null when OnCompleteTransaction is called");
								}
								iFinalResultCode = sessFinal.responseCode;
								if (200 == iFinalResultCode)
								{
									ServerChatter oSC = sessFinal.oResponse;
									if (oSC != null)
									{
										ServerPipe oPipe = oSC.pipeServer;
										if (oPipe != null)
										{
											object esStateLock = this._esStateLock;
											lock (esStateLock)
											{
												if (this._esState != null)
												{
													this._esState.newSocket = oPipe.GetRawSocket();
												}
											}
											oSC.pipeServer = null;
										}
									}
								}
								oWaitForTunnel.Set();
							};
							ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback(oTunnel.Execute), null);
							if (!oWaitForTunnel.WaitOne(30000, false))
							{
								throw new Exception("Upstream Gateway timed out CONNECT.");
							}
							if (iFinalResultCode != 200)
							{
								throw new Exception("Upstream Gateway refused requested CONNECT. " + iFinalResultCode.ToString());
							}
							if (this._esState.newSocket == null)
							{
								throw new Exception("Upstream Gateway CONNECT failed.");
							}
							this.m_session.oFlags["x-CreatedTunnel"] = "Fiddler-Created-A-CONNECT-Tunnel";
						}
						else
						{
							this._esState.newSocket = ServerChatter.CreateConnectedSocket(this._esState.arrIPEPDest, this.m_session);
							if (this._esState.bUseSOCKSGateway)
							{
								this._esState.newSocket = this._SOCKSifyConnection(this._esState.sServerHostname, this._esState.iServerPort, this._esState.newSocket);
							}
						}
						this.pipeServer = new ServerPipe(this._esState.newSocket, "ServerPipe#" + this.m_session.id.ToString(), this.m_bWasForwarded, this._esState.sSuitableConnectionID);
						if (this._esState.bUseSOCKSGateway)
						{
							this.pipeServer.isConnectedViaSOCKS = true;
						}
						if (this.m_session.isHTTPS)
						{
							SslProtocols sslprot = SslProtocols.None;
							if (this.m_session.oRequest != null && this.m_session.oRequest.pipeClient != null)
							{
								sslprot = this.m_session.oRequest.pipeClient.SecureProtocol;
							}
							if (!this.pipeServer.SecureExistingConnection(this.m_session, this._esState.sServerHostname, this.m_session.oFlags["https-Client-Certificate"], sslprot, ref this.m_session.Timers.HTTPSHandshakeTime))
							{
								string sError = "Failed to negotiate HTTPS connection with server.";
								if (!Utilities.IsNullOrEmpty(this.m_session.responseBodyBytes))
								{
									sError += Encoding.UTF8.GetString(this.m_session.responseBodyBytes);
								}
								throw new SecurityException(sError);
							}
						}
						this._esState.CurrentState = ServerChatter.StateConnecting.Established;
						break;
					}
					catch (Exception eX2)
					{
						this._smHandleConnectionException(eX2);
						this._esState.CurrentState = ServerChatter.StateConnecting.Failed;
						break;
					}
					goto IL_DFB;
				case ServerChatter.StateConnecting.EndConnectSocket:
					goto IL_DFB;
				case ServerChatter.StateConnecting.Established:
					this._smNotifyCSMDone();
					bAsyncExit = true;
					break;
				case ServerChatter.StateConnecting.Failed:
					this._smNotifyCSMDone();
					bAsyncExit = true;
					break;
				default:
				{
					Exception eeX = new InvalidOperationException(string.Concat(new string[]
					{
						"Fatal Error in Session #",
						this.m_session.id.ToString(),
						". In RunConnectionStateMachine, _esState is ",
						this._esState.CurrentState.ToString(),
						"\n",
						this.m_session.fullUrl,
						"\r\nState: ",
						this.m_session.state.ToString()
					}));
					FiddlerApplication.Log.LogString(eeX.ToString());
					bAsyncExit = true;
					break;
				}
				}
				IL_ECC:
				if (bAsyncExit)
				{
					return;
				}
				continue;
				IL_DFB:
				ServerChatter.MakeConnectionExecutionState esState4 = this._esState;
				esState4.CurrentState += 1;
				goto IL_ECC;
			}
			Exception eX3 = new NullReferenceException(string.Concat(new string[]
			{
				"Fatal Error in Session #",
				this.m_session.id.ToString(),
				". Looping RunConnectionStateMachine, _esState null for ",
				this.m_session.fullUrl,
				"\r\nState: ",
				this.m_session.state.ToString()
			}));
			FiddlerApplication.Log.LogString(eX3.ToString());
			throw eX3;
		}

		// Token: 0x060003B5 RID: 949 RVA: 0x00023CC4 File Offset: 0x00021EC4
		private void _smNotifyCSMDone()
		{
			AsyncCallback acb = null;
			object esStateLock = this._esStateLock;
			lock (esStateLock)
			{
				if (this._esState != null)
				{
					acb = this._esState.OnDone;
					this._esState = null;
				}
			}
			if (acb != null)
			{
				acb(null);
			}
		}

		/// <summary>
		/// If a Connection cannot be established, we need to report the failure to our caller
		/// </summary>
		/// <param name="eX"></param>
		// Token: 0x060003B6 RID: 950 RVA: 0x00023D28 File Offset: 0x00021F28
		private void _smHandleConnectionException(Exception eX)
		{
			string sAdditionalTips = string.Empty;
			bool bGiveNetworkProxyAdvice = true;
			if (eX is SecurityException)
			{
				bGiveNetworkProxyAdvice = false;
			}
			SocketException eXS = eX as SocketException;
			if (eXS != null)
			{
				if (eXS.SocketErrorCode == SocketError.AccessDenied || eXS.SocketErrorCode == SocketError.NetworkDown || eXS.SocketErrorCode == SocketError.InvalidArgument)
				{
					sAdditionalTips = string.Format("A Firewall may be blocking Fiddler's traffic.<br />Error: {0} (0x{1:x}).", eXS.SocketErrorCode, (int)eXS.SocketErrorCode);
					bGiveNetworkProxyAdvice = false;
				}
				else
				{
					sAdditionalTips = string.Format("<br />Error: {0} (0x{1:x}).", eXS.SocketErrorCode, (int)eXS.SocketErrorCode);
				}
			}
			string sStatusLine;
			string sErrorBody;
			if (this.m_bWasForwarded)
			{
				sStatusLine = "Fiddler - Gateway Connection Failed";
				sErrorBody = "[Fiddler] The connection to the upstream proxy/gateway failed.";
				if (bGiveNetworkProxyAdvice)
				{
					sAdditionalTips = string.Format("Closing Fiddler, changing your system proxy settings, and restarting Fiddler may help. {0}", sAdditionalTips);
				}
			}
			else
			{
				sStatusLine = "Fiddler - Connection Failed";
				sErrorBody = string.Format("[Fiddler] The connection to '{0}' failed.", Utilities.HtmlEncode(this._esState.sServerHostname));
			}
			this.m_session.oRequest.FailSession(502, sStatusLine, string.Format("{0} {1} <br />{2}", sErrorBody, sAdditionalTips, Utilities.HtmlEncode(Utilities.DescribeException(eX))));
		}

		/// <summary>
		/// Given an address list and port, attempts to create a socket to the first responding host in the list (retrying via DNS Failover if needed).
		/// </summary>
		/// <param name="arrDest">IPEndpoints to attempt to reach</param>
		/// <param name="_oSession">Session object to annotate with timings and errors</param>
		/// <returns>Connected Socket. Throws Exceptions on errors.</returns>
		// Token: 0x060003B7 RID: 951 RVA: 0x00023E3C File Offset: 0x0002203C
		private static Socket CreateConnectedSocket(IPEndPoint[] arrDest, Session _oSession)
		{
			Socket oSocket = null;
			bool bGotConnection = false;
			Stopwatch oSW = Stopwatch.StartNew();
			Exception exLast = null;
			foreach (IPEndPoint ipepDest in arrDest)
			{
				try
				{
					oSocket = new Socket(ipepDest.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					oSocket.NoDelay = true;
					if (FiddlerApplication.oProxy._DefaultEgressEndPoint != null)
					{
						oSocket.Bind(FiddlerApplication.oProxy._DefaultEgressEndPoint);
					}
					oSocket.Connect(ipepDest);
					_oSession.m_hostIP = ipepDest.Address.ToString();
					_oSession.oFlags["x-hostIP"] = _oSession.m_hostIP;
					if (ServerChatter.s_SO_RCVBUF_Option >= 0)
					{
						oSocket.ReceiveBufferSize = ServerChatter.s_SO_RCVBUF_Option;
					}
					if (ServerChatter.s_SO_SNDBUF_Option >= 0)
					{
						oSocket.SendBufferSize = ServerChatter.s_SO_SNDBUF_Option;
					}
					FiddlerApplication.DoAfterSocketConnect(_oSession, oSocket);
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("[ServerPipe]\n SendBufferSize:\t{0}\n ReceiveBufferSize:\t{1}\n SendTimeout:\t{2}\n ReceiveTimeOut:\t{3}\n NoDelay:\t{4}\n EgressEP:\t{5}\n", new object[]
						{
							oSocket.SendBufferSize,
							oSocket.ReceiveBufferSize,
							oSocket.SendTimeout,
							oSocket.ReceiveTimeout,
							oSocket.NoDelay,
							(FiddlerApplication.oProxy._DefaultEgressEndPoint != null) ? FiddlerApplication.oProxy._DefaultEgressEndPoint.ToString() : "none"
						});
					}
					bGotConnection = true;
					break;
				}
				catch (Exception eX)
				{
					exLast = eX;
					if (!FiddlerApplication.Prefs.GetBoolPref("fiddler.network.dns.fallback", true))
					{
						break;
					}
					_oSession.oFlags["x-DNS-Failover"] = _oSession.oFlags["x-DNS-Failover"] + "+1";
				}
			}
			_oSession.Timers.ServerConnected = DateTime.Now;
			_oSession.Timers.TCPConnectTime = (int)oSW.ElapsedMilliseconds;
			if (!bGotConnection)
			{
				throw exLast;
			}
			return oSocket;
		}

		/// <summary>
		/// If the Session was configured to stream the request body, we need to read from the client
		/// and send it to the server here.
		/// </summary>
		/// <returns>
		/// FALSE on transfer error, TRUE otherwise.
		/// </returns>
		// Token: 0x060003B8 RID: 952 RVA: 0x00024018 File Offset: 0x00022218
		internal bool StreamRequestBody()
		{
			long cBytesRemaining = 0L;
			long cBytesSentToServer = 0L;
			ChunkReader oChunkReader = null;
			if (long.TryParse(this.m_session.oRequest["Content-Length"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out cBytesRemaining))
			{
				cBytesRemaining -= (long)this.m_session.requestBodyBytes.Length;
			}
			else if (this.m_session.oRequest.headers.ExistsAndContains("Transfer-Encoding", "chunked"))
			{
				oChunkReader = new ChunkReader();
				oChunkReader.pushBytes(this.m_session.requestBodyBytes, 0, this.m_session.requestBodyBytes.Length);
			}
			else
			{
				cBytesRemaining = 0L;
			}
			if (cBytesRemaining < 1L && oChunkReader == null)
			{
				return true;
			}
			bool bUpdateRequestBody = !this.m_session.oFlags.ContainsKey("log-drop-request-body");
			PipeReadBuffer _requestData = null;
			if (bUpdateRequestBody)
			{
				_requestData = new PipeReadBuffer(true);
				_requestData.Write(this.m_session.requestBodyBytes, 0, this.m_session.requestBodyBytes.Length);
			}
			else
			{
				if (!Utilities.IsNullOrEmpty(this.m_session.requestBodyBytes))
				{
					cBytesSentToServer = (long)this.m_session.requestBodyBytes.Length;
				}
				this.m_session.requestBodyBytes = Utilities.emptyByteArray;
				this.m_session.SetBitFlag(SessionFlags.RequestBodyDropped, true);
			}
			ClientPipe pipeClient = this.m_session.oRequest.pipeClient;
			if (pipeClient == null)
			{
				return false;
			}
			bool bAbort = false;
			bool bDone = false;
			byte[] _arrReadFromPipe = new byte[ClientChatter.s_cbClientReadBuffer];
			int cbLastReceive = 0;
			SessionTimers.NetTimestamps oNTS = SessionTimers.NetTimestamps.FromCopy(this.m_session.Timers.ClientReads);
			Stopwatch oSW = Stopwatch.StartNew();
			for (;;)
			{
				try
				{
					cbLastReceive = pipeClient.Receive(_arrReadFromPipe);
					oNTS.AddRead(oSW.ElapsedMilliseconds, cbLastReceive);
				}
				catch (SocketException eeX)
				{
					bAbort = true;
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("STREAMReadRequest {0} threw #{1} - {2}", new object[]
						{
							pipeClient.ToString(),
							eeX.ErrorCode,
							eeX.Message
						});
					}
					if (eeX.SocketErrorCode == SocketError.TimedOut)
					{
						this.m_session.oFlags["X-ClientPipeError"] = string.Format("STREAMReadRequest timed out; total of ?{0}? bytes read from client.", _requestData.Length);
						this.m_session.oRequest.FailSession(408, "Request Timed Out", "The client failed to send a complete request before the timeout period elapsed.");
						return false;
					}
					goto IL_544;
				}
				catch (Exception ex)
				{
					bAbort = true;
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("STREAMReadRequest {0} threw {1}", new object[]
						{
							pipeClient.ToString(),
							ex.Message
						});
					}
					goto IL_544;
				}
				goto IL_265;
				IL_544:
				if (bDone || bAbort)
				{
					goto IL_54F;
				}
				continue;
				IL_265:
				if (cbLastReceive < 1)
				{
					bDone = true;
					FiddlerApplication.DoReadRequestBuffer(this.m_session, _arrReadFromPipe, 0);
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("STREAMReadRequest {0} returned {1}", new object[]
						{
							pipeClient.ToString(),
							cbLastReceive
						});
					}
					goto IL_544;
				}
				else
				{
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("STREAMREAD FROM {0}:\n{1}", new object[]
						{
							pipeClient,
							Utilities.ByteArrayToHexView(_arrReadFromPipe, 32, cbLastReceive)
						});
					}
					if (!FiddlerApplication.DoReadRequestBuffer(this.m_session, _arrReadFromPipe, cbLastReceive))
					{
						break;
					}
					if (oChunkReader != null)
					{
						oChunkReader.pushBytes(_arrReadFromPipe, 0, cbLastReceive);
					}
					if (oChunkReader != null)
					{
						if (oChunkReader.state == ChunkedTransferState.Overread)
						{
							byte[] arrExcess = new byte[oChunkReader.getOverage()];
							FiddlerApplication.Log.LogFormat("HTTP Pipelining Client detected; {0:N0} bytes of excess data on client socket for Session #{1}.", new object[]
							{
								arrExcess.Length,
								this.m_session.id
							});
							Buffer.BlockCopy(_arrReadFromPipe, cbLastReceive - arrExcess.Length, arrExcess, 0, arrExcess.Length);
							cbLastReceive -= arrExcess.Length;
						}
					}
					else if ((long)cbLastReceive > cBytesRemaining)
					{
						byte[] arrExcess2 = new byte[(long)cbLastReceive - cBytesRemaining];
						FiddlerApplication.Log.LogFormat("HTTP Pipelining Client detected; {0:N0} bytes of excess data on client socket for Session #{1}.", new object[]
						{
							arrExcess2.Length,
							this.m_session.id
						});
						Buffer.BlockCopy(_arrReadFromPipe, (int)cBytesRemaining, arrExcess2, 0, arrExcess2.Length);
						cbLastReceive = (int)cBytesRemaining;
					}
					if (bUpdateRequestBody)
					{
						_requestData.Write(_arrReadFromPipe, 0, cbLastReceive);
					}
					if (this.pipeServer != null)
					{
						try
						{
							this.pipeServer.Send(_arrReadFromPipe, 0, cbLastReceive);
						}
						catch (SocketException eeX2)
						{
							bAbort = true;
							FiddlerApplication.Log.LogFormat("STREAMSendRequest {0} threw #{1} - {2}", new object[]
							{
								this.pipeServer.ToString(),
								eeX2.ErrorCode,
								eeX2.Message
							});
							if (CONFIG.bDebugSpew)
							{
								FiddlerApplication.DebugSpew("STREAMSendRequest {0} threw #{1} - {2}", new object[]
								{
									this.pipeServer.ToString(),
									eeX2.ErrorCode,
									eeX2.Message
								});
							}
							goto IL_544;
						}
						catch (Exception ex2)
						{
							bAbort = true;
							FiddlerApplication.Log.LogFormat("STREAMSendRequest {0} threw {1}", new object[]
							{
								this.pipeServer.ToString(),
								ex2.Message
							});
							if (CONFIG.bDebugSpew)
							{
								FiddlerApplication.DebugSpew("STREAMSendRequest {0} threw {1}", new object[]
								{
									this.pipeServer.ToString(),
									ex2.Message
								});
							}
							goto IL_544;
						}
					}
					cBytesSentToServer += (long)cbLastReceive;
					if (oChunkReader == null)
					{
						cBytesRemaining -= (long)cbLastReceive;
						FiddlerApplication.DebugSpew("Streaming Session #{0} to server. Wrote {1} bytes, {2} remain...", new object[]
						{
							this.m_session.id,
							cbLastReceive,
							cBytesRemaining
						});
						bDone = cBytesRemaining < 1L;
						goto IL_544;
					}
					bDone = oChunkReader.state >= ChunkedTransferState.Completed;
					goto IL_544;
				}
			}
			FiddlerApplication.DebugSpew("ReadRequest() aborted by OnReadRequestBuffer");
			return false;
			IL_54F:
			_arrReadFromPipe = null;
			oSW = null;
			this.m_session.Timers.ClientReads = oNTS;
			this.m_session.Timers.ClientDoneRequest = DateTime.Now;
			if (oChunkReader != null)
			{
				this.m_session["X-UnchunkedBodySize"] = oChunkReader.getEntityLength().ToString();
				if (oChunkReader.state == ChunkedTransferState.Malformed)
				{
					bAbort = true;
				}
			}
			if (bAbort)
			{
				FiddlerApplication.DebugSpew("Reading from client or writing to server set bAbort");
				return false;
			}
			if (bUpdateRequestBody)
			{
				this.m_session.requestBodyBytes = _requestData.ToArray();
			}
			else
			{
				this.m_session.oFlags["x-RequestBodyLength"] = cBytesSentToServer.ToString("N0");
			}
			return true;
		}

		/// <summary>
		/// Sends (or resends) the Request to the server or upstream proxy. If the request is a CONNECT and there's no
		/// gateway, this method ~only~ establishes the connection to the target, but does NOT send a request.
		///
		/// Note: THROWS on failures
		/// </summary>
		// Token: 0x060003B9 RID: 953 RVA: 0x00024650 File Offset: 0x00022850
		internal void SendRequest()
		{
			if (this.m_session.isFTP && !this.m_session.isFlagSet(SessionFlags.SentToGateway))
			{
				return;
			}
			if (this.pipeServer == null)
			{
				throw new InvalidOperationException("Cannot SendRequest unless pipeServer is set!");
			}
			this.pipeServer.IncrementUse(this.m_session.id);
			this.pipeServer.setTimeouts();
			this.m_session.Timers.ServerConnected = this.pipeServer.dtConnected;
			this.m_bWasForwarded = this.pipeServer.isConnectedToGateway;
			this.m_session.SetBitFlag(SessionFlags.ServerPipeReused, this.pipeServer.iUseCount > 1U);
			this.m_session.SetBitFlag(SessionFlags.SentToGateway, this.m_bWasForwarded);
			if (this.pipeServer.isConnectedViaSOCKS)
			{
				this.m_session.SetBitFlag(SessionFlags.SentToSOCKSGateway, true);
			}
			if (!this.m_bWasForwarded && !this.m_session.isHTTPS)
			{
				this.m_session.oRequest.headers.RenameHeaderItems("Proxy-Connection", "Connection");
			}
			if (!this.pipeServer.isAuthenticated)
			{
				string __requestAuth = this.m_session.oRequest.headers["Authorization"];
				if (__requestAuth != null && __requestAuth.OICStartsWith("N"))
				{
					this.pipeServer.MarkAsAuthenticated(this.m_session.LocalProcessID);
				}
			}
			if (this.m_session.oFlags.ContainsKey("request-trickle-delay"))
			{
				int iDelayPerK = int.Parse(this.m_session.oFlags["request-trickle-delay"]);
				this.pipeServer.TransmitDelay = iDelayPerK;
			}
			this.m_session.Timers.FiddlerBeginRequest = DateTime.Now;
			if (this.m_bWasForwarded || !this.m_session.HTTPMethodIs("CONNECT"))
			{
				bool bSendFullyQualifiedUrl = this.m_bWasForwarded && !this.m_session.isHTTPS;
				byte[] arrHeaderBytes = this.m_session.oRequest.headers.ToByteArray(true, true, bSendFullyQualifiedUrl, this.m_session.oFlags["X-OverrideHost"]);
				this.pipeServer.Send(arrHeaderBytes);
				if (!Utilities.IsNullOrEmpty(this.m_session.requestBodyBytes))
				{
					if (this.m_session.oFlags.ContainsKey("request-body-delay"))
					{
						int iDelayMS = int.Parse(this.m_session.oFlags["request-body-delay"]);
						Thread.Sleep(iDelayMS);
					}
					this.pipeServer.Send(this.m_session.requestBodyBytes);
				}
			}
			this.m_session.oFlags["x-EgressPort"] = this.pipeServer.LocalPort.ToString();
			this.m_session.oFlags["https-Server-ProtocolVersion"] = this.pipeServer.SecureProtocol.ToString();
		}

		/// <summary>
		/// May request be resent on a different connection because the .Send() of the request did not complete?
		/// </summary>
		/// <returns>TRUE if the request may be resent</returns>
		// Token: 0x060003BA RID: 954 RVA: 0x0002492D File Offset: 0x00022B2D
		internal bool _MayRetryWhenSendFailed()
		{
			return this.bServerSocketReused && this.m_session.state != SessionStates.Aborted;
		}

		/// <summary>
		/// Performs a SOCKSv4A handshake on the socket
		/// </summary>
		// Token: 0x060003BB RID: 955 RVA: 0x0002494C File Offset: 0x00022B4C
		private Socket _SOCKSifyConnection(string sServerHostname, int iServerPort, Socket newSocket)
		{
			this.m_bWasForwarded = false;
			FiddlerApplication.DebugSpew("Creating SOCKS connection for {0}:{1}.", new object[] { sServerHostname, iServerPort });
			byte[] arrSOCKSHandshake = ServerChatter._BuildSOCKS4ConnectHandshakeForTarget(sServerHostname, iServerPort);
			newSocket.Send(arrSOCKSHandshake);
			byte[] oResponse = new byte[64];
			int iReadCount = newSocket.Receive(oResponse);
			if (iReadCount > 1 && oResponse[0] == 0 && oResponse[1] == 90)
			{
				if (iReadCount > 7)
				{
					string addrDest = string.Format("{0}.{1}.{2}.{3}", new object[]
					{
						oResponse[4],
						oResponse[5],
						oResponse[6],
						oResponse[7]
					});
					this.m_session.m_hostIP = addrDest;
					this.m_session.oFlags["x-hostIP"] = addrDest;
				}
				return newSocket;
			}
			try
			{
				newSocket.Close();
			}
			catch
			{
			}
			string sError = string.Empty;
			if (iReadCount > 1 && oResponse[0] == 0)
			{
				int iError = (int)oResponse[1];
				sError = string.Format("Gateway returned error 0x{0:x}", iError);
				switch (iError)
				{
				case 91:
					sError += "-'request rejected or failed'";
					break;
				case 92:
					sError += "-'request failed because client is not running identd (or not reachable from the server)'";
					break;
				case 93:
					sError += "-'request failed because client's identd could not confirm the user ID string in the request'";
					break;
				default:
					sError += "-'unknown'";
					break;
				}
			}
			else if (iReadCount > 0)
			{
				sError = "Gateway returned a malformed response:\n" + Utilities.ByteArrayToHexView(oResponse, 8, iReadCount);
			}
			else
			{
				sError = "Gateway returned no data.";
			}
			throw new InvalidDataException("SOCKS gateway failed: " + sError);
		}

		/// <summary>
		/// Build the SOCKS4 outbound connection handshake as a byte array.
		/// http://en.wikipedia.org/wiki/SOCKS#SOCKS4a
		/// </summary>
		// Token: 0x060003BC RID: 956 RVA: 0x00024AD8 File Offset: 0x00022CD8
		private static byte[] _BuildSOCKS4ConnectHandshakeForTarget(string sTargetHost, int iPort)
		{
			byte[] arrHostname = Encoding.ASCII.GetBytes(sTargetHost);
			byte[] arrHandshake = new byte[10 + arrHostname.Length];
			arrHandshake[0] = 4;
			arrHandshake[1] = 1;
			arrHandshake[2] = (byte)(iPort >> 8);
			arrHandshake[3] = (byte)(iPort & 255);
			arrHandshake[7] = 127;
			Buffer.BlockCopy(arrHostname, 0, arrHandshake, 9, arrHostname.Length);
			return arrHandshake;
		}

		/// <summary>
		/// Replaces body with an error message
		/// </summary>
		/// <param name="sRemoteError">Error to send if client was remote</param>
		/// <param name="sTrustedError">Error to send if cilent was local</param>
		// Token: 0x060003BD RID: 957 RVA: 0x00024B2C File Offset: 0x00022D2C
		private void _ReturnFileReadError(string sRemoteError, string sTrustedError)
		{
			this.Initialize(false);
			string sErrorBody;
			if (this.m_session.LocalProcessID > 0 || this.m_session.isFlagSet(SessionFlags.RequestGeneratedByFiddler))
			{
				sErrorBody = sTrustedError;
			}
			else
			{
				sErrorBody = sRemoteError;
			}
			sErrorBody = sErrorBody.PadRight(512, ' ');
			this.m_session.responseBodyBytes = Encoding.UTF8.GetBytes(sErrorBody);
			this.m_inHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
			this.m_inHeaders.SetStatus(404, "Not Found");
			this.m_inHeaders.Add("Content-Length", this.m_session.responseBodyBytes.Length.ToString());
			this.m_inHeaders.Add("Cache-Control", "max-age=0, must-revalidate");
		}

		/// <summary>
		/// The Session object will call this method if it wishes to stream a file from disk instead
		/// of loading it into memory. This method sets default headers.
		/// </summary>
		/// <param name="sFilename"></param>
		// Token: 0x060003BE RID: 958 RVA: 0x00024BEC File Offset: 0x00022DEC
		internal void GenerateHeadersForLocalFile(string sFilename)
		{
			FileInfo oFI = new FileInfo(sFilename);
			this.Initialize(false);
			this.m_inHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
			this.m_inHeaders.SetStatus(200, "OK with automatic headers");
			this.m_inHeaders["Date"] = DateTime.UtcNow.ToString("r");
			this.m_inHeaders["Content-Length"] = oFI.Length.ToString();
			this.m_inHeaders["Cache-Control"] = "max-age=0, must-revalidate";
			string sContentTypeHint = Utilities.ContentTypeForFilename(sFilename);
			if (sContentTypeHint != null)
			{
				this.m_inHeaders["Content-Type"] = sContentTypeHint;
			}
		}

		// Token: 0x060003BF RID: 959 RVA: 0x00024C9C File Offset: 0x00022E9C
		private bool ReadResponseFromArray(byte[] arrResponse, bool bAllowBOM, string sContentTypeHint)
		{
			this.Initialize(true);
			int iLength = arrResponse.Length;
			int iStart = 0;
			bool bHasUTF8Preamble = false;
			if (bAllowBOM)
			{
				bHasUTF8Preamble = arrResponse.Length > 3 && arrResponse[0] == 239 && arrResponse[1] == 187 && arrResponse[2] == 191;
				if (bHasUTF8Preamble)
				{
					iStart = 3;
					iLength -= 3;
				}
			}
			bool bSmellsLikeHTTP = arrResponse.Length > 5 + iStart && arrResponse[iStart] == 72 && arrResponse[iStart + 1] == 84 && arrResponse[iStart + 2] == 84 && arrResponse[iStart + 3] == 80 && arrResponse[iStart + 4] == 47;
			if (bHasUTF8Preamble && !bSmellsLikeHTTP)
			{
				iLength += 3;
				iStart = 0;
			}
			this.m_responseData.Capacity = iLength;
			this.m_responseData.Write(arrResponse, iStart, iLength);
			if (bSmellsLikeHTTP && this.HeadersAvailable() && this.ParseResponseForHeaders())
			{
				this.m_session.responseBodyBytes = this.TakeEntity();
			}
			else
			{
				this.Initialize(false);
				this.m_inHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
				this.m_inHeaders.SetStatus(200, "OK with automatic headers");
				this.m_inHeaders["Date"] = DateTime.UtcNow.ToString("r");
				this.m_inHeaders["Content-Length"] = ((long)arrResponse.Length).ToString();
				this.m_inHeaders["Cache-Control"] = "max-age=0, must-revalidate";
				if (sContentTypeHint != null)
				{
					this.m_inHeaders["Content-Type"] = sContentTypeHint;
				}
				this.m_session.responseBodyBytes = arrResponse;
			}
			return true;
		}

		/// <summary>
		/// Loads a HTTP response from a file
		/// </summary>
		/// <param name="sFilename">The name of the file from which a response should be loaded</param>
		/// <returns>False if the file wasn't found. Throws on other errors.</returns>
		// Token: 0x060003C0 RID: 960 RVA: 0x00024E14 File Offset: 0x00023014
		internal bool ReadResponseFromFile(string sFilename, string sOptionalContentTypeHint)
		{
			if (!File.Exists(sFilename))
			{
				this._ReturnFileReadError("Fiddler - The requested file was not found.", "Fiddler - The file '" + sFilename + "' was not found.");
				return false;
			}
			byte[] arrTmp;
			try
			{
				arrTmp = File.ReadAllBytes(sFilename);
			}
			catch (Exception eX)
			{
				this._ReturnFileReadError("Fiddler - The requested file could not be read.", "Fiddler - The requested file could not be read. " + Utilities.DescribeException(eX));
				return false;
			}
			return this.ReadResponseFromArray(arrTmp, true, sOptionalContentTypeHint);
		}

		// Token: 0x060003C1 RID: 961 RVA: 0x00024E8C File Offset: 0x0002308C
		internal bool ReadResponseFromStream(Stream oResponse, string sContentTypeHint)
		{
			MemoryStream oMS = new MemoryStream();
			byte[] buffer = new byte[32768];
			int bytesRead;
			while ((bytesRead = oResponse.Read(buffer, 0, buffer.Length)) > 0)
			{
				oMS.Write(buffer, 0, bytesRead);
			}
			byte[] arrTmp = oMS.ToArray();
			return this.ReadResponseFromArray(arrTmp, false, sContentTypeHint);
		}

		/// <summary>
		/// Reads the response from the ServerPipe.
		/// </summary>
		/// <returns>TRUE if a response was read</returns>
		// Token: 0x060003C2 RID: 962 RVA: 0x00024ED8 File Offset: 0x000230D8
		internal bool ReadResponse()
		{
			if (this.pipeServer == null)
			{
				return this.IsWorkableFTPRequest();
			}
			bool bGotFIN = false;
			bool bAbort = false;
			bool bDiscardResponseBodyBytes = false;
			bool bLeakWriteFailed = false;
			byte[] _arrReadFromPipe = new byte[ServerChatter.s_cbServerReadBuffer];
			SessionTimers.NetTimestamps oNTS = new SessionTimers.NetTimestamps();
			Stopwatch oSW = Stopwatch.StartNew();
			do
			{
				try
				{
					int cbLastReceive = this.pipeServer.Receive(_arrReadFromPipe);
					oNTS.AddRead(oSW.ElapsedMilliseconds, cbLastReceive);
					if (this.m_session.Timers.ServerBeginResponse.Ticks == 0L)
					{
						this.m_session.Timers.ServerBeginResponse = DateTime.Now;
					}
					if (cbLastReceive < 1)
					{
						bGotFIN = true;
						FiddlerApplication.DoReadResponseBuffer(this.m_session, _arrReadFromPipe, 0);
						if (CONFIG.bDebugSpew)
						{
							FiddlerApplication.DebugSpew("END-OF-STREAM: Read from {0}: returned {1}", new object[] { this.pipeServer, cbLastReceive });
						}
					}
					else
					{
						if (CONFIG.bDebugSpew)
						{
							FiddlerApplication.DebugSpew("READ {0:N0} FROM {1}:\n{2}", new object[]
							{
								cbLastReceive,
								this.pipeServer,
								Utilities.ByteArrayToHexView(_arrReadFromPipe, 32, cbLastReceive)
							});
						}
						if (!FiddlerApplication.DoReadResponseBuffer(this.m_session, _arrReadFromPipe, cbLastReceive))
						{
							FiddlerApplication.DebugSpew("ReadResponse() aborted by OnReadResponseBuffer");
							this.m_session.state = SessionStates.Aborted;
							return false;
						}
						this.m_responseData.Write(_arrReadFromPipe, 0, cbLastReceive);
						this.m_responseTotalDataCount += (long)cbLastReceive;
						if (this.m_inHeaders == null)
						{
							if (!this.GetHeaders())
							{
								goto IL_66E;
							}
							this.m_session.Timers.FiddlerGotResponseHeaders = DateTime.Now;
							if (this.m_session.state == SessionStates.Aborted && this.m_session.isAnyFlagSet(SessionFlags.ProtocolViolationInResponse))
							{
								return false;
							}
							uint uiLikelySize = 0U;
							if (!this.m_session.HTTPMethodIs("HEAD") && this.m_inHeaders.TryGetEntitySize(out uiLikelySize) && uiLikelySize > 0U)
							{
								uiLikelySize = (uint)((long)this.m_iEntityBodyOffset + Math.Min((long)CONFIG.cbAutoStreamAndForget, (long)((ulong)uiLikelySize)));
								this.m_responseData.HintTotalSize(uiLikelySize);
							}
							FiddlerApplication.DoResponseHeadersAvailable(this.m_session);
							if (407 == this.m_inHeaders.HTTPResponseCode && (!this.m_session.isAnyFlagSet(SessionFlags.SentToGateway) || this.m_session.isHTTPS) && FiddlerApplication.Prefs.GetBoolPref("fiddler.security.ForbidServer407", true))
							{
								this.m_session.SetBitFlag(SessionFlags.ProtocolViolationInResponse, true);
								this._PoisonPipe();
								string sDetailedError = "<plaintext>\n[Fiddler] Security Warning\nA HTTP/407 response was received on a request not sent to an upstream proxy.\nThis may reflect an attempt to compromise your credentials.\nPreference 'fiddler.security.ForbidServer407' is set to true.";
								this.m_session.oRequest.FailSession(500, "Fiddler - Illegal Response", sDetailedError);
								return false;
							}
							this._EnableStreamingIfAppropriate();
							if ((ulong)uiLikelySize > (ulong)((long)CONFIG.cbAutoStreamAndForget))
							{
								this.m_session.oFlags["log-drop-response-body"] = "OverToolsOptionsLimit";
								this.m_session.bBufferResponse = false;
							}
							if (this.m_session.oFlags.ContainsKey("x-breakresponse"))
							{
								this.m_session.bBufferResponse = true;
							}
							if (this.m_session.isAnyFlagSet(SessionFlags.IsRPCTunnel) && 200 == this.m_inHeaders.HTTPResponseCode)
							{
								this.m_session.bBufferResponse = true;
							}
							this.m_session.SetBitFlag(SessionFlags.ResponseStreamed, !this.m_session.bBufferResponse);
							if (!this.m_session.bBufferResponse)
							{
								if (this.m_session.oFlags.ContainsKey("response-trickle-delay"))
								{
									int iDelayPerK = int.Parse(this.m_session.oFlags["response-trickle-delay"]);
									this.m_session.oRequest.pipeClient.TransmitDelay = iDelayPerK;
								}
								if (this.m_session.oFlags.ContainsKey("log-drop-response-body") || FiddlerApplication.Prefs.GetBoolPref("fiddler.network.streaming.ForgetStreamedData", false))
								{
									bDiscardResponseBodyBytes = true;
								}
							}
						}
						if (!bDiscardResponseBodyBytes && this.m_responseData.Length - (long)this.m_iEntityBodyOffset > (long)CONFIG.cbAutoStreamAndForget)
						{
							if (CONFIG.bDebugSpew)
							{
								FiddlerApplication.DebugSpew("While reading response, exceeded CONFIG.cbAutoStreamAndForget when stream reached {0:N0} bytes. Enabling streaming now", new object[] { this.m_responseData.Length });
							}
							this.m_session.SetBitFlag(SessionFlags.ResponseStreamed, true);
							this.m_session.oFlags["log-drop-response-body"] = "OverToolsOptionsLimit";
							this.m_session.bBufferResponse = false;
							bDiscardResponseBodyBytes = true;
							if (this.m_session.oFlags.ContainsKey("response-trickle-delay"))
							{
								int iDelayPerK2 = int.Parse(this.m_session.oFlags["response-trickle-delay"]);
								this.m_session.oRequest.pipeClient.TransmitDelay = iDelayPerK2;
							}
						}
						if (this.m_session.isFlagSet(SessionFlags.ResponseStreamed))
						{
							if (!bLeakWriteFailed && !this.LeakResponseBytes())
							{
								bLeakWriteFailed = true;
							}
							if (bDiscardResponseBodyBytes)
							{
								this.m_session.SetBitFlag(SessionFlags.ResponseBodyDropped, true);
								if (this.m_lngLastChunkInfoOffset > -1L)
								{
									this.ReleaseStreamedChunkedData();
								}
								else if (this.m_inHeaders.ExistsAndContains("Transfer-Encoding", "chunked"))
								{
									this.ReleaseStreamedChunkedData();
								}
								else
								{
									this.ReleaseStreamedData();
								}
							}
						}
					}
				}
				catch (SocketException eXS)
				{
					bAbort = true;
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("ReadResponse() failure {0}", new object[] { Utilities.DescribeException(eXS) });
					}
					if (eXS.SocketErrorCode == SocketError.TimedOut)
					{
						this.m_session.oFlags["X-ServerPipeError"] = "Timed out while reading response.";
					}
					else
					{
						FiddlerApplication.Log.LogFormat("fiddler.network.readresponse.failure> Session #{0} raised exception {1}", new object[]
						{
							this.m_session.id,
							Utilities.DescribeException(eXS)
						});
					}
				}
				catch (Exception eX)
				{
					bAbort = true;
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("ReadResponse() failure {0}\n{1}", new object[]
						{
							Utilities.DescribeException(eX),
							Utilities.ByteArrayToHexView(this.m_responseData.ToArray(), 32)
						});
					}
					if (eX is OperationCanceledException)
					{
						this.m_session.state = SessionStates.Aborted;
						FiddlerApplication.Log.LogFormat("fiddler.network.readresponse.failure> Session #{0} was aborted {1}", new object[]
						{
							this.m_session.id,
							Utilities.DescribeException(eX)
						});
					}
					else if (eX is OutOfMemoryException)
					{
						FiddlerApplication.Log.LogString(eX.ToString());
						this.m_session.state = SessionStates.Aborted;
						FiddlerApplication.Log.LogFormat("fiddler.network.readresponse.failure> Session #{0} Out of Memory", new object[] { this.m_session.id });
					}
					else
					{
						FiddlerApplication.Log.LogFormat("fiddler.network.readresponse.failure> Session #{0} raised exception {1}", new object[]
						{
							this.m_session.id,
							Utilities.DescribeException(eX)
						});
					}
				}
				IL_66E:;
			}
			while (!bGotFIN && !bAbort && (this.m_inHeaders == null || !this.isResponseBodyComplete()));
			this.m_session.Timers.ServerDoneResponse = DateTime.Now;
			if (this.m_session.isFlagSet(SessionFlags.ResponseStreamed))
			{
				this.m_session.Timers.ClientDoneResponse = this.m_session.Timers.ServerDoneResponse;
			}
			_arrReadFromPipe = null;
			oSW = null;
			this.m_session.Timers.ServerReads = oNTS;
			FiddlerApplication.DebugSpew("Finished reading server response: {0:N0} bytes.", new object[] { this.m_responseTotalDataCount });
			if (this.m_responseTotalDataCount == 0L && this.m_inHeaders == null)
			{
				bAbort = true;
			}
			if (bAbort)
			{
				if (this.m_bLeakedHeaders)
				{
					FiddlerApplication.DebugSpew("*** Aborted on a Response  #{0} which had partially streamed ****", new object[] { this.m_session.id });
				}
				FiddlerApplication.DebugSpew("*** Abort on Read from Server for Session #{0} ****", new object[] { this.m_session.id });
				return false;
			}
			if (this.m_inHeaders == null)
			{
				FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, true, true, "The Server did not return properly-formatted HTTP Headers. Maybe missing altogether (e.g. HTTP/0.9), maybe only \\r\\r instead of \\r\\n\\r\\n?\n");
				this.m_session.SetBitFlag(SessionFlags.ResponseStreamed, false);
				this.m_inHeaders = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
				this.m_inHeaders.HTTPVersion = "HTTP/1.0";
				this.m_inHeaders.SetStatus(200, "This buggy server did not return headers");
				this.m_iEntityBodyOffset = 0;
				return true;
			}
			if (bGotFIN)
			{
				FiddlerApplication.DebugSpew("Got FIN reading Response to #{0}.", new object[] { this.m_session.id });
				this._PoisonPipe();
				if (this.m_inHeaders.ExistsAndEquals("Transfer-Encoding", "chunked", false))
				{
					FiddlerApplication.DebugSpew("^ Previous FIN unexpected; chunked body ended abnormally for #{0}.", new object[] { this.m_session.id });
					FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInResponse, true, true, "Transfer-Encoding: Chunked response did not terminate with a proper zero-size chunk.");
				}
			}
			if (bDiscardResponseBodyBytes)
			{
				this.m_session["x-ResponseBodyTransferLength"] = this.m_responseTotalDataCount.ToString("N0");
			}
			return true;
		}

		/// <summary>
		/// When the headers first arrive, update bBufferResponse based on their contents.
		/// </summary>
		// Token: 0x060003C3 RID: 963 RVA: 0x00025794 File Offset: 0x00023994
		private void _EnableStreamingIfAppropriate()
		{
			string sContentType = this.m_inHeaders["Content-Type"];
			if (sContentType.OICStartsWithAny(new string[] { "text/event-stream", "multipart/x-mixed-replace" }) && FiddlerApplication.Prefs.GetBoolPref("fiddler.network.streaming.AutoStreamByMIME", true))
			{
				this.m_session.bBufferResponse = false;
			}
			else if (CONFIG.StreamAudioVideo && sContentType.OICStartsWithAny(new string[] { "video/", "audio/", "application/x-mms-framed" }))
			{
				this.m_session.bBufferResponse = false;
			}
			if (this.m_session.bBufferResponse)
			{
				return;
			}
			if (this.m_session.HTTPMethodIs("CONNECT"))
			{
				this.m_session.bBufferResponse = true;
				return;
			}
			if (101 == this.m_inHeaders.HTTPResponseCode)
			{
				this.m_session.bBufferResponse = true;
				return;
			}
			if (this.m_session.oRequest.pipeClient == null)
			{
				this.m_session.bBufferResponse = true;
				return;
			}
			if ((401 == this.m_inHeaders.HTTPResponseCode || 407 == this.m_inHeaders.HTTPResponseCode) && this.m_session.oFlags.ContainsKey("x-AutoAuth"))
			{
				this.m_session.bBufferResponse = true;
				return;
			}
		}

		/// <summary>
		/// Detects whether this is an direct FTP request and if so executes it and returns true.
		/// </summary>
		/// <returns>FALSE if the request wasn't FTP or wasn't direct.</returns>
		// Token: 0x060003C4 RID: 964 RVA: 0x000258DC File Offset: 0x00023ADC
		private bool IsWorkableFTPRequest()
		{
			if (this.m_session.isFTP && !this.m_session.isFlagSet(SessionFlags.SentToGateway))
			{
				try
				{
					FTPGateway.MakeFTPRequest(this.m_session, this.m_responseData, out this.m_inHeaders);
					return true;
				}
				catch (Exception eX)
				{
					this.m_session.oFlags["X-ServerPipeError"] = Utilities.DescribeException(eX);
					FiddlerApplication.Log.LogFormat("fiddler.network.readresponse.failure> FTPSession #{0} raised exception: {1}", new object[]
					{
						this.m_session.id,
						Utilities.DescribeException(eX)
					});
					return false;
				}
				return false;
			}
			return false;
		}

		/// <summary>
		/// Remove from memory the response data that we have already returned to the client.
		/// </summary>
		// Token: 0x060003C5 RID: 965 RVA: 0x00025988 File Offset: 0x00023B88
		private void ReleaseStreamedData()
		{
			this.m_responseData = new PipeReadBuffer(false);
			this.m_lngLeakedOffset = 0L;
			if (this.m_iEntityBodyOffset > 0)
			{
				this.m_responseTotalDataCount -= (long)this.m_iEntityBodyOffset;
				this.m_iEntityBodyOffset = 0;
			}
		}

		/// <summary>
		/// Remove from memory the response data that we have already returned to the client, up to the last chunk
		/// size indicator, which we need to keep around for chunk-integrity purposes.
		/// </summary>
		// Token: 0x060003C6 RID: 966 RVA: 0x000259C4 File Offset: 0x00023BC4
		private void ReleaseStreamedChunkedData()
		{
			if ((long)this.m_iEntityBodyOffset > this.m_lngLastChunkInfoOffset)
			{
				this.m_lngLastChunkInfoOffset = (long)this.m_iEntityBodyOffset;
			}
			long lngDontCare;
			Utilities.IsChunkedBodyComplete(this.m_session, this.m_responseData, this.m_lngLastChunkInfoOffset, out this.m_lngLastChunkInfoOffset, out lngDontCare);
			int iBytesLeakedAlreadyButSavedForChunkIntegrity = (int)(this.m_responseData.Length - this.m_lngLastChunkInfoOffset);
			PipeReadBuffer newMS = new PipeReadBuffer(iBytesLeakedAlreadyButSavedForChunkIntegrity);
			newMS.Write(this.m_responseData.GetBuffer(), (int)this.m_lngLastChunkInfoOffset, iBytesLeakedAlreadyButSavedForChunkIntegrity);
			this.m_responseData = newMS;
			this.m_lngLeakedOffset = (long)iBytesLeakedAlreadyButSavedForChunkIntegrity;
			this.m_lngLastChunkInfoOffset = 0L;
			this.m_iEntityBodyOffset = 0;
		}

		/// <summary>
		/// Leak the current bytes of the response to client. We wait for the full header
		/// set before starting to stream for a variety of impossible-to-change reasons.
		/// </summary>
		/// <returns>Returns TRUE if response bytes were leaked, false otherwise (e.g. write error). THROWS if "fiddler.network.streaming.abortifclientaborts" is TRUE</returns>
		// Token: 0x060003C7 RID: 967 RVA: 0x00025A60 File Offset: 0x00023C60
		private bool LeakResponseBytes()
		{
			bool result;
			try
			{
				if (this.m_session.oRequest.pipeClient == null)
				{
					result = false;
				}
				else
				{
					if (!this.m_bLeakedHeaders)
					{
						if ((401 == this.m_inHeaders.HTTPResponseCode && this.m_inHeaders["WWW-Authenticate"].OICStartsWith("N")) || (407 == this.m_inHeaders.HTTPResponseCode && this.m_inHeaders["Proxy-Authenticate"].OICStartsWith("N")))
						{
							this.m_inHeaders["Proxy-Support"] = "Session-Based-Authentication";
						}
						this.m_session.Timers.ClientBeginResponse = DateTime.Now;
						this.m_bLeakedHeaders = true;
						this.m_session.oRequest.pipeClient.Send(this.m_inHeaders.ToByteArray(true, true));
						this.m_lngLeakedOffset = (long)this.m_iEntityBodyOffset;
					}
					this.m_session.oRequest.pipeClient.Send(this.m_responseData.GetBuffer(), (int)this.m_lngLeakedOffset, (int)(this.m_responseData.Length - this.m_lngLeakedOffset));
					this.m_lngLeakedOffset = this.m_responseData.Length;
					result = true;
				}
			}
			catch (Exception eXInner)
			{
				this.m_session.PoisonClientPipe();
				FiddlerApplication.Log.LogFormat("fiddler.network.streaming> Streaming of response #{0} to client failed: {1}. Leaking aborted.", new object[]
				{
					this.m_session.id,
					eXInner.Message
				});
				if (FiddlerApplication.Prefs.GetBoolPref("fiddler.network.streaming.abortifclientaborts", false))
				{
					throw new OperationCanceledException("Leaking response to client failed", eXInner);
				}
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Mark this connection as non-reusable
		/// </summary>
		// Token: 0x060003C8 RID: 968 RVA: 0x00025C18 File Offset: 0x00023E18
		internal void _PoisonPipe()
		{
			if (this.pipeServer != null)
			{
				this.pipeServer.ReusePolicy = PipeReusePolicy.NoReuse;
			}
		}

		/// <summary>
		/// Size of buffer passed to pipe.Receive when reading from the server
		/// </summary>
		/// <remarks>
		/// PERF: Currently, I use [32768]; but I'd assume bigger buffers are faster. Does ReceiveBufferSize/SO_RCVBUF figure in here?
		/// Anecdotal data suggests that current reads rarely fill the full 32k buffer.
		/// </remarks>
		// Token: 0x040001B0 RID: 432
		internal static int s_cbServerReadBuffer = 32768;

		// Token: 0x040001B1 RID: 433
		internal static int s_SO_SNDBUF_Option = -1;

		// Token: 0x040001B2 RID: 434
		internal static int s_SO_RCVBUF_Option = -1;

		/// <summary>
		/// Interval, in milliseconds, after which Fiddler will check to see whether a response should continue to be read. Otherwise,
		/// a never-ending network stream can accumulate ever larger amounts of data that will never be seen by the garbage collector.
		/// </summary>
		// Token: 0x040001B3 RID: 435
		internal static int s_WATCHDOG_INTERVAL = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.serverpipe.watchdoginterval", (int)new TimeSpan(0, 5, 0).TotalMilliseconds);

		/// <summary>
		/// The pipeServer represents Fiddler's connection to the server.
		/// </summary>
		// Token: 0x040001B4 RID: 436
		public ServerPipe pipeServer;

		/// <summary>
		/// The session to which this ServerChatter belongs
		/// </summary>
		// Token: 0x040001B5 RID: 437
		private Session m_session;

		/// <summary>
		/// The inbound headers on this response
		/// </summary>
		// Token: 0x040001B6 RID: 438
		private HTTPResponseHeaders m_inHeaders;

		/// <summary>
		/// Indicates whether this request was sent to a (non-SOCKS) Gateway, which influences whether the protocol and host are
		/// mentioned in the Request line
		/// When True, the session should have SessionFlags.SentToGateway set.
		/// </summary>
		// Token: 0x040001B7 RID: 439
		internal bool m_bWasForwarded;

		/// <summary>
		/// Buffer holds this response's data as it is read from the pipe.
		/// </summary>
		// Token: 0x040001B8 RID: 440
		private PipeReadBuffer m_responseData;

		/// <summary>
		/// The total count of bytes read for this response. Typically equals m_responseData.Length unless 
		/// Streaming &amp; Log-Drop-Response-Body - in which case it will be larger since the m_responseData is cleared after every read.
		///
		/// BUG BUG: This value is reset to 0 when clearing streamed data. It probably shouldn't be; the problem is that this field is getting used for two purposes
		/// </summary>
		// Token: 0x040001B9 RID: 441
		internal long m_responseTotalDataCount;

		/// <summary>
		/// Pointer to first byte of Entity body (or to the start of the next set of headers in the case where there's a HTTP/1xx intermediate header)
		/// Note: This gets reset to 0 if we're streaming and dropping the response body.
		/// </summary>
		// Token: 0x040001BA RID: 442
		private int m_iEntityBodyOffset;

		/// <summary>
		/// Optimization: tracks how far we've looked into the Request when determining iEntityBodyOffset
		/// </summary>
		// Token: 0x040001BB RID: 443
		private int m_iBodySeekProgress;

		/// <summary>
		/// True if final (non-1xx) HTTP Response headers have been returned to the client.
		/// </summary>
		// Token: 0x040001BC RID: 444
		private bool m_bLeakedHeaders;

		/// <summary>
		/// Indicates how much of _responseData buffer has already been streamed to the client
		/// </summary>
		// Token: 0x040001BD RID: 445
		private long m_lngLeakedOffset;

		/// <summary>
		/// Position in responseData of the start of the latest parsed chunk size information
		/// </summary>
		// Token: 0x040001BE RID: 446
		private long m_lngLastChunkInfoOffset = -1L;

		/// <summary>
		/// Locals used by the Connect-to-Host state machine
		/// </summary>
		// Token: 0x040001BF RID: 447
		private ServerChatter.MakeConnectionExecutionState _esState;

		// Token: 0x040001C0 RID: 448
		private readonly object _esStateLock = new object();

		/// <summary>
		/// The ExecutionState object holds information that is used by the Connect-to-Host state machine
		/// </summary>
		// Token: 0x020000D6 RID: 214
		internal class MakeConnectionExecutionState
		{
			// Token: 0x04000378 RID: 888
			internal ServerChatter.StateConnecting CurrentState;

			// Token: 0x04000379 RID: 889
			internal AsyncCallback OnDone;

			// Token: 0x0400037A RID: 890
			internal string sPoolKeyContext;

			// Token: 0x0400037B RID: 891
			internal string sTarget;

			// Token: 0x0400037C RID: 892
			internal bool bUseSOCKSGateway;

			// Token: 0x0400037D RID: 893
			internal IPEndPoint[] ipepGateways;

			// Token: 0x0400037E RID: 894
			internal IPEndPoint[] arrIPEPDest;

			// Token: 0x0400037F RID: 895
			internal string sServerHostname;

			// Token: 0x04000380 RID: 896
			internal string sSuitableConnectionID;

			// Token: 0x04000381 RID: 897
			internal Socket newSocket;

			// Token: 0x04000382 RID: 898
			internal Exception lastException;

			// Token: 0x04000383 RID: 899
			internal int iServerPort = -1;
		}

		// Token: 0x020000D7 RID: 215
		internal enum StateConnecting : byte
		{
			// Token: 0x04000385 RID: 901
			BeginFindGateway,
			// Token: 0x04000386 RID: 902
			EndFindGateway,
			// Token: 0x04000387 RID: 903
			BeginGenerateIPEndPoint,
			// Token: 0x04000388 RID: 904
			EndGenerateIPEndPoint,
			// Token: 0x04000389 RID: 905
			BeginConnectSocket,
			// Token: 0x0400038A RID: 906
			EndConnectSocket,
			// Token: 0x0400038B RID: 907
			Established,
			// Token: 0x0400038C RID: 908
			Failed
		}
	}
}
