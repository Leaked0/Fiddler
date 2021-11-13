using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fiddler;

namespace FiddlerCore.Common.Rules
{
	/// <summary>
	/// Rules related to key/value pairs of requests - headers, query, cookies
	/// </summary>
	// Token: 0x020000AA RID: 170
	internal class KeyValueRules
	{
		/// <summary>
		/// Match a collection with a condition with a key and a value
		/// </summary>
		/// <param name="condition">the condition to check - e.g. eq, contains</param>
		/// <param name="searchKey">the key to search for</param>
		/// <param name="searchValue">the value to search for</param>
		/// <param name="collection">the collection to search in</param>
		// Token: 0x06000689 RID: 1673 RVA: 0x00036048 File Offset: 0x00034248
		internal static bool SearchString(string condition, string searchKey, string searchValue, IEnumerable<HTTPHeaderItem> collection)
		{
			List<HTTPHeaderItem> listCollection = new List<HTTPHeaderItem>(collection);
			IEnumerable<HTTPHeaderItem> filteredItems = (string.IsNullOrEmpty(searchKey) ? collection : (from item in collection
				where ((item != null) ? item.Name : null) == searchKey
				select item));
			if (condition == "exists")
			{
				return filteredItems.Count<HTTPHeaderItem>() > 0;
			}
			if (condition == "doesnotexist")
			{
				return filteredItems.Count<HTTPHeaderItem>() == 0;
			}
			foreach (HTTPHeaderItem item2 in filteredItems)
			{
				if (ValueRules.SearchString(condition, (item2 != null) ? item2.Value : null, searchValue))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Get Cookies from a session
		/// </summary>
		/// <param name="session"></param>
		// Token: 0x0600068A RID: 1674 RVA: 0x00036110 File Offset: 0x00034310
		internal static IEnumerable<HTTPHeaderItem> GetCookies(Session session)
		{
			List<HTTPHeaderItem> requestCookies = KeyValueRules.GetCookieData(session.RequestHeaders);
			List<HTTPHeaderItem> responseCookies = KeyValueRules.GetCookieData(session.ResponseHeaders);
			return requestCookies.Concat(responseCookies);
		}

		// Token: 0x0600068B RID: 1675 RVA: 0x0003613C File Offset: 0x0003433C
		private static List<HTTPHeaderItem> GetCookieData(HTTPHeaders headers)
		{
			List<HTTPHeaderItem> result = new List<HTTPHeaderItem>();
			if (headers == null)
			{
				return result;
			}
			foreach (object obj in headers)
			{
				HTTPHeaderItem oHeader = (HTTPHeaderItem)obj;
				if (headers is HTTPRequestHeaders && (string.Equals(oHeader.Name, "Cookie", StringComparison.InvariantCultureIgnoreCase) || string.Equals(oHeader.Name, "Cookie2", StringComparison.InvariantCultureIgnoreCase)))
				{
					string sCookieValue = oHeader.Value.ToString();
					if (!string.IsNullOrEmpty(sCookieValue))
					{
						string[] cookies = sCookieValue.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string cookie in cookies)
						{
							int splitIndex = cookie.IndexOf("=");
							if (splitIndex > 0)
							{
								result.Add(new HTTPHeaderItem(cookie.Substring(0, splitIndex).Trim(), cookie.Substring(splitIndex + 1).Trim()));
							}
						}
					}
				}
				if (headers is HTTPResponseHeaders && (string.Equals(oHeader.Name, "Set-Cookie", StringComparison.InvariantCultureIgnoreCase) || string.Equals(oHeader.Name, "Set-Cookie2", StringComparison.InvariantCultureIgnoreCase)))
				{
					string sCookieValue2 = oHeader.Value.ToString();
					if (!string.IsNullOrEmpty(sCookieValue2))
					{
						HTTPHeaderItem cookie2 = KeyValueRules.ParseResponseCookie(sCookieValue2);
						if (cookie2 != null)
						{
							result.Add(cookie2);
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600068C RID: 1676 RVA: 0x000362BC File Offset: 0x000344BC
		private static HTTPHeaderItem ParseResponseCookie(string sCookieValue)
		{
			if (string.IsNullOrEmpty(sCookieValue))
			{
				return null;
			}
			int splitIndex = sCookieValue.IndexOf(";");
			string keyValue = ((splitIndex > 0) ? sCookieValue.Substring(0, splitIndex) : sCookieValue);
			int equalIndex = keyValue.Trim().IndexOf("=");
			if (equalIndex <= 0)
			{
				return null;
			}
			return new HTTPHeaderItem(keyValue.Substring(0, equalIndex).Trim(), keyValue.Substring(equalIndex + 1).Trim());
		}

		/// <summary>
		/// Replace text in header keys or values
		/// Allowed patterns - https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference
		/// </summary>
		/// <param name="headers">collection of headers (request or response)</param>
		/// <param name="condition">What to do - add, set, remove, replace.</param>
		/// <param name="headerName">the header name to update (not required)</param>
		/// <param name="find">the search string</param>
		/// <param name="value">the value (if direct set) or the replace string</param>
		/// <returns>True if headers were modified</returns>
		// Token: 0x0600068D RID: 1677 RVA: 0x00036328 File Offset: 0x00034528
		internal static bool HeadersValueRegex(HTTPHeaders headers, string condition, string headerName, string find, string value)
		{
			if (headers == null)
			{
				return false;
			}
			bool modified = false;
			if (!(condition == "set"))
			{
				if (!(condition == "add"))
				{
					if (!(condition == "append"))
					{
						if (!(condition == "remove"))
						{
							if (!(condition == "replace") && !(condition == "regex"))
							{
								return false;
							}
							for (int i = 0; i < headers.Count(); i++)
							{
								if (string.IsNullOrEmpty(headerName) || headers[i].Name == headerName)
								{
									string replaced = ((condition == "regex") ? Regex.Replace(headers[i].Value, find, value) : headers[i].Value.Replace(find, value));
									if (headers[i].Value != replaced)
									{
										headers[i].Value = replaced;
										modified = true;
									}
								}
							}
						}
						else
						{
							int count = headers.Count();
							if (string.IsNullOrEmpty(headerName))
							{
								headers.RemoveAll();
							}
							else
							{
								headers.Remove(headerName);
							}
							if (headers.Count() < count)
							{
								modified = true;
							}
						}
					}
					else
					{
						for (int j = 0; j < headers.Count(); j++)
						{
							if (string.IsNullOrEmpty(headerName) || headers[j].Name == headerName)
							{
								HTTPHeaderItem httpheaderItem = headers[j];
								httpheaderItem.Value += value;
								modified = true;
							}
						}
					}
				}
				else if (!string.IsNullOrEmpty(headerName) && headers.FindAll(headerName).Count == 0)
				{
					headers.Add(headerName, value);
					modified = true;
				}
			}
			else
			{
				for (int k = 0; k < headers.Count(); k++)
				{
					if (string.IsNullOrEmpty(headerName) || headers[k].Name == headerName)
					{
						headers[k].Value = value;
						modified = true;
					}
				}
				if (!string.IsNullOrEmpty(headerName) && !modified)
				{
					headers.Add(headerName, value);
					modified = true;
				}
			}
			return modified;
		}

		/// <summary>
		/// Replace text in request/response body
		/// Allowed patterns - https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference
		/// </summary>
		/// <param name="session">the session to search in and update</param>
		/// <param name="isRequest">True to replace in request body, False to replace in response body</param>
		/// <param name="condition">What to do - add, set, remove, replace</param>
		/// <param name="find">the value to search for if not replacing the whole body</param>
		/// <param name="value">the value to set/replace in the body</param>
		/// <returns>True if body was modified</returns>
		// Token: 0x0600068E RID: 1678 RVA: 0x00036548 File Offset: 0x00034748
		internal static bool BodyRegex(Session session, bool isRequest, string condition, string find, string value)
		{
			if (session == null || session.isTunnel)
			{
				return false;
			}
			bool modified = false;
			if (!(condition == "set"))
			{
				if (!(condition == "add"))
				{
					if (!(condition == "append"))
					{
						if (!(condition == "remove"))
						{
							if (!(condition == "replace") && !(condition == "regex"))
							{
								return false;
							}
							if (string.IsNullOrEmpty(find))
							{
								return false;
							}
							string oldBody = (isRequest ? session.GetRequestBodyAsString() : session.GetResponseBodyAsString());
							string newBody = ((condition == "regex") ? Regex.Replace(oldBody, find, value) : oldBody.Replace(find, value));
							if (oldBody != newBody)
							{
								if (isRequest)
								{
									session.utilSetRequestBody(newBody);
								}
								else
								{
									session.utilSetResponseBody(newBody);
								}
								modified = true;
							}
						}
						else
						{
							if (isRequest)
							{
								session.utilSetRequestBody(string.Empty);
							}
							else
							{
								session.utilSetResponseBody(string.Empty);
							}
							modified = true;
						}
					}
					else
					{
						string bodyAsString = (isRequest ? session.GetRequestBodyAsString() : session.GetResponseBodyAsString());
						bodyAsString += value;
						if (isRequest)
						{
							session.utilSetRequestBody(bodyAsString);
						}
						else
						{
							session.utilSetResponseBody(bodyAsString);
						}
						modified = true;
					}
				}
				else if (isRequest && (long)session.RequestBody.Length == 0L)
				{
					session.utilSetRequestBody(value);
					modified = true;
				}
				else if (!isRequest && (long)session.ResponseBody.Length == 0L)
				{
					session.utilSetResponseBody(value);
					modified = true;
				}
			}
			else
			{
				if (isRequest)
				{
					session.utilSetRequestBody(value);
				}
				else
				{
					session.utilSetResponseBody(value);
				}
				modified = true;
			}
			return modified;
		}

		/// <summary>
		/// Set and replace text in cookie values.
		/// Allowed patterns for regular expression pattern - https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference
		/// </summary>
		/// <param name="headers">collection of headers (request or response)</param>
		/// <param name="condition">What to do - add, set, remove, replace.</param>
		/// <param name="cookieName">the header name to update (not required)</param>
		/// <param name="find">the string or regular expression pattern to look for</param>
		/// <param name="value">the value (if direct set) or the replacement string</param>
		/// <returns>True if cookies were modified</returns>
		// Token: 0x0600068F RID: 1679 RVA: 0x000366D0 File Offset: 0x000348D0
		internal static bool CookieRegex(HTTPHeaders headers, string condition, string cookieName, string find, string value)
		{
			if (headers == null)
			{
				return false;
			}
			List<HTTPHeaderItem> cookies = KeyValueRules.GetCookieData(headers);
			bool modified = false;
			string condition2 = condition;
			if (!(condition2 == "set"))
			{
				if (!(condition2 == "add"))
				{
					if (!(condition2 == "append"))
					{
						if (!(condition2 == "remove"))
						{
							if (!(condition2 == "replace") && !(condition2 == "regex"))
							{
								return false;
							}
							cookies.ForEach(delegate(HTTPHeaderItem c)
							{
								if (string.IsNullOrEmpty(cookieName) || c.Name == cookieName)
								{
									string replaced = ((condition == "regex") ? Regex.Replace(c.Value, find, value) : c.Value.Replace(find, value));
									if (c.Value != replaced)
									{
										c.Value = replaced;
										modified = true;
									}
								}
							});
						}
						else if (cookies.RemoveAll((HTTPHeaderItem c) => string.IsNullOrEmpty(cookieName) || c.Name == cookieName) != 0)
						{
							modified = true;
						}
					}
					else
					{
						cookies.ForEach(delegate(HTTPHeaderItem c)
						{
							if (string.IsNullOrEmpty(cookieName) || c.Name == cookieName)
							{
								c.Value += value;
								modified = true;
							}
						});
					}
				}
				else if (!string.IsNullOrEmpty(cookieName) && cookies.Find((HTTPHeaderItem c) => c.Name == cookieName) == null)
				{
					cookies.Add(new HTTPHeaderItem(cookieName, value));
					modified = true;
				}
			}
			else
			{
				cookies.ForEach(delegate(HTTPHeaderItem c)
				{
					if (string.IsNullOrEmpty(cookieName) || c.Name == cookieName)
					{
						c.Value = value;
						modified = true;
					}
				});
				if (!string.IsNullOrEmpty(cookieName) && !modified)
				{
					cookies.Add(new HTTPHeaderItem(cookieName, value));
					modified = true;
				}
			}
			if (modified)
			{
				if (headers is HTTPRequestHeaders)
				{
					headers.Remove("Cookie");
					headers.Remove("Cookie2");
					if (cookies.Count<HTTPHeaderItem>() > 0)
					{
						string headerValue = string.Join("; ", from c in cookies
							select c.Name + "=" + c.Value);
						headers.Add("Cookie", headerValue);
					}
				}
				else
				{
					List<HTTPHeaderItem> existingHeaders = headers.FindAll("Set-Cookie");
					existingHeaders.AddRange(headers.FindAll("Set-Cookie2"));
					cookies.ForEach(delegate(HTTPHeaderItem c)
					{
						HTTPHeaderItem header = existingHeaders.Find((HTTPHeaderItem h) => h.Value.Trim().StartsWith(c.Name + "="));
						if (header != null)
						{
							int idxS = c.Name.Length + 1;
							int idxE = header.Value.IndexOf(";", idxS);
							if (idxE == -1)
							{
								idxE = header.Value.Length;
							}
							header.Value = header.Value.Remove(idxS, idxE - idxS).Insert(idxS, c.Value);
							return;
						}
						headers.Add("Set-Cookie", c.Name + "=" + c.Value);
					});
					existingHeaders.ForEach(delegate(HTTPHeaderItem existingHeader)
					{
						if (cookies.Find((HTTPHeaderItem c) => existingHeader.Value.Trim().StartsWith(c.Name + "=")) == null)
						{
							headers.Remove(existingHeader);
						}
					});
				}
			}
			return modified;
		}

		/// <summary>
		/// Update a session full URL.
		/// Allowed patterns for regular expression pattern - https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference
		/// </summary>
		/// <param name="oSession">The session to update</param>
		/// <param name="condition">What to do with the URL - add, set, remove, replace.</param>
		/// <param name="find">the string or regular expression pattern to look for</param>
		/// <param name="value">the value (if direct set) or the replacement string</param>
		/// <returns>True if the URL was modified</returns>
		// Token: 0x06000690 RID: 1680 RVA: 0x000369AC File Offset: 0x00034BAC
		internal static bool UrlValueRegex(Session oSession, string condition, string find, string value)
		{
			string url = oSession.fullUrl;
			if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(value))
			{
				return false;
			}
			bool modified = false;
			if (!(condition == "set"))
			{
				if (!(condition == "append"))
				{
					if (!(condition == "replace") && !(condition == "regex"))
					{
						return false;
					}
					string replaced = ((condition == "regex") ? Regex.Replace(url, find, value) : url.Replace(find, value));
					if (url != replaced)
					{
						url = replaced;
						modified = true;
					}
				}
				else
				{
					url += value;
					modified = true;
				}
			}
			else
			{
				url = value;
				modified = true;
			}
			if (modified)
			{
				if (!oSession.oFlags.ContainsKey("X-OriginalURL"))
				{
					oSession.oFlags["X-OriginalURL"] = oSession.fullUrl;
				}
				try
				{
					oSession.fullUrl = url;
				}
				catch (ArgumentException)
				{
					return false;
				}
				return modified;
			}
			return modified;
		}

		// Token: 0x06000691 RID: 1681 RVA: 0x00036A9C File Offset: 0x00034C9C
		private static List<HTTPHeaderItem> GetQueryParams(string sQueryString)
		{
			List<HTTPHeaderItem> result = new List<HTTPHeaderItem>();
			if (sQueryString != null && sQueryString.Length > 0)
			{
				string[] aQueryStrings = sQueryString.Split('&', StringSplitOptions.None);
				foreach (string oQSNVP in aQueryStrings)
				{
					if (oQSNVP.Trim().Length >= 1)
					{
						int iSplit = oQSNVP.IndexOf("=");
						string sName;
						string sValue;
						if (iSplit < 0)
						{
							sName = Utilities.UrlDecode(oQSNVP.Trim());
							sValue = string.Empty;
						}
						else
						{
							sName = Utilities.UrlDecode(oQSNVP.Substring(0, iSplit));
							sValue = Utilities.UrlDecode(oQSNVP.Substring(iSplit + 1));
						}
						result.Add(new HTTPHeaderItem(sName, sValue));
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Set and replace text in query parameter values.
		/// Allowed patterns for regular expression pattern - https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference
		/// </summary>
		/// <param name="oSession">The session to update.</param>
		/// <param name="condition">What to do with the query parameter values - add, set, remove, replace.</param>
		/// <param name="key">the query parameter key to update (not required)</param>
		/// <param name="find">the string or regular expression pattern to look for</param>
		/// <param name="value">the value (if direct set) or the replacement string</param>
		/// <returns>True if headers were modified</returns>
		// Token: 0x06000692 RID: 1682 RVA: 0x00036B50 File Offset: 0x00034D50
		internal static bool UrlQueryValueRegex(Session oSession, string condition, string key, string find, string value)
		{
			string url = oSession.PathAndQuery;
			int idx = url.IndexOf("?");
			if (string.IsNullOrEmpty(url))
			{
				return false;
			}
			Encoding oEncoding = Utilities.getEntityBodyEncoding(oSession.RequestHeaders, null) ?? CONFIG.oHeaderEncoding;
			List<HTTPHeaderItem> query = ((idx != -1) ? KeyValueRules.GetQueryParams(oSession.PathAndQuery.Substring(idx + 1)) : new List<HTTPHeaderItem>());
			bool modified = false;
			string condition2 = condition;
			if (!(condition2 == "set"))
			{
				if (!(condition2 == "add"))
				{
					if (!(condition2 == "append"))
					{
						if (!(condition2 == "remove"))
						{
							if (!(condition2 == "replace") && !(condition2 == "regex"))
							{
								return false;
							}
							query.ForEach(delegate(HTTPHeaderItem c)
							{
								if (string.IsNullOrEmpty(key) || c.Name == key)
								{
									string replaced = ((condition == "regex") ? Regex.Replace(c.Value, find, value) : c.Value.Replace(find, value));
									if (c.Value != replaced)
									{
										c.Value = replaced;
										modified = true;
									}
								}
							});
						}
						else if (query.RemoveAll((HTTPHeaderItem c) => string.IsNullOrEmpty(key) || c.Name == key) != 0)
						{
							modified = true;
						}
					}
					else
					{
						query.ForEach(delegate(HTTPHeaderItem c)
						{
							if (string.IsNullOrEmpty(key) || c.Name == key)
							{
								c.Value += value;
								modified = true;
							}
						});
					}
				}
				else if (!string.IsNullOrEmpty(key) && query.Find((HTTPHeaderItem c) => c.Name == key) == null)
				{
					query.Add(new HTTPHeaderItem(key, value));
					modified = true;
				}
			}
			else
			{
				query.ForEach(delegate(HTTPHeaderItem c)
				{
					if (string.IsNullOrEmpty(key) || c.Name == key)
					{
						c.Value = value;
						modified = true;
					}
				});
				if (!string.IsNullOrEmpty(key) && !modified)
				{
					query.Add(new HTTPHeaderItem(key, value));
					modified = true;
				}
			}
			if (modified)
			{
				string queryValue = ((idx != -1) ? url.Remove(idx) : url);
				if (query.Count<HTTPHeaderItem>() > 0)
				{
					queryValue = queryValue + "?" + string.Join("&", from c in query
						select Utilities.UrlEncode(c.Name, oEncoding) + "=" + Utilities.UrlEncode(c.Value, oEncoding));
				}
				if (!oSession.oFlags.ContainsKey("X-OriginalURL"))
				{
					oSession.oFlags["X-OriginalURL"] = oSession.fullUrl;
				}
				oSession.PathAndQuery = queryValue;
			}
			return modified;
		}
	}
}
