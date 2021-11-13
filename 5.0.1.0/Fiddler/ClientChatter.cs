using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using FiddlerCore.Utilities;

namespace Fiddler
{
	/// <summary>
	/// The ClientChatter object, exposed as the oRequest object on the Session object, represents a single web request.
	/// </summary>
	// Token: 0x02000019 RID: 25
	public class ClientChatter
	{
		/// <summary>
		/// Returns the port on which Fiddler read the request (typically 8888)
		/// </summary>
		// Token: 0x1700002E RID: 46
		// (get) Token: 0x06000107 RID: 263 RVA: 0x0000F510 File Offset: 0x0000D710
		[CodeDescription("Returns the port on which Fiddler read the request (typically 8888). Only available while the request is alive.")]
		public int InboundPort
		{
			get
			{
				try
				{
					if (this.pipeClient != null)
					{
						return this.pipeClient.LocalPort;
					}
				}
				catch
				{
				}
				return 0;
			}
		}

		/// <summary>
		/// Count of body bytes read from the client. If no body bytes have yet been read, returns count of header bytes.
		/// </summary>
		// Token: 0x1700002F RID: 47
		// (get) Token: 0x06000108 RID: 264 RVA: 0x0000F54C File Offset: 0x0000D74C
		internal long _PeekUploadProgress
		{
			get
			{
				ClientChatter.RequestReaderState _rrs = this.stateRead;
				if (_rrs == null)
				{
					return -1L;
				}
				return _rrs.GetBodyBytesRead();
			}
		}

		/// <summary>
		/// HTTP Headers sent in the client request, or null.
		/// </summary>
		// Token: 0x17000030 RID: 48
		// (get) Token: 0x06000109 RID: 265 RVA: 0x0000F56C File Offset: 0x0000D76C
		// (set) Token: 0x0600010A RID: 266 RVA: 0x0000F574 File Offset: 0x0000D774
		public HTTPRequestHeaders headers
		{
			get
			{
				return this.m_headers;
			}
			set
			{
				this.m_headers = value;
			}
		}

		/// <summary>
		/// Was this request received from a reused client connection? Looks at SessionFlags.ClientPipeReused flag on owning Session.
		/// </summary>
		// Token: 0x17000031 RID: 49
		// (get) Token: 0x0600010B RID: 267 RVA: 0x0000F57D File Offset: 0x0000D77D
		public bool bClientSocketReused
		{
			get
			{
				return this.m_session.isFlagSet(SessionFlags.ClientPipeReused);
			}
		}

		/// <summary>
		/// Note: This returns the request's HOST header, which may include a trailing port #.
		/// If the Host is an IPv6 literal, it will be enclosed in brackets '[' and ']'
		/// </summary>
		// Token: 0x17000032 RID: 50
		// (get) Token: 0x0600010C RID: 268 RVA: 0x0000F58B File Offset: 0x0000D78B
		// (set) Token: 0x0600010D RID: 269 RVA: 0x0000F5AC File Offset: 0x0000D7AC
		public string host
		{
			get
			{
				if (this.m_headers != null)
				{
					return this.m_headers["Host"];
				}
				return string.Empty;
			}
			internal set
			{
				if (this.m_headers == null)
				{
					return;
				}
				if (value == null)
				{
					value = string.Empty;
				}
				if (value.EndsWith(":80") && "HTTP".OICEquals(this.m_headers.UriScheme))
				{
					value = value.Substring(0, value.Length - 3);
				}
				this.m_headers["Host"] = value;
				if ("CONNECT".OICEquals(this.m_headers.HTTPMethod))
				{
					this.m_headers.RequestPath = value;
				}
			}
		}

		/// <summary>
		/// Controls whether the request body is streamed to the server as it is read from the client.
		/// </summary>
		// Token: 0x17000033 RID: 51
		// (get) Token: 0x0600010E RID: 270 RVA: 0x0000F635 File Offset: 0x0000D835
		// (set) Token: 0x0600010F RID: 271 RVA: 0x0000F644 File Offset: 0x0000D844
		[CodeDescription("Controls whether the request body is streamed to the server as it is read from the client.")]
		public bool BufferRequest
		{
			get
			{
				return this.m_session.isFlagSet(SessionFlags.RequestStreamed);
			}
			set
			{
				if (this.m_session.state > SessionStates.ReadingRequest)
				{
					throw new InvalidOperationException("Too late. BufferRequest may only be set before or while ReadingRequest.");
				}
				this.m_session.SetBitFlag(SessionFlags.RequestStreamed, !value);
			}
		}

		// Token: 0x06000110 RID: 272 RVA: 0x0000F670 File Offset: 0x0000D870
		internal ClientChatter(Session oSession)
		{
			this.m_session = oSession;
		}

		/// <summary>
		/// Create a ClientChatter object initialized with a set of HTTP headers
		/// Called primarily when loading session data from a file.
		/// </summary>
		/// <param name="oSession">The Session object which will own this request</param>
		/// <param name="sData">The string containing the request data</param>
		// Token: 0x06000111 RID: 273 RVA: 0x0000F680 File Offset: 0x0000D880
		internal ClientChatter(Session oSession, string sData)
		{
			this.m_session = oSession;
			this.headers = Parser.ParseRequest(sData);
			if (this.headers != null)
			{
				if ("CONNECT" == this.m_headers.HTTPMethod)
				{
					this.m_session.isTunnel = true;
					return;
				}
			}
			else
			{
				this.headers = new HTTPRequestHeaders("/MALFORMED", new string[] { "Fiddler: Malformed header string" });
			}
		}

		/// <summary>
		/// Loads a HTTP request body from a file rather than a memory stream.
		/// </summary>
		/// <param name="sFilename">The file to load</param>
		/// <returns>TRUE if the file existed. THROWS on most errors other than File-Not-Found</returns>
		// Token: 0x06000112 RID: 274 RVA: 0x0000F6F0 File Offset: 0x0000D8F0
		internal bool ReadRequestBodyFromFile(string sFilename)
		{
			if (!File.Exists(sFilename))
			{
				this.m_session.utilSetRequestBody("File not found: " + sFilename);
				return false;
			}
			this.m_session.RequestBody = File.ReadAllBytes(sFilename);
			return true;
		}

		/// <summary>
		/// Based on this session's data, determine the expected Transfer-Size of the request body. See RFC2616 Section 4.4 Message Length.
		/// Note, there's currently no support for "multipart/byteranges" requests anywhere in Fiddler.
		/// </summary>
		/// <returns>Expected Transfer-Size of the body, in bytes.</returns>
		// Token: 0x06000113 RID: 275 RVA: 0x0000F724 File Offset: 0x0000D924
		private long _calculateExpectedEntityTransferSize()
		{
			if (this.m_headers == null)
			{
				throw new InvalidDataException("HTTP Request did not contain headers");
			}
			long cbExpected = 0L;
			if (this.m_headers.ExistsAndEquals("Transfer-Encoding", "chunked", false))
			{
				if (this.m_session.isAnyFlagSet(SessionFlags.RequestStreamed | SessionFlags.IsRPCTunnel))
				{
					return this.stateRead.m_requestData.Length - (long)this.stateRead.iEntityBodyOffset;
				}
				ClientChatter.RequestReaderState _rrs = this.stateRead;
				if ((long)_rrs.iEntityBodyOffset >= _rrs.m_requestData.Length)
				{
					throw new InvalidDataException("Bad request: Chunked Body was missing entirely.");
				}
				long lngLastChunkInfoOffset;
				long lngEndOfEntity;
				if (!Utilities.IsChunkedBodyComplete(this.m_session, _rrs.m_requestData, (long)_rrs.iEntityBodyOffset, out lngLastChunkInfoOffset, out lngEndOfEntity))
				{
					throw new InvalidDataException("Bad request: Chunked Body was incomplete.");
				}
				if (lngEndOfEntity < (long)_rrs.iEntityBodyOffset)
				{
					throw new InvalidDataException("Bad request: Chunked Body was malformed. Entity ends before it starts!");
				}
				return lngEndOfEntity - (long)_rrs.iEntityBodyOffset;
			}
			else
			{
				if (!long.TryParse(this.m_headers["Content-Length"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out cbExpected) || cbExpected < 0L)
				{
					return 0L;
				}
				return cbExpected;
			}
		}

		/// <summary>
		/// Free Request data. Called by TakeEntity or by ReadRequest method on request failure
		/// </summary>
		// Token: 0x06000114 RID: 276 RVA: 0x0000F828 File Offset: 0x0000DA28
		private void _freeRequestData()
		{
			ClientChatter.RequestReaderState _rrs = this.stateRead;
			this.stateRead = null;
			if (_rrs != null)
			{
				_rrs.Dispose();
			}
		}

		/// <summary>
		/// Extract byte array representing the entity, put any excess bytes back in the pipe, free the RequestReadState, and 
		/// return the entity.
		/// </summary>
		/// <returns>Byte array containing the entity body</returns>
		// Token: 0x06000115 RID: 277 RVA: 0x0000F84C File Offset: 0x0000DA4C
		internal byte[] TakeEntity()
		{
			if (this.stateRead == null)
			{
				return Utilities.emptyByteArray;
			}
			if (this.stateRead.m_requestData.Length < 1L)
			{
				this._freeRequestData();
				return Utilities.emptyByteArray;
			}
			long cbAvailableEntityData = this.stateRead.m_requestData.Length - (long)this.stateRead.iEntityBodyOffset;
			long cbExpectedEntitySize = this._calculateExpectedEntityTransferSize();
			if (cbAvailableEntityData != cbExpectedEntitySize)
			{
				if (cbAvailableEntityData > cbExpectedEntitySize)
				{
					try
					{
						byte[] arrExcess = new byte[cbAvailableEntityData - cbExpectedEntitySize];
						FiddlerApplication.Log.LogFormat("HTTP Pipelining Client detected; {0:N0} bytes of excess data on client socket for Session #{1}.", new object[]
						{
							arrExcess.Length,
							this.m_session.id
						});
						Buffer.BlockCopy(this.stateRead.m_requestData.GetBuffer(), this.stateRead.iEntityBodyOffset + (int)cbExpectedEntitySize, arrExcess, 0, arrExcess.Length);
						this.pipeClient.putBackSomeBytes(arrExcess);
					}
					catch (OutOfMemoryException oOOM)
					{
						this.m_session.PoisonClientPipe();
						FiddlerApplication.Log.LogFormat("HTTP Request Pipelined data too large to store. Abandoning it" + Utilities.DescribeException(oOOM), Array.Empty<object>());
					}
					cbAvailableEntityData = cbExpectedEntitySize;
				}
				else if (!this.m_session.isAnyFlagSet(SessionFlags.RequestStreamed | SessionFlags.IsRPCTunnel))
				{
					FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, true, true, string.Format("Content-Length mismatch: Request Header indicated {0:N0} bytes, but client sent {1:N0} bytes.", cbExpectedEntitySize, cbAvailableEntityData));
					if (!this.m_session.isAnyFlagSet(SessionFlags.RequestGeneratedByFiddler))
					{
						if (FiddlerApplication.Prefs.GetBoolPref("fiddler.network.RejectIncompleteRequests", true))
						{
							this.FailSession(408, "Request body incomplete", string.Format("The request body did not contain the specified number of bytes. Got {0:N0}, expected {1:N0}", cbAvailableEntityData, cbExpectedEntitySize));
							throw new InvalidDataException(string.Format("The request body did not contain the specified number of bytes. Got {0:N0}, expected {1:N0}", cbAvailableEntityData, cbExpectedEntitySize));
						}
						if (FiddlerApplication.Prefs.GetBoolPref("fiddler.network.FixRequestContentLength", true))
						{
							this.m_headers.RenameHeaderItems("Content-Length", "Original-Content-Length");
							this.m_headers["Content-Length"] = cbAvailableEntityData.ToString();
						}
					}
				}
			}
			byte[] arrResult;
			try
			{
				arrResult = new byte[cbAvailableEntityData];
				Buffer.BlockCopy(this.stateRead.m_requestData.GetBuffer(), this.stateRead.iEntityBodyOffset, arrResult, 0, arrResult.Length);
			}
			catch (OutOfMemoryException oOOM2)
			{
				string title = "HTTP Request Too Large";
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[]
				{
					title,
					oOOM2.ToString()
				});
				arrResult = Encoding.ASCII.GetBytes("Fiddler: Out of memory");
				this.m_session.PoisonClientPipe();
			}
			this._freeRequestData();
			return arrResult;
		}

		/// <summary>
		/// Simple indexer into the Request Headers object
		/// </summary>
		// Token: 0x17000034 RID: 52
		public string this[string sHeader]
		{
			get
			{
				if (this.m_headers == null)
				{
					return string.Empty;
				}
				return this.m_headers[sHeader];
			}
			set
			{
				if (this.m_headers == null)
				{
					throw new InvalidDataException("Request Headers object does not exist");
				}
				this.m_headers[sHeader] = value;
			}
		}

		/// <summary>
		/// Send a HTTP/XXX Error Message to the Client, calling FiddlerApplication.BeforeReturningError and DoReturningError in FiddlerScript.
		/// Note: This method does not poison the Server pipe, so if poisoning is desired, it's the caller's responsibility to do that.
		/// Note: Because this method uses Connection: close on the returned response, it has the effect of poisoning the client pipe
		/// </summary>
		/// <param name="iError">Response code</param>
		/// <param name="sErrorStatusText">Response status text</param>
		/// <param name="sErrorBody">Body of the HTTP Response</param>
		// Token: 0x06000118 RID: 280 RVA: 0x0000FB28 File Offset: 0x0000DD28
		public void FailSession(int iError, string sErrorStatusText, string sErrorBody)
		{
			this.m_session.EnsureID();
			this.m_session.oFlags["X-FailSession-When"] = this.m_session.state.ToString();
			this.BuildAndReturnResponse(iError, sErrorStatusText, sErrorBody, null);
		}

		/// <summary>
		/// Return a HTTP response and signal that the client should close the connection
		/// </summary>
		/// <param name="delLastChance">A Delegate that fires to give one final chance to modify the Session before
		/// calling the DoBeforeReturningError and returning the response</param>
		// Token: 0x06000119 RID: 281 RVA: 0x0000FB78 File Offset: 0x0000DD78
		internal void BuildAndReturnResponse(int iStatus, string sStatusText, string sBodyText, Action<Session> delLastChance)
		{
			this.m_session.SetBitFlag(SessionFlags.ResponseGeneratedByFiddler, true);
			if (iStatus >= 400 && sBodyText.Length < 512)
			{
				sBodyText = sBodyText.PadRight(512, ' ');
			}
			this.m_session.responseBodyBytes = Encoding.UTF8.GetBytes(sBodyText);
			this.m_session.oResponse.headers = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
			this.m_session.oResponse.headers.SetStatus(iStatus, sStatusText);
			this.m_session.oResponse.headers.Add("Date", DateTime.UtcNow.ToString("r"));
			this.m_session.oResponse.headers.Add("Content-Type", "text/html; charset=UTF-8");
			this.m_session.oResponse.headers.Add("Connection", "close");
			this.m_session.oResponse.headers.Add("Cache-Control", "no-cache, must-revalidate");
			this.m_session.oResponse.headers.Add("Timestamp", DateTime.Now.ToString("HH:mm:ss.fff"));
			this.m_session.state = SessionStates.Aborted;
			if (delLastChance != null)
			{
				delLastChance(this.m_session);
			}
			FiddlerApplication.DoBeforeReturningError(this.m_session);
			this.m_session.ReturnResponse(false);
		}

		/// <summary>
		/// Parse the headers from the requestData buffer.  
		/// Precondition: Call AFTER having set the correct iEntityBodyOffset.
		///
		/// Note: This code used to be a lot simpler before, when it used strings instead of byte[]s. Sadly,
		/// we've gotta use byte[]s to ensure nothing in the URI gets lost.
		/// </summary>
		/// <returns>TRUE if successful.</returns>
		// Token: 0x0600011A RID: 282 RVA: 0x0000FCF4 File Offset: 0x0000DEF4
		private bool _ParseRequestForHeaders()
		{
			if (this.stateRead.m_requestData == null || this.stateRead.iEntityBodyOffset < 4)
			{
				return false;
			}
			this.m_headers = new HTTPRequestHeaders(CONFIG.oHeaderEncoding);
			byte[] arrRequest = this.stateRead.m_requestData.GetBuffer();
			int ixURIOffset;
			int iURILen;
			int ixHeaderNVPOffset;
			string sOtherErrors;
			Parser.CrackRequestLine(arrRequest, out ixURIOffset, out iURILen, out ixHeaderNVPOffset, out sOtherErrors);
			if (ixURIOffset < 1 || iURILen < 1)
			{
				FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, true, false, "Incorrectly formed Request-Line");
				FiddlerApplication.Log.LogFormat("!CrackRequestLine couldn't find URI.\n{0}\n", new object[] { Utilities.ByteArrayToHexView(arrRequest, 16, 256, true) });
				return false;
			}
			if (!string.IsNullOrEmpty(sOtherErrors))
			{
				FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, true, false, sOtherErrors);
				FiddlerApplication.Log.LogFormat("!CrackRequestLine returned '{0}'.\n{1}\n", new object[]
				{
					sOtherErrors,
					Utilities.ByteArrayToHexView(arrRequest, 16, 256, true)
				});
			}
			string sMethod = Encoding.ASCII.GetString(arrRequest, 0, ixURIOffset - 1);
			this.m_headers.HTTPMethod = sMethod.ToUpperInvariant();
			if (sMethod != this.m_headers.HTTPMethod)
			{
				FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, false, false, string.Format("Per RFC2616, HTTP Methods are case-sensitive. Client sent '{0}', expected '{1}'.", sMethod, this.m_headers.HTTPMethod));
			}
			this.m_headers.HTTPVersion = Encoding.ASCII.GetString(arrRequest, ixURIOffset + iURILen + 1, ixHeaderNVPOffset - iURILen - ixURIOffset - 2).Trim().ToUpperInvariant();
			int ixSlashPrecedingHost = 0;
			if (arrRequest[ixURIOffset] != 47)
			{
				if (iURILen > 7 && arrRequest[ixURIOffset + 4] == 58 && arrRequest[ixURIOffset + 5] == 47 && arrRequest[ixURIOffset + 6] == 47)
				{
					this.m_headers.UriScheme = Encoding.ASCII.GetString(arrRequest, ixURIOffset, 4);
					ixSlashPrecedingHost = ixURIOffset + 6;
					ixURIOffset += 7;
					iURILen -= 7;
				}
				else if (iURILen > 8 && arrRequest[ixURIOffset + 5] == 58 && arrRequest[ixURIOffset + 6] == 47 && arrRequest[ixURIOffset + 7] == 47)
				{
					this.m_headers.UriScheme = Encoding.ASCII.GetString(arrRequest, ixURIOffset, 5);
					ixSlashPrecedingHost = ixURIOffset + 7;
					ixURIOffset += 8;
					iURILen -= 8;
				}
				else if (iURILen > 6 && arrRequest[ixURIOffset + 3] == 58 && arrRequest[ixURIOffset + 4] == 47 && arrRequest[ixURIOffset + 5] == 47)
				{
					this.m_headers.UriScheme = Encoding.ASCII.GetString(arrRequest, ixURIOffset, 3);
					ixSlashPrecedingHost = ixURIOffset + 5;
					ixURIOffset += 6;
					iURILen -= 6;
				}
			}
			if (ixSlashPrecedingHost == 0)
			{
				if (this.pipeClient != null && this.pipeClient.bIsSecured)
				{
					this.m_headers.UriScheme = "https";
				}
				else
				{
					this.m_headers.UriScheme = "http";
				}
			}
			if (ixSlashPrecedingHost > 0)
			{
				if (iURILen == 0)
				{
					FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, true, false, "Incorrectly formed Request-Line. Request-URI component was missing.\r\n\r\n" + Encoding.ASCII.GetString(arrRequest, 0, ixHeaderNVPOffset));
					return false;
				}
				while (iURILen > 0 && arrRequest[ixURIOffset] != 47 && arrRequest[ixURIOffset] != 63)
				{
					ixURIOffset++;
					iURILen--;
				}
				int ixStartHost = ixSlashPrecedingHost + 1;
				int iHostLen = ixURIOffset - ixStartHost;
				if (iHostLen > 0)
				{
					this.stateRead.m_sHostFromURI = CONFIG.oHeaderEncoding.GetString(arrRequest, ixStartHost, iHostLen);
					if (this.m_headers.UriScheme == "ftp" && this.stateRead.m_sHostFromURI.Contains("@"))
					{
						int ixHostOffset = this.stateRead.m_sHostFromURI.LastIndexOf("@") + 1;
						this.m_headers.UriUserInfo = this.stateRead.m_sHostFromURI.Substring(0, ixHostOffset);
						this.stateRead.m_sHostFromURI = this.stateRead.m_sHostFromURI.Substring(ixHostOffset);
					}
				}
			}
			byte[] rawURI = new byte[iURILen];
			Buffer.BlockCopy(arrRequest, ixURIOffset, rawURI, 0, iURILen);
			this.m_headers.RawPath = rawURI;
			if (string.IsNullOrEmpty(this.m_headers.RequestPath))
			{
				FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, false, false, "Incorrectly formed Request-Line. abs_path was empty (e.g. missing /). RFC2616 Section 5.1.2");
			}
			string sHeaders = CONFIG.oHeaderEncoding.GetString(arrRequest, ixHeaderNVPOffset, this.stateRead.iEntityBodyOffset - ixHeaderNVPOffset).Trim();
			if (sHeaders.Length >= 1)
			{
				string[] arrLines = sHeaders.Replace("\r\n", "\n").Split('\n', StringSplitOptions.None);
				string sErrs = string.Empty;
				if (!Parser.ParseNVPHeaders(this.m_headers, arrLines, 0, ref sErrs))
				{
					FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, true, false, "Incorrectly formed request headers.\n" + sErrs);
				}
			}
			if (this.m_headers.Exists("Content-Length") && this.m_headers.ExistsAndContains("Transfer-Encoding", "chunked"))
			{
				FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, false, false, "Content-Length request header MUST NOT be present when Transfer-Encoding is used (RFC2616 Section 4.4)");
			}
			return true;
		}

		/// <summary>
		/// This function decides if the request string represents a complete HTTP request
		/// </summary>
		/// <returns></returns>
		// Token: 0x0600011B RID: 283 RVA: 0x0001018C File Offset: 0x0000E38C
		private bool _isRequestComplete()
		{
			if (this.m_headers == null)
			{
				if (!this.stateRead._areHeadersAvailable(this.m_session))
				{
					if (this.stateRead.m_requestData.Length > (long)ClientPipe._cbLimitRequestHeaders)
					{
						this.m_headers = new HTTPRequestHeaders();
						this.m_headers.HTTPMethod = "BAD";
						this.m_headers["Host"] = "BAD-REQUEST";
						this.m_headers.RequestPath = "/REQUEST_TOO_LONG";
						this.FailSession(414, "Fiddler - Request Too Long", "[Fiddler] Request Header parsing failed. Headers not found in the first " + this.stateRead.m_requestData.Length.ToString() + " bytes.");
						return true;
					}
					return false;
				}
				else
				{
					if (!this._ParseRequestForHeaders())
					{
						string sDetailedError;
						if (this.stateRead.m_requestData != null)
						{
							sDetailedError = Utilities.ByteArrayToHexView(this.stateRead.m_requestData.GetBuffer(), 24, (int)Math.Min(this.stateRead.m_requestData.Length, 2048L));
						}
						else
						{
							sDetailedError = "{Fiddler:no data}";
						}
						if (this.m_headers == null)
						{
							this.m_headers = new HTTPRequestHeaders();
							this.m_headers.HTTPMethod = "BAD";
							this.m_headers["Host"] = "BAD-REQUEST";
							this.m_headers.RequestPath = "/BAD_REQUEST";
						}
						this.FailSession(400, "Fiddler - Bad Request", "[Fiddler] Request Header parsing failed. Request was:\n" + sDetailedError);
						return true;
					}
					this.m_session.Timers.FiddlerGotRequestHeaders = DateTime.Now;
					this.m_session._AssignID();
					FiddlerApplication.DoRequestHeadersAvailable(this.m_session);
					if (this.m_session.isFlagSet(SessionFlags.RequestStreamed))
					{
						if (!("CONNECT" == this.m_headers.HTTPMethod) && !this.m_headers.ExistsAndEquals("Content-Length", "0", false) && (this.m_headers.Exists("Content-Length") || this.m_headers.Exists("Transfer-Encoding")))
						{
							return true;
						}
						this.m_session.SetBitFlag(SessionFlags.RequestStreamed, false);
					}
					if (Utilities.isRPCOverHTTPSMethod(this.m_headers.HTTPMethod) && !this.m_headers.ExistsAndEquals("Content-Length", "0", false))
					{
						this.m_session.SetBitFlag(SessionFlags.IsRPCTunnel, true);
						return true;
					}
					if (this.m_headers.ExistsAndEquals("Transfer-Encoding", "chunked", false))
					{
						this.stateRead.bIsChunkedBody = true;
					}
					else if (this.m_headers.Exists("Content-Length"))
					{
						long iHeaderCL = 0L;
						if (!long.TryParse(this.m_headers["Content-Length"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out iHeaderCL) || iHeaderCL < 0L)
						{
							FiddlerApplication.HandleHTTPError(this.m_session, SessionFlags.ProtocolViolationInRequest, true, true, "Request content length was invalid.\nContent-Length: " + this.m_headers["Content-Length"]);
							this.FailSession(400, "Fiddler - Bad Request", "[Fiddler] Request Content-Length header parsing failed.\nContent-Length: " + this.m_headers["Content-Length"]);
							return true;
						}
						this.stateRead.iContentLength = iHeaderCL;
						if (iHeaderCL > 0L)
						{
							this.stateRead.m_requestData.HintTotalSize((uint)(iHeaderCL + (long)this.stateRead.iEntityBodyOffset));
						}
					}
				}
			}
			if (this.stateRead.bIsChunkedBody)
			{
				if (this.stateRead.m_lngLastChunkInfoOffset < (long)this.stateRead.iEntityBodyOffset)
				{
					this.stateRead.m_lngLastChunkInfoOffset = (long)this.stateRead.iEntityBodyOffset;
				}
				long lngDontCare;
				return Utilities.IsChunkedBodyComplete(this.m_session, this.stateRead.m_requestData, this.stateRead.m_lngLastChunkInfoOffset, out this.stateRead.m_lngLastChunkInfoOffset, out lngDontCare);
			}
			return this.stateRead.m_requestData.Length >= (long)this.stateRead.iEntityBodyOffset + this.stateRead.iContentLength;
		}

		/// <summary>
		/// Read a (usually complete) request from pipeClient. If RequestStreamed flag is set, only the headers have been read.
		/// </summary>
		/// <returns>TRUE, if a request could be read. FALSE, otherwise.</returns>
		// Token: 0x0600011C RID: 284 RVA: 0x00010568 File Offset: 0x0000E768
		internal bool ReadRequest()
		{
			if (this.stateRead != null)
			{
				string exceptionMessage = "ReadRequest called when requestData buffer already existed.";
				FiddlerApplication.Log.LogString(exceptionMessage);
				return false;
			}
			if (this.pipeClient == null)
			{
				string exceptionMessage2 = "ReadRequest called after pipeClient was null'd.";
				FiddlerApplication.Log.LogString(exceptionMessage2);
				return false;
			}
			this.stateRead = new ClientChatter.RequestReaderState();
			this.m_session.SetBitFlag(SessionFlags.ClientPipeReused, this.pipeClient.iUseCount > 0U);
			this.pipeClient.IncrementUse(0);
			this.pipeClient.setReceiveTimeout(true);
			bool bAbort = false;
			bool bDone = false;
			byte[] _arrReadFromPipe = new byte[ClientChatter.s_cbClientReadBuffer];
			int cbLastReceive = 0;
			SessionTimers.NetTimestamps oNTS = new SessionTimers.NetTimestamps();
			Stopwatch oSW = Stopwatch.StartNew();
			for (;;)
			{
				try
				{
					cbLastReceive = this.pipeClient.Receive(_arrReadFromPipe);
					oNTS.AddRead(oSW.ElapsedMilliseconds, cbLastReceive);
				}
				catch (SocketException eeX)
				{
					bAbort = true;
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("ReadRequest {0} threw #{1} - {2}", new object[]
						{
							(this.pipeClient == null) ? "Null pipeClient" : this.pipeClient.ToString(),
							eeX.ErrorCode,
							eeX.Message
						});
					}
					if (eeX.SocketErrorCode == SocketError.TimedOut)
					{
						this.m_session.oFlags["X-ClientPipeError"] = string.Format("ReadRequest timed out; total of {0:N0} bytes read from client.", this.stateRead.m_requestData.Length);
						this.FailSession(408, "Request Timed Out", string.Format("The client failed to send a complete request on this {0} connection before the timeout period elapsed; {1} bytes were read from client.", (this.pipeClient.iUseCount < 2U || (this.pipeClient.bIsSecured && this.pipeClient.iUseCount < 3U)) ? "NEW" : "REUSED", this.stateRead.m_requestData.Length));
					}
					goto IL_387;
				}
				catch (Exception ex)
				{
					bAbort = true;
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("ReadRequest {0} threw {1}", new object[]
						{
							(this.pipeClient == null) ? "Null pipeClient" : this.pipeClient.ToString(),
							ex.Message
						});
					}
					goto IL_387;
				}
				goto IL_1FE;
				IL_387:
				if (bDone || bAbort || this._isRequestComplete())
				{
					goto IL_398;
				}
				continue;
				IL_1FE:
				if (cbLastReceive < 1)
				{
					bDone = true;
					FiddlerApplication.DoReadRequestBuffer(this.m_session, _arrReadFromPipe, 0);
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("ReadRequest {0} returned {1}", new object[]
						{
							(this.pipeClient == null) ? "Null pipeClient" : this.pipeClient.ToString(),
							cbLastReceive
						});
						goto IL_387;
					}
					goto IL_387;
				}
				else
				{
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("READ {0} FROM {1}:\n{2}", new object[]
						{
							cbLastReceive,
							this.pipeClient,
							Utilities.ByteArrayToHexView(_arrReadFromPipe, 32, cbLastReceive)
						});
					}
					if (!FiddlerApplication.DoReadRequestBuffer(this.m_session, _arrReadFromPipe, cbLastReceive))
					{
						break;
					}
					if (this.stateRead.m_requestData.Length != 0L)
					{
						this.stateRead.m_requestData.Write(_arrReadFromPipe, 0, cbLastReceive);
						goto IL_387;
					}
					this.m_session.Timers.ClientBeginRequest = DateTime.Now;
					if (1U == this.pipeClient.iUseCount && cbLastReceive > 2 && (_arrReadFromPipe[0] == 4 || _arrReadFromPipe[0] == 5))
					{
						goto IL_2EF;
					}
					int iMsgStart = 0;
					while (iMsgStart < cbLastReceive && (13 == _arrReadFromPipe[iMsgStart] || 10 == _arrReadFromPipe[iMsgStart]))
					{
						iMsgStart++;
					}
					this.stateRead.m_requestData.Write(_arrReadFromPipe, iMsgStart, cbLastReceive - iMsgStart);
					this.pipeClient.setReceiveTimeout(false);
					goto IL_387;
				}
			}
			FiddlerApplication.DebugSpew("ReadRequest() aborted by OnReadRequestBuffer");
			return false;
			IL_2EF:
			FiddlerApplication.Log.LogFormat("It looks like '{0}' is trying to send SOCKS traffic to us.\r\n{1}", new object[]
			{
				this.m_session["X-ProcessInfo"],
				Utilities.ByteArrayToHexView(_arrReadFromPipe, 16, Math.Min(cbLastReceive, 256))
			});
			return false;
			IL_398:
			_arrReadFromPipe = null;
			oSW = null;
			this.m_session.Timers.ClientReads = oNTS;
			if (bAbort || this.stateRead.m_requestData.Length == 0L)
			{
				FiddlerApplication.DebugSpew("Reading from client set bAbort or m_requestData was empty");
				if (this.pipeClient != null && (this.pipeClient.iUseCount < 2U || (this.pipeClient.bIsSecured && this.pipeClient.iUseCount < 3U)))
				{
					FiddlerApplication.Log.LogFormat("[Fiddler] No {0} request was received from ({1}) new client socket, port {2}.", new object[]
					{
						this.pipeClient.bIsSecured ? "HTTPS" : "HTTP",
						this.m_session.oFlags["X-ProcessInfo"],
						this.m_session.oFlags["X-CLIENTPORT"]
					});
				}
				return false;
			}
			if (this.m_headers == null)
			{
				FiddlerApplication.DebugSpew("Reading from client set either bDone or bAbort without making any headers available");
				return false;
			}
			if (this.m_session.state >= SessionStates.Done)
			{
				FiddlerApplication.DebugSpew("SessionState >= Done while reading request");
				return false;
			}
			if ("CONNECT" == this.m_headers.HTTPMethod)
			{
				this.m_session.isTunnel = true;
				this.stateRead.m_sHostFromURI = this.m_session.PathAndQuery;
			}
			this._ValidateHostDuringReadRequest();
			return this.m_headers.Exists("Host");
		}

		/// <summary>
		/// Verifies that the Hostname specified in the request line is compatible with the HOST header
		/// </summary>
		// Token: 0x0600011D RID: 285 RVA: 0x00010A78 File Offset: 0x0000EC78
		private void _ValidateHostDuringReadRequest()
		{
			if (this.stateRead.m_sHostFromURI == null)
			{
				return;
			}
			if (this.m_headers.Exists("Host"))
			{
				if (!Utilities.areOriginsEquivalent(this.stateRead.m_sHostFromURI, this.m_headers["Host"], this.m_session.isHTTPS ? 443 : (this.m_session.isFTP ? 21 : 80)) && (!this.m_session.isTunnel || !Utilities.areOriginsEquivalent(this.stateRead.m_sHostFromURI, this.m_headers["Host"], 443)))
				{
					this.m_session.oFlags["X-Original-Host"] = this.m_headers["Host"];
					this.m_session.oFlags["X-URI-Host"] = this.stateRead.m_sHostFromURI;
					if (FiddlerApplication.Prefs.GetBoolPref("fiddler.network.SetHostHeaderFromURL", true))
					{
						this.m_headers["Host"] = this.stateRead.m_sHostFromURI;
					}
				}
				return;
			}
			if ("HTTP/1.1".OICEquals(this.m_headers.HTTPVersion))
			{
				this.m_session.oFlags["X-Original-Host"] = string.Empty;
			}
			this.m_headers["Host"] = this.stateRead.m_sHostFromURI;
		}

		/// <summary>
		/// Size of buffer passed to pipe.Receive when reading from the client. 
		/// </summary>
		// Token: 0x0400005A RID: 90
		internal static int s_cbClientReadBuffer = 8192;

		// Token: 0x0400005B RID: 91
		internal static int s_SO_SNDBUF_Option = -1;

		// Token: 0x0400005C RID: 92
		internal static int s_SO_RCVBUF_Option = -1;

		/// <summary>
		/// Tracks the progress of reading the request from the client. Because of the multi-threaded nature 
		/// of some users of this field, most will make a local copy before accessing its members.
		/// </summary>
		// Token: 0x0400005D RID: 93
		private ClientChatter.RequestReaderState stateRead;

		/// <summary>
		/// The ClientPipe object which is connected to the client, or null.
		/// </summary>
		// Token: 0x0400005E RID: 94
		public ClientPipe pipeClient;

		/// <summary>
		/// Parsed Headers
		/// </summary>
		// Token: 0x0400005F RID: 95
		private HTTPRequestHeaders m_headers;

		/// <summary>
		/// The Session object which owns this ClientChatter
		/// </summary>
		// Token: 0x04000060 RID: 96
		private Session m_session;

		/// <summary>
		/// Discardable State of Read Operation
		///
		/// While it is reading a request from the client, the ClientChatter class uses a RequestReaderState object to track
		/// the state of the read. This state is discarded when the request has been completely read.
		/// </summary>
		// Token: 0x020000C2 RID: 194
		private class RequestReaderState : IDisposable
		{
			// Token: 0x060006F8 RID: 1784 RVA: 0x00037A11 File Offset: 0x00035C11
			internal RequestReaderState()
			{
				this.m_requestData = new PipeReadBuffer(true);
			}

			/// <summary>
			/// Count of body bytes read from the client. If no body bytes have yet been read, returns count of header bytes.
			/// </summary>
			/// <returns></returns>
			// Token: 0x060006F9 RID: 1785 RVA: 0x00037A28 File Offset: 0x00035C28
			internal long GetBodyBytesRead()
			{
				PipeReadBuffer prb = this.m_requestData;
				if (prb == null)
				{
					return 0L;
				}
				long iBytesRead = prb.Length;
				if (iBytesRead > (long)this.iEntityBodyOffset)
				{
					return iBytesRead - (long)this.iEntityBodyOffset;
				}
				return iBytesRead;
			}

			/// <summary>
			/// Scans requestData stream for the \r\n\r\n (or variants) sequence
			/// which indicates that the header block is complete.
			///
			/// SIDE EFFECTS:
			///             		iBodySeekProgress is updated and maintained across calls to this function
			///             		iEntityBodyOffset is updated if the end of headers is found
			/// </summary>
			/// <returns>True, if requestData contains a full set of headers</returns>
			// Token: 0x060006FA RID: 1786 RVA: 0x00037A60 File Offset: 0x00035C60
			internal bool _areHeadersAvailable(Session oS)
			{
				if (this.m_requestData.Length < 16L)
				{
					return false;
				}
				long lngDataLen = this.m_requestData.Length;
				byte[] arrData = this.m_requestData.GetBuffer();
				HTTPHeaderParseWarnings oHPW;
				bool bFoundEndOfHeaders = Parser.FindEndOfHeaders(arrData, ref this.iBodySeekProgress, lngDataLen, out oHPW);
				if (bFoundEndOfHeaders)
				{
					this.iEntityBodyOffset = this.iBodySeekProgress + 1;
					if (oHPW != HTTPHeaderParseWarnings.EndedWithLFLF)
					{
						if (oHPW == HTTPHeaderParseWarnings.EndedWithLFCRLF)
						{
							FiddlerApplication.HandleHTTPError(oS, SessionFlags.ProtocolViolationInRequest, false, false, "The Client did not send properly formatted HTTP Headers. HTTP headers\nshould be terminated with CRLFCRLF. These were terminated with LFCRLF.");
						}
					}
					else
					{
						FiddlerApplication.HandleHTTPError(oS, SessionFlags.ProtocolViolationInRequest, false, false, "The Client did not send properly formatted HTTP Headers. HTTP headers\nshould be terminated with CRLFCRLF. These were terminated with LFLF.");
					}
					return true;
				}
				return false;
			}

			// Token: 0x060006FB RID: 1787 RVA: 0x00037AEB File Offset: 0x00035CEB
			public void Dispose()
			{
				if (this.m_requestData != null)
				{
					this.m_requestData.Dispose();
					this.m_requestData = null;
				}
			}

			/// <summary>
			/// The Host pulled from the URI
			/// </summary>
			// Token: 0x04000335 RID: 821
			internal string m_sHostFromURI;

			/// <summary>
			/// Buffer holds this request's data as it is read from the pipe.
			/// </summary>
			// Token: 0x04000336 RID: 822
			internal PipeReadBuffer m_requestData;

			/// <summary>
			/// Offset to first byte of body in m_requestData
			/// </summary>
			// Token: 0x04000337 RID: 823
			internal int iEntityBodyOffset;

			/// <summary>
			/// Optimization: Offset of most recent transfer-encoded chunk
			/// </summary>
			// Token: 0x04000338 RID: 824
			internal long m_lngLastChunkInfoOffset;

			/// <summary>
			/// Optimization: tracks how far we've previously looked when determining iEntityBodyOffset
			/// </summary>
			// Token: 0x04000339 RID: 825
			internal int iBodySeekProgress;

			/// <summary>
			/// Did the request specify Transfer-Encoding: chunked
			/// </summary>
			// Token: 0x0400033A RID: 826
			internal bool bIsChunkedBody;

			/// <summary>
			/// The integer value of the Content-Length header, if any
			/// </summary>
			// Token: 0x0400033B RID: 827
			internal long iContentLength;
		}
	}
}
