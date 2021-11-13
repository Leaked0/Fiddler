using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fiddler
{
	/// <summary>
	/// HTTP Request headers object
	/// </summary>
	// Token: 0x02000041 RID: 65
	public class HTTPRequestHeaders : HTTPHeaders, ICloneable, IEnumerable<HTTPHeaderItem>, IEnumerable
	{
		/// <summary>
		/// Warning: You should protect your enumeration using the GetReaderLock
		/// </summary>
		// Token: 0x0600028A RID: 650 RVA: 0x00017B08 File Offset: 0x00015D08
		public new IEnumerator<HTTPHeaderItem> GetEnumerator()
		{
			return this.storage.GetEnumerator();
		}

		/// <summary>
		/// Warning: You should protect your enumeration using the GetReaderLock
		/// </summary>
		// Token: 0x0600028B RID: 651 RVA: 0x00017B1A File Offset: 0x00015D1A
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.storage.GetEnumerator();
		}

		/// <summary>
		/// Clones the HTTP request headers 
		/// </summary>
		/// <returns>The new HTTPRequestHeaders object, cast to an object</returns>
		// Token: 0x0600028C RID: 652 RVA: 0x00017B2C File Offset: 0x00015D2C
		public object Clone()
		{
			HTTPRequestHeaders oNew = (HTTPRequestHeaders)base.MemberwiseClone();
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

		// Token: 0x0600028D RID: 653 RVA: 0x00017BD0 File Offset: 0x00015DD0
		public override int ByteCount()
		{
			int iLen = 4;
			iLen += this.HTTPMethod.StrLen();
			iLen += this.RequestPath.StrLen();
			iLen += this.HTTPVersion.StrLen();
			if (!"CONNECT".OICEquals(this.HTTPMethod))
			{
				iLen += this._UriScheme.StrLen();
				iLen += this._uriUserInfo.StrLen();
				iLen += base["Host"].StrLen();
				iLen += 3;
			}
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
		/// Constructor for HTTP Request headers object
		/// </summary>
		// Token: 0x0600028E RID: 654 RVA: 0x00017CC0 File Offset: 0x00015EC0
		public HTTPRequestHeaders()
		{
		}

		// Token: 0x0600028F RID: 655 RVA: 0x00017CF4 File Offset: 0x00015EF4
		public HTTPRequestHeaders(string sPath, string[] sHeaders)
		{
			this.HTTPMethod = "GET";
			this.RequestPath = sPath.Trim();
			if (sHeaders != null)
			{
				string sErrs = string.Empty;
				Parser.ParseNVPHeaders(this, sHeaders, 0, ref sErrs);
			}
		}

		/// <summary>
		/// Constructor for HTTP Request headers object
		/// </summary>
		/// <param name="encodingForHeaders">Text encoding to be used for this set of Headers when converting to a byte array</param>
		// Token: 0x06000290 RID: 656 RVA: 0x00017D5E File Offset: 0x00015F5E
		public HTTPRequestHeaders(Encoding encodingForHeaders)
		{
			this._HeaderEncoding = encodingForHeaders;
		}

		/// <summary>
		/// The (lowercased) URI scheme for this request (https, http, or ftp)
		/// </summary>
		// Token: 0x1700007A RID: 122
		// (get) Token: 0x06000291 RID: 657 RVA: 0x00017D99 File Offset: 0x00015F99
		// (set) Token: 0x06000292 RID: 658 RVA: 0x00017DAA File Offset: 0x00015FAA
		[CodeDescription("URI Scheme for this HTTP Request; usually 'http' or 'https'")]
		public string UriScheme
		{
			get
			{
				return this._UriScheme ?? string.Empty;
			}
			set
			{
				this._UriScheme = value.ToLowerInvariant();
			}
		}

		/// <summary>
		/// Username:Password info for FTP URLs. (either null or "user:pass@")
		/// (Note: It's silly that this contains a trailing @, but whatever...)
		/// </summary>
		// Token: 0x1700007B RID: 123
		// (get) Token: 0x06000293 RID: 659 RVA: 0x00017DB8 File Offset: 0x00015FB8
		// (set) Token: 0x06000294 RID: 660 RVA: 0x00017DC0 File Offset: 0x00015FC0
		[CodeDescription("For FTP URLs, returns either null or user:pass@")]
		public string UriUserInfo
		{
			get
			{
				return this._uriUserInfo;
			}
			internal set
			{
				if (string.Empty == value)
				{
					value = null;
				}
				this._uriUserInfo = value;
			}
		}

		/// <summary>
		/// Get or set the request path as a string
		/// </summary>
		// Token: 0x1700007C RID: 124
		// (get) Token: 0x06000295 RID: 661 RVA: 0x00017DD9 File Offset: 0x00015FD9
		// (set) Token: 0x06000296 RID: 662 RVA: 0x00017DEA File Offset: 0x00015FEA
		[CodeDescription("String representing the HTTP Request path, e.g. '/path.htm'.")]
		public string RequestPath
		{
			get
			{
				return this._Path ?? string.Empty;
			}
			set
			{
				if (value == null)
				{
					value = string.Empty;
				}
				this._Path = value;
				this._RawPath = this._HeaderEncoding.GetBytes(value);
			}
		}

		/// <summary>
		/// Get or set the request path as a byte array
		/// </summary>
		// Token: 0x1700007D RID: 125
		// (get) Token: 0x06000297 RID: 663 RVA: 0x00017E0F File Offset: 0x0001600F
		// (set) Token: 0x06000298 RID: 664 RVA: 0x00017E20 File Offset: 0x00016020
		[CodeDescription("Byte array representing the HTTP Request path.")]
		public byte[] RawPath
		{
			get
			{
				return this._RawPath ?? Utilities.emptyByteArray;
			}
			set
			{
				if (value == null)
				{
					value = Utilities.emptyByteArray;
				}
				this._RawPath = Utilities.Dupe(value);
				this._Path = this._HeaderEncoding.GetString(this._RawPath);
			}
		}

		/// <summary>
		/// Parses a string and assigns the headers parsed to this object
		/// </summary>
		/// <param name="sHeaders">The header string</param>
		/// <returns>TRUE if the operation succeeded, false otherwise</returns>
		// Token: 0x06000299 RID: 665 RVA: 0x00017E50 File Offset: 0x00016050
		[CodeDescription("Replaces the current Request header set using a string representing the new HTTP headers.")]
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
			HTTPRequestHeaders oCandidateHeaders = null;
			try
			{
				oCandidateHeaders = Parser.ParseRequest(sHeaders);
			}
			catch (Exception eX)
			{
			}
			if (oCandidateHeaders == null)
			{
				return false;
			}
			this.HTTPMethod = oCandidateHeaders.HTTPMethod;
			this._Path = oCandidateHeaders._Path;
			this._RawPath = oCandidateHeaders._RawPath;
			this._UriScheme = oCandidateHeaders._UriScheme;
			this.HTTPVersion = oCandidateHeaders.HTTPVersion;
			this._uriUserInfo = oCandidateHeaders._uriUserInfo;
			this.storage = oCandidateHeaders.storage;
			return true;
		}

		/// <summary>
		/// Returns a byte array representing the HTTP headers.
		/// </summary>
		/// <param name="prependVerbLine">TRUE if the HTTP REQUEST line (method+path+httpversion) should be included</param>
		/// <param name="appendEmptyLine">TRUE if there should be a trailing \r\n byte sequence included</param>
		/// <param name="includeProtocolInPath">TRUE if the SCHEME and HOST should be included in the HTTP REQUEST LINE</param>
		/// <returns>The HTTP headers as a byte[]</returns>
		// Token: 0x0600029A RID: 666 RVA: 0x00017F04 File Offset: 0x00016104
		[CodeDescription("Returns current Request Headers as a byte array.")]
		public byte[] ToByteArray(bool prependVerbLine, bool appendEmptyLine, bool includeProtocolInPath)
		{
			return this.ToByteArray(prependVerbLine, appendEmptyLine, includeProtocolInPath, null);
		}

		/// <summary>
		/// Returns a byte array representing the HTTP headers.
		/// </summary>
		/// <param name="prependVerbLine">TRUE if the HTTP REQUEST line (method+path+httpversion) should be included</param>
		/// <param name="appendEmptyLine">TRUE if there should be a trailing \r\n byte sequence included</param>
		/// <param name="includeProtocolInPath">TRUE if the SCHEME and HOST should be included in the HTTP REQUEST LINE</param>
		/// <param name="sVerbLineHost">Only meaningful if prependVerbLine is TRUE, the host to use in the HTTP REQUEST LINE</param>
		/// <returns>The HTTP headers as a byte[]</returns>
		// Token: 0x0600029B RID: 667 RVA: 0x00017F10 File Offset: 0x00016110
		[CodeDescription("Returns current Request Headers as a byte array.")]
		public byte[] ToByteArray(bool prependVerbLine, bool appendEmptyLine, bool includeProtocolInPath, string sVerbLineHost)
		{
			if (!prependVerbLine)
			{
				return this._HeaderEncoding.GetBytes(this.ToString(false, appendEmptyLine, false));
			}
			byte[] arrMethod = Encoding.ASCII.GetBytes(this.HTTPMethod);
			byte[] arrVersion = Encoding.ASCII.GetBytes(this.HTTPVersion);
			byte[] arrHeaders = this._HeaderEncoding.GetBytes(this.ToString(false, appendEmptyLine, false));
			MemoryStream oMS = new MemoryStream(arrHeaders.Length + 1024);
			oMS.Write(arrMethod, 0, arrMethod.Length);
			oMS.WriteByte(32);
			if (includeProtocolInPath && !"CONNECT".OICEquals(this.HTTPMethod))
			{
				if (sVerbLineHost == null)
				{
					sVerbLineHost = base["Host"];
				}
				byte[] arrHost = this._HeaderEncoding.GetBytes(this._UriScheme + "://" + this._uriUserInfo + sVerbLineHost);
				oMS.Write(arrHost, 0, arrHost.Length);
			}
			if ("CONNECT".OICEquals(this.HTTPMethod) && sVerbLineHost != null)
			{
				byte[] arrHost2 = this._HeaderEncoding.GetBytes(sVerbLineHost);
				oMS.Write(arrHost2, 0, arrHost2.Length);
			}
			else
			{
				oMS.Write(this._RawPath, 0, this._RawPath.Length);
			}
			oMS.WriteByte(32);
			oMS.Write(arrVersion, 0, arrVersion.Length);
			oMS.WriteByte(13);
			oMS.WriteByte(10);
			oMS.Write(arrHeaders, 0, arrHeaders.Length);
			return oMS.ToArray();
		}

		/// <summary>
		/// Returns a string representing the HTTP headers.
		/// </summary>
		/// <param name="prependVerbLine">TRUE if the HTTP REQUEST line (method+path+httpversion) should be included</param>
		/// <param name="appendEmptyLine">TRUE if there should be a trailing CRLF sequence included</param>
		/// <param name="includeProtocolAndHostInPath">TRUE if the SCHEME and HOST should be included in the HTTP REQUEST LINE (Automatically set to FALSE for CONNECT requests)</param>
		/// <returns>The HTTP headers as a string.</returns>
		// Token: 0x0600029C RID: 668 RVA: 0x00018064 File Offset: 0x00016264
		[CodeDescription("Returns current Request Headers as a string.")]
		public string ToString(bool prependVerbLine, bool appendEmptyLine, bool includeProtocolAndHostInPath)
		{
			StringBuilder sbResult = new StringBuilder(512);
			if (prependVerbLine)
			{
				if (includeProtocolAndHostInPath && !"CONNECT".OICEquals(this.HTTPMethod))
				{
					sbResult.AppendFormat("{0} {1}://{2}{3}{4} {5}\r\n", new object[]
					{
						this.HTTPMethod,
						this._UriScheme,
						this._uriUserInfo,
						base["Host"],
						this.RequestPath,
						this.HTTPVersion
					});
				}
				else
				{
					sbResult.AppendFormat("{0} {1} {2}\r\n", this.HTTPMethod, this.RequestPath, this.HTTPVersion);
				}
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
		/// Returns a string representing the HTTP headers, without the SCHEME+HOST in the HTTP REQUEST line
		/// </summary>
		/// <param name="prependVerbLine">TRUE if the HTTP REQUEST line (method+path+httpversion) should be included</param>
		/// <param name="appendEmptyLine">TRUE if there should be a trailing CRLF sequence included</param>
		/// <returns>The header string</returns>
		// Token: 0x0600029D RID: 669 RVA: 0x00018180 File Offset: 0x00016380
		[CodeDescription("Returns a string representing the HTTP Request.")]
		public string ToString(bool prependVerbLine, bool appendEmptyLine)
		{
			return this.ToString(prependVerbLine, appendEmptyLine, false);
		}

		/// <summary>
		/// Returns a string representing the HTTP headers, without the SCHEME+HOST in the HTTP request line, and no trailing CRLF
		/// </summary>
		/// <returns>The header string</returns>
		// Token: 0x0600029E RID: 670 RVA: 0x0001818B File Offset: 0x0001638B
		[CodeDescription("Returns a string representing the HTTP Request.")]
		public override string ToString()
		{
			return this.ToString(true, false, false);
		}

		// Token: 0x04000123 RID: 291
		private string _UriScheme = "http";

		/// <summary>
		/// The HTTP Method (e.g. GET, POST, etc)
		/// </summary>
		// Token: 0x04000124 RID: 292
		[CodeDescription("HTTP Method or Verb from HTTP Request.")]
		public string HTTPMethod = string.Empty;

		// Token: 0x04000125 RID: 293
		private byte[] _RawPath = Utilities.emptyByteArray;

		// Token: 0x04000126 RID: 294
		private string _Path = string.Empty;

		// Token: 0x04000127 RID: 295
		private string _uriUserInfo;
	}
}
