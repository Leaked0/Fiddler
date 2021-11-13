using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Fiddler
{
	/// <summary>
	/// Base class for RequestHeaders and ResponseHeaders
	/// </summary>
	// Token: 0x02000042 RID: 66
	public abstract class HTTPHeaders
	{
		/// <summary>
		/// Get the Reader Lock if you plan to enumerate the Storage collection.
		/// </summary>
		// Token: 0x0600029F RID: 671 RVA: 0x00018196 File Offset: 0x00016396
		protected internal void GetReaderLock()
		{
			Monitor.Enter(this.storage);
		}

		// Token: 0x060002A0 RID: 672 RVA: 0x000181A3 File Offset: 0x000163A3
		protected internal void FreeReaderLock()
		{
			Monitor.Exit(this.storage);
		}

		/// <summary>
		/// Get the Writer Lock if you plan to change the Storage collection.
		/// NB: You only need this lock if you plan to change the collection itself; you can party on the items in the collection if you like without locking.
		/// </summary>
		// Token: 0x060002A1 RID: 673 RVA: 0x000181B0 File Offset: 0x000163B0
		protected void GetWriterLock()
		{
			Monitor.Enter(this.storage);
		}

		/// <summary>
		/// If you get the Writer lock, Free it ASAP or you're going to hang or deadlock the Session
		/// </summary>
		// Token: 0x060002A2 RID: 674 RVA: 0x000181BD File Offset: 0x000163BD
		protected void FreeWriterLock()
		{
			Monitor.Exit(this.storage);
		}

		// Token: 0x060002A3 RID: 675
		public abstract bool AssignFromString(string sHeaders);

		/// <summary>
		/// Get byte count of this HTTP header instance.
		/// NOTE: This method should've been abstract.
		/// </summary>
		/// <returns>Byte Count</returns>
		// Token: 0x060002A4 RID: 676 RVA: 0x000181CA File Offset: 0x000163CA
		public virtual int ByteCount()
		{
			return this.ToString().Length;
		}

		// Token: 0x060002A5 RID: 677 RVA: 0x000181D7 File Offset: 0x000163D7
		internal bool TryGetEntitySize(out uint iSize)
		{
			iSize = 0U;
			return !this.ExistsAndEquals("Transfer-Encoding", "chunked", false) && uint.TryParse(this["Content-Length"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out iSize);
		}

		/// <summary>
		/// Number of HTTP headers
		/// </summary>
		/// <returns>Number of HTTP headers</returns>
		// Token: 0x060002A6 RID: 678 RVA: 0x00018210 File Offset: 0x00016410
		[CodeDescription("Returns an integer representing the number of headers.")]
		public int Count()
		{
			int count;
			try
			{
				this.GetReaderLock();
				count = this.storage.Count;
			}
			finally
			{
				this.FreeReaderLock();
			}
			return count;
		}

		/// <summary>
		/// Returns all instances of the named header
		/// </summary>
		/// <param name="sHeaderName">Header name</param>
		/// <returns>List of instances of the named header</returns>
		// Token: 0x060002A7 RID: 679 RVA: 0x0001824C File Offset: 0x0001644C
		public List<HTTPHeaderItem> FindAll(string sHeaderName)
		{
			List<HTTPHeaderItem> result;
			try
			{
				this.GetReaderLock();
				result = this.storage.FindAll((HTTPHeaderItem oHI) => string.Equals(sHeaderName, oHI.Name, StringComparison.OrdinalIgnoreCase));
			}
			finally
			{
				this.FreeReaderLock();
			}
			return result;
		}

		/// <summary>
		/// Copies the Headers to a new array.
		/// Prefer this method over the enumerator to avoid cross-thread problems.
		/// </summary>
		/// <returns>An array containing HTTPHeaderItems</returns>
		// Token: 0x060002A8 RID: 680 RVA: 0x000182A0 File Offset: 0x000164A0
		public HTTPHeaderItem[] ToArray()
		{
			HTTPHeaderItem[] result;
			try
			{
				this.GetReaderLock();
				result = this.storage.ToArray();
			}
			finally
			{
				this.FreeReaderLock();
			}
			return result;
		}

		/// <summary>
		/// Returns all values of the named header in a single string, delimited by commas
		/// </summary>
		/// <param name="sHeaderName">Header</param>
		/// <returns>Each, Header's, Value</returns>
		// Token: 0x060002A9 RID: 681 RVA: 0x000182DC File Offset: 0x000164DC
		public string AllValues(string sHeaderName)
		{
			List<HTTPHeaderItem> oHIs = this.FindAll(sHeaderName);
			if (oHIs.Count == 0)
			{
				return string.Empty;
			}
			if (oHIs.Count == 1)
			{
				return oHIs[0].Value;
			}
			List<string> sValues = new List<string>();
			foreach (HTTPHeaderItem oHI in oHIs)
			{
				sValues.Add(oHI.Value);
			}
			return string.Join(", ", sValues.ToArray());
		}

		/// <summary>
		/// Returns the count of instances of the named header
		/// </summary>
		/// <param name="sHeaderName">Header name</param>
		/// <returns>Count of instances of the named header</returns>
		// Token: 0x060002AA RID: 682 RVA: 0x00018374 File Offset: 0x00016574
		public int CountOf(string sHeaderName)
		{
			int iResult = 0;
			try
			{
				this.GetReaderLock();
				this.storage.ForEach(delegate(HTTPHeaderItem oHI)
				{
					if (string.Equals(sHeaderName, oHI.Name, StringComparison.OrdinalIgnoreCase))
					{
						int iResult;
						iResult++;
						iResult = iResult;
					}
				});
			}
			finally
			{
				this.FreeReaderLock();
			}
			return iResult;
		}

		/// <summary>
		/// Enumerator for HTTPHeader storage collection
		/// </summary>
		/// <returns>Enumerator</returns>
		// Token: 0x060002AB RID: 683 RVA: 0x000183D4 File Offset: 0x000165D4
		public IEnumerator GetEnumerator()
		{
			return this.storage.GetEnumerator();
		}

		/// <summary>
		/// Gets or sets the value of a header. In the case of Gets, the value of the first header of that name is returned.
		/// If the header does not exist, returns null.
		/// In the case of Sets, the value of the first header of that name is updated.  
		/// If the header does not exist, it is added.
		/// </summary>
		// Token: 0x1700007E RID: 126
		[CodeDescription("Indexer property. Gets or sets the value of a header. In the case of Gets, the value of the FIRST header of that name is returned.\nIf the header does not exist, returns null.\nIn the case of Sets, the value of the FIRST header of that name is updated.\nIf the header does not exist, it is added.")]
		public string this[string HeaderName]
		{
			get
			{
				string empty;
				try
				{
					this.GetReaderLock();
					for (int x = 0; x < this.storage.Count; x++)
					{
						if (string.Equals(this.storage[x].Name, HeaderName, StringComparison.OrdinalIgnoreCase))
						{
							return this.storage[x].Value;
						}
					}
					empty = string.Empty;
				}
				finally
				{
					this.FreeReaderLock();
				}
				return empty;
			}
			set
			{
				for (int x = 0; x < this.storage.Count; x++)
				{
					if (string.Equals(this.storage[x].Name, HeaderName, StringComparison.OrdinalIgnoreCase))
					{
						this.storage[x].Value = value;
						return;
					}
				}
				this.Add(HeaderName, value);
			}
		}

		/// <summary>
		/// Indexer property. Returns HTTPHeaderItem by index. Throws Exception if index out of bounds
		/// </summary>
		// Token: 0x1700007F RID: 127
		[CodeDescription("Indexer property. Returns HTTPHeaderItem by index.")]
		public HTTPHeaderItem this[int iHeaderNumber]
		{
			get
			{
				HTTPHeaderItem result;
				try
				{
					this.GetReaderLock();
					result = this.storage[iHeaderNumber];
				}
				finally
				{
					this.FreeReaderLock();
				}
				return result;
			}
			set
			{
				try
				{
					this.GetWriterLock();
					this.storage[iHeaderNumber] = value;
				}
				finally
				{
					this.FreeWriterLock();
				}
			}
		}

		/// <summary>
		/// Adds a new header containing the specified name and value.
		/// </summary>
		/// <param name="sHeaderName">Name of the header to add.</param>
		/// <param name="sValue">Value of the header.</param>
		/// <returns>Returns the newly-created HTTPHeaderItem.</returns>
		// Token: 0x060002B0 RID: 688 RVA: 0x00018534 File Offset: 0x00016734
		[CodeDescription("Add a new header containing the specified name and value.")]
		public HTTPHeaderItem Add(string sHeaderName, string sValue)
		{
			HTTPHeaderItem result = new HTTPHeaderItem(sHeaderName, sValue);
			try
			{
				this.GetWriterLock();
				this.storage.Add(result);
			}
			finally
			{
				this.FreeWriterLock();
			}
			return result;
		}

		/// <summary>
		/// Adds one or more headers
		/// </summary>
		// Token: 0x060002B1 RID: 689 RVA: 0x00018578 File Offset: 0x00016778
		public void AddRange(IEnumerable<HTTPHeaderItem> collHIs)
		{
			try
			{
				this.GetWriterLock();
				this.storage.AddRange(collHIs);
			}
			finally
			{
				this.FreeWriterLock();
			}
		}

		/// <summary>
		/// Returns the Value from a token in the header. Correctly handles double-quoted strings. Requires semicolon for delimiting tokens
		/// Limitation: FAILS if semicolon is in token's value, even if quoted. 
		/// FAILS in the case of crazy headers, e.g. Header: Blah="SoughtToken=Blah" SoughtToken=MissedMe
		///
		/// We really need a "proper" header parser
		/// </summary>
		/// <param name="sHeaderName">Name of the header</param>
		/// <param name="sTokenName">Name of the token</param>
		/// <returns>Value of the token if present; otherwise, null</returns>
		// Token: 0x060002B2 RID: 690 RVA: 0x000185B0 File Offset: 0x000167B0
		[CodeDescription("Returns a string representing the value of the named token within the named header.")]
		public string GetTokenValue(string sHeaderName, string sTokenName)
		{
			string sHeaderValue = this[sHeaderName];
			if (string.IsNullOrEmpty(sHeaderValue))
			{
				return null;
			}
			return Utilities.ExtractAttributeValue(sHeaderValue, sTokenName);
		}

		/// <summary>
		/// Determines if the Headers collection contains a header of the specified name, with any value.
		/// </summary>
		/// <param name="sHeaderName">The name of the header to check. (case insensitive)</param>
		/// <returns>True, if the header exists.</returns>
		// Token: 0x060002B3 RID: 691 RVA: 0x000185D8 File Offset: 0x000167D8
		[CodeDescription("Returns true if the Headers collection contains a header of the specified (case-insensitive) name.")]
		public bool Exists(string sHeaderName)
		{
			if (string.IsNullOrEmpty(sHeaderName))
			{
				return false;
			}
			try
			{
				this.GetReaderLock();
				for (int x = 0; x < this.storage.Count; x++)
				{
					if (string.Equals(this.storage[x].Name, sHeaderName, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
			}
			finally
			{
				this.FreeReaderLock();
			}
			return false;
		}

		/// <summary>
		/// Determines if the Headers collection contains any header from the specified list, with any value.
		/// </summary>
		/// <param name="sHeaderNames">list of headers</param>
		/// <returns>True, if any named header exists.</returns>
		// Token: 0x060002B4 RID: 692 RVA: 0x00018648 File Offset: 0x00016848
		[CodeDescription("Returns true if the Headers collection contains a header of the specified (case-insensitive) name.")]
		public bool ExistsAny(IEnumerable<string> sHeaderNames)
		{
			if (sHeaderNames == null)
			{
				return false;
			}
			try
			{
				this.GetReaderLock();
				for (int x = 0; x < this.storage.Count; x++)
				{
					foreach (string s in sHeaderNames)
					{
						if (string.Equals(this.storage[x].Name, s, StringComparison.OrdinalIgnoreCase))
						{
							return true;
						}
					}
				}
			}
			finally
			{
				this.FreeReaderLock();
			}
			return false;
		}

		/// <summary>
		/// Determines if the Headers collection contains one or more headers of the specified name, and 
		/// sHeaderValue is part of one of those Headers' value.
		/// </summary>
		/// <param name="sHeaderName">The name of the header to check. (case insensitive)</param>
		/// <param name="sHeaderValue">The partial header value. (case insensitive)</param>
		/// <returns>True if the header is found and the value case-insensitively contains the parameter</returns>
		// Token: 0x060002B5 RID: 693 RVA: 0x000186E4 File Offset: 0x000168E4
		[CodeDescription("Returns true if the collection contains a header of the specified (case-insensitive) name, and sHeaderValue (case-insensitive) is part of the Header's value.")]
		public bool ExistsAndContains(string sHeaderName, string sHeaderValue)
		{
			if (string.IsNullOrEmpty(sHeaderName))
			{
				return false;
			}
			try
			{
				this.GetReaderLock();
				for (int x = 0; x < this.storage.Count; x++)
				{
					if (this.storage[x].Name.OICEquals(sHeaderName) && this.storage[x].Value.OICContains(sHeaderValue))
					{
						return true;
					}
				}
			}
			finally
			{
				this.FreeReaderLock();
			}
			return false;
		}

		/// <summary>
		/// Determines if the Headers collection contains a header of the specified name, and sHeaderValue=Header's value. Similar
		/// to a case-insensitive version of: headers[sHeaderName]==sHeaderValue, although it checks all instances of the named header.
		/// </summary>
		/// <param name="sHeaderName">The name of the header to check. (case insensitive)</param>
		/// <param name="sHeaderValue">The full header value. (case insensitive)</param>
		/// <returns>True if the header is found and the value case-insensitively matches the parameter</returns>
		// Token: 0x060002B6 RID: 694 RVA: 0x0001876C File Offset: 0x0001696C
		[CodeDescription("Returns true if the collection contains a header of the specified (case-insensitive) name, with value sHeaderValue (case-insensitive).")]
		public bool ExistsAndEquals(string sHeaderName, string sHeaderValue, bool isUrl = false)
		{
			if (string.IsNullOrEmpty(sHeaderName))
			{
				return false;
			}
			try
			{
				this.GetReaderLock();
				for (int x = 0; x < this.storage.Count; x++)
				{
					if (this.storage[x].Name.OICEquals(sHeaderName))
					{
						string sValue = this.storage[x].Value.Trim();
						bool isEqual = (isUrl ? Utilities.UrlsEquals(sValue, sHeaderValue) : sValue.OICEquals(sHeaderValue));
						if (isEqual)
						{
							return true;
						}
					}
				}
			}
			finally
			{
				this.FreeReaderLock();
			}
			return false;
		}

		/// <summary>
		/// Removes all headers from the header collection which have the specified name.
		/// </summary>
		/// <param name="sHeaderName">The name of the header to remove. (case insensitive)</param>
		// Token: 0x060002B7 RID: 695 RVA: 0x00018808 File Offset: 0x00016A08
		[CodeDescription("Removes ALL headers from the header collection which have the specified (case-insensitive) name.")]
		public void Remove(string sHeaderName)
		{
			if (string.IsNullOrEmpty(sHeaderName))
			{
				return;
			}
			try
			{
				this.GetWriterLock();
				for (int x = this.storage.Count - 1; x >= 0; x--)
				{
					if (this.storage[x].Name.OICEquals(sHeaderName))
					{
						this.storage.RemoveAt(x);
					}
				}
			}
			finally
			{
				this.FreeWriterLock();
			}
		}

		/// <summary>
		/// Removes all headers from the header collection which have the specified names.
		/// </summary>
		/// <param name="arrToRemove">Array of names of headers to remove. (case insensitive)</param>
		// Token: 0x060002B8 RID: 696 RVA: 0x0001887C File Offset: 0x00016A7C
		[CodeDescription("Removes ALL headers from the header collection which have the specified (case-insensitive) names.")]
		public void RemoveRange(string[] arrToRemove)
		{
			if (arrToRemove == null || arrToRemove.Length < 1)
			{
				return;
			}
			try
			{
				this.GetWriterLock();
				for (int x = this.storage.Count - 1; x >= 0; x--)
				{
					foreach (string sToRemove in arrToRemove)
					{
						if (this.storage[x].Name.OICEquals(sToRemove))
						{
							this.storage.RemoveAt(x);
							break;
						}
					}
				}
			}
			finally
			{
				this.FreeWriterLock();
			}
		}

		/// <summary>
		/// Removes a HTTPHeader item from the collection
		/// </summary>
		/// <param name="oRemove">The HTTPHeader item to be removed</param>
		// Token: 0x060002B9 RID: 697 RVA: 0x00018908 File Offset: 0x00016B08
		public void Remove(HTTPHeaderItem oRemove)
		{
			try
			{
				this.GetWriterLock();
				this.storage.Remove(oRemove);
			}
			finally
			{
				this.FreeWriterLock();
			}
		}

		/// <summary>
		/// Removes all HTTPHeader items from the collection
		/// </summary>
		// Token: 0x060002BA RID: 698 RVA: 0x00018944 File Offset: 0x00016B44
		public void RemoveAll()
		{
			try
			{
				this.GetWriterLock();
				this.storage.Clear();
			}
			finally
			{
				this.FreeWriterLock();
			}
		}

		/// <summary>
		/// Renames all headers in the header collection which have the specified name.
		/// </summary>
		/// <param name="sOldHeaderName">The name of the header to rename. (case insensitive)</param>
		/// <param name="sNewHeaderName">The new name for the header.</param>
		/// <returns>True if one or more replacements were made.</returns>
		// Token: 0x060002BB RID: 699 RVA: 0x0001897C File Offset: 0x00016B7C
		[CodeDescription("Renames ALL headers in the header collection which have the specified (case-insensitive) name.")]
		public bool RenameHeaderItems(string sOldHeaderName, string sNewHeaderName)
		{
			bool bMadeReplacements = false;
			try
			{
				this.GetReaderLock();
				for (int x = 0; x < this.storage.Count; x++)
				{
					if (this.storage[x].Name.OICEquals(sOldHeaderName))
					{
						this.storage[x].Name = sNewHeaderName;
						bMadeReplacements = true;
					}
				}
			}
			finally
			{
				this.FreeReaderLock();
			}
			return bMadeReplacements;
		}

		/// <summary>
		/// Text encoding to be used when converting this header object to/from a byte array
		/// </summary>
		// Token: 0x04000128 RID: 296
		protected Encoding _HeaderEncoding = CONFIG.oHeaderEncoding;

		/// <summary>
		/// HTTP version (e.g. HTTP/1.1)
		/// </summary>
		// Token: 0x04000129 RID: 297
		[CodeDescription("HTTP version (e.g. HTTP/1.1).")]
		public string HTTPVersion = "HTTP/1.1";

		/// <summary>
		/// Storage for individual HTTPHeaderItems in this header collection
		/// NB: Using a list is important, as order can matter
		/// </summary>
		// Token: 0x0400012A RID: 298
		protected List<HTTPHeaderItem> storage = new List<HTTPHeaderItem>();
	}
}
