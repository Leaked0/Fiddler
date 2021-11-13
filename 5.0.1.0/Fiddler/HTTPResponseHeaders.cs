using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Fiddler
{
	/// <summary>
	/// HTTP Response headers object
	/// </summary>
	// Token: 0x02000040 RID: 64
	public class HTTPResponseHeaders : HTTPHeaders, ICloneable, IEnumerable<HTTPHeaderItem>, IEnumerable
	{
		/// <summary>
		/// Protect your enumeration using GetReaderLock
		/// </summary>
		// Token: 0x0600027B RID: 635 RVA: 0x00017760 File Offset: 0x00015960
		public new IEnumerator<HTTPHeaderItem> GetEnumerator()
		{
			return this.storage.GetEnumerator();
		}

		/// <summary>
		/// Protect your enumeration using GetReaderLock
		/// </summary>
		// Token: 0x0600027C RID: 636 RVA: 0x00017772 File Offset: 0x00015972
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.storage.GetEnumerator();
		}

		/// <summary>
		/// Clone this HTTPResponseHeaders object and return the result cast to an Object
		/// </summary>
		/// <returns>The new response headers object, cast to an object</returns>
		// Token: 0x0600027D RID: 637 RVA: 0x00017784 File Offset: 0x00015984
		public object Clone()
		{
			HTTPResponseHeaders oNew = (HTTPResponseHeaders)base.MemberwiseClone();
			try
			{
				base.GetReaderLock();
				oNew.storage = new List<HTTPHeaderItem>(this.storage.Count);
				foreach (HTTPHeaderItem oItem in this.storage)
				{
					oNew.storage.Add(new HTTPHeaderItem(oItem.Name, oItem.Value));
				}
			}
			finally
			{
				base.FreeReaderLock();
			}
			return oNew;
		}

		// Token: 0x0600027E RID: 638 RVA: 0x00017828 File Offset: 0x00015A28
		public override int ByteCount()
		{
			int iLen = 3;
			iLen += this.HTTPVersion.StrLen();
			iLen += this.HTTPResponseStatus.StrLen();
			try
			{
				base.GetReaderLock();
				for (int x = 0; x < this.storage.Count; x++)
				{
					iLen += 4;
					iLen += this.storage[x].Name.StrLen();
					iLen += this.storage[x].Value.StrLen();
				}
			}
			finally
			{
				base.FreeReaderLock();
			}
			iLen += 2;
			return iLen;
		}

		/// <summary>
		/// Gets or sets the text associated with the response code (e.g. "OK", "Not Found", etc)
		/// </summary>
		// Token: 0x17000079 RID: 121
		// (get) Token: 0x0600027F RID: 639 RVA: 0x000178C4 File Offset: 0x00015AC4
		// (set) Token: 0x06000280 RID: 640 RVA: 0x000178FC File Offset: 0x00015AFC
		public string StatusDescription
		{
			get
			{
				if (string.IsNullOrEmpty(this.HTTPResponseStatus))
				{
					return string.Empty;
				}
				if (this.HTTPResponseStatus.IndexOf(' ') < 1)
				{
					return string.Empty;
				}
				return Utilities.TrimBefore(this.HTTPResponseStatus, ' ');
			}
			set
			{
				this.HTTPResponseStatus = string.Format("{0} {1}", this.HTTPResponseCode, value);
			}
		}

		/// <summary>
		/// Update the response status code and text
		/// </summary>
		/// <param name="iCode">HTTP Status code (e.g. 401)</param>
		/// <param name="sDescription">HTTP Status text (e.g. "Access Denied")</param>
		// Token: 0x06000281 RID: 641 RVA: 0x0001791A File Offset: 0x00015B1A
		public void SetStatus(int iCode, string sDescription)
		{
			this.HTTPResponseCode = iCode;
			this.HTTPResponseStatus = string.Format("{0} {1}", iCode, sDescription);
		}

		/// <summary>
		/// Constructor for HTTP Response headers object
		/// </summary>
		// Token: 0x06000282 RID: 642 RVA: 0x0001793A File Offset: 0x00015B3A
		public HTTPResponseHeaders()
		{
		}

		// Token: 0x06000283 RID: 643 RVA: 0x0001794D File Offset: 0x00015B4D
		public HTTPResponseHeaders(int iStatus, string[] sHeaders)
			: this(iStatus, "Generated", sHeaders)
		{
		}

		// Token: 0x06000284 RID: 644 RVA: 0x0001795C File Offset: 0x00015B5C
		public HTTPResponseHeaders(int iStatusCode, string sStatusText, string[] sHeaders)
		{
			this.SetStatus(iStatusCode, sStatusText);
			if (sHeaders != null)
			{
				string sErrs = string.Empty;
				Parser.ParseNVPHeaders(this, sHeaders, 0, ref sErrs);
			}
		}

		/// <summary>
		/// Constructor for HTTP Response headers object
		/// </summary>
		/// <param name="encodingForHeaders">Text encoding to be used for this set of Headers when converting to a byte array</param>
		// Token: 0x06000285 RID: 645 RVA: 0x00017996 File Offset: 0x00015B96
		public HTTPResponseHeaders(Encoding encodingForHeaders)
		{
			this._HeaderEncoding = encodingForHeaders;
		}

		/// <summary>
		/// Returns a byte array representing the HTTP headers.
		/// </summary>
		/// <param name="prependStatusLine">TRUE if the response status line should be included</param>
		/// <param name="appendEmptyLine">TRUE if there should be a trailing \r\n byte sequence included</param>
		/// <returns>Byte[] containing the headers</returns>
		// Token: 0x06000286 RID: 646 RVA: 0x000179B0 File Offset: 0x00015BB0
		[CodeDescription("Returns a byte[] representing the HTTP headers.")]
		public byte[] ToByteArray(bool prependStatusLine, bool appendEmptyLine)
		{
			return this._HeaderEncoding.GetBytes(this.ToString(prependStatusLine, appendEmptyLine));
		}

		/// <summary>
		/// Returns a string containing http headers
		/// </summary>
		/// <param name="prependStatusLine">TRUE if the response status line should be included</param>
		/// <param name="appendEmptyLine">TRUE if there should be a trailing CRLF included</param>
		/// <returns>String containing http headers</returns>
		// Token: 0x06000287 RID: 647 RVA: 0x000179C8 File Offset: 0x00015BC8
		[CodeDescription("Returns a string representing the HTTP headers.")]
		public string ToString(bool prependStatusLine, bool appendEmptyLine)
		{
			StringBuilder sbResult = new StringBuilder(512);
			if (prependStatusLine)
			{
				sbResult.AppendFormat("{0} {1}\r\n", this.HTTPVersion, this.HTTPResponseStatus);
			}
			try
			{
				base.GetReaderLock();
				for (int x = 0; x < this.storage.Count; x++)
				{
					sbResult.AppendFormat("{0}: {1}\r\n", this.storage[x].Name, this.storage[x].Value);
				}
			}
			finally
			{
				base.FreeReaderLock();
			}
			if (appendEmptyLine)
			{
				sbResult.Append("\r\n");
			}
			return sbResult.ToString();
		}

		/// <summary>
		/// Returns a string containing the http headers
		/// </summary>
		/// <returns>
		/// Returns a string containing http headers with a status line but no trailing CRLF
		/// </returns>
		// Token: 0x06000288 RID: 648 RVA: 0x00017A74 File Offset: 0x00015C74
		[CodeDescription("Returns a string containing the HTTP Response headers.")]
		public override string ToString()
		{
			return this.ToString(true, false);
		}

		/// <summary>
		/// Parses a string and assigns the headers parsed to this object
		/// </summary>
		/// <param name="sHeaders">The header string</param>
		/// <returns>TRUE if the operation succeeded, false otherwise</returns>
		// Token: 0x06000289 RID: 649 RVA: 0x00017A80 File Offset: 0x00015C80
		[CodeDescription("Replaces the current Response header set using a string representing the new HTTP headers.")]
		public override bool AssignFromString(string sHeaders)
		{
			if (string.IsNullOrEmpty(sHeaders))
			{
				throw new ArgumentException("Header string must not be null or empty");
			}
			if (!sHeaders.Contains("\r\n\r\n"))
			{
				sHeaders += "\r\n\r\n";
			}
			HTTPResponseHeaders oCandidateHeaders = null;
			try
			{
				oCandidateHeaders = Parser.ParseResponse(sHeaders);
			}
			catch (Exception eX)
			{
			}
			if (oCandidateHeaders == null)
			{
				return false;
			}
			this.SetStatus(oCandidateHeaders.HTTPResponseCode, oCandidateHeaders.StatusDescription);
			this.HTTPVersion = oCandidateHeaders.HTTPVersion;
			this.storage = oCandidateHeaders.storage;
			return true;
		}

		/// <summary>
		/// Status code from HTTP Response. If setting, also set HTTPResponseStatus too!
		/// </summary>
		// Token: 0x04000121 RID: 289
		[CodeDescription("Status code from HTTP Response. Call SetStatus() instead of manipulating directly.")]
		public int HTTPResponseCode;

		/// <summary>
		/// Code AND Description of Response Status (e.g. '200 OK').
		/// </summary>
		// Token: 0x04000122 RID: 290
		[CodeDescription("Status text from HTTP Response (e.g. '200 OK'). Call SetStatus() instead of manipulating directly.")]
		public string HTTPResponseStatus = string.Empty;
	}
}
