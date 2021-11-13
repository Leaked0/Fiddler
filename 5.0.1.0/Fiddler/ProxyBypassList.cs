using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Fiddler
{
	/// <summary>
	/// This class maintains the Proxy Bypass List for the upstream gateway. 
	/// In the constructor, pass the desired proxy bypass string, as retrieved from WinINET for the Options screen.
	/// Then, call the IsBypass(sTarget) method to determine if the Gateway should be bypassed
	/// </summary>
	// Token: 0x02000058 RID: 88
	internal class ProxyBypassList
	{
		/// <summary>
		/// Pass the desired proxy bypass string retrieved from WinINET.
		/// </summary>
		/// <param name="sBypassList"></param>
		// Token: 0x06000395 RID: 917 RVA: 0x00021CE5 File Offset: 0x0001FEE5
		public ProxyBypassList(string sBypassList)
		{
			if (string.IsNullOrEmpty(sBypassList))
			{
				return;
			}
			this.AssignBypassList(sBypassList);
		}

		/// <summary>
		/// Does the bypassList contain any rules at all?
		/// </summary>
		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x06000396 RID: 918 RVA: 0x00021CFD File Offset: 0x0001FEFD
		public bool HasEntries
		{
			get
			{
				return this._BypassOnLocal || this._RegExBypassList != null;
			}
		}

		// Token: 0x06000397 RID: 919 RVA: 0x00021D14 File Offset: 0x0001FF14
		[Obsolete]
		public bool IsBypass(string sSchemeHostPort)
		{
			string sScheme = Utilities.TrimAfter(sSchemeHostPort, "://");
			string sHostAndPort = Utilities.TrimBefore(sSchemeHostPort, "://");
			return this.IsBypass(sScheme, sHostAndPort);
		}

		/// <summary>
		/// Given the rules for this bypasslist, should this target bypass the proxy?
		/// </summary>
		/// <param name="sScheme">The URI Scheme</param>
		/// <param name="sHostAndPort">The Host and PORT</param>
		/// <returns>True if this request should not be sent to the gateway proxy</returns>
		// Token: 0x06000398 RID: 920 RVA: 0x00021D44 File Offset: 0x0001FF44
		public bool IsBypass(string sScheme, string sHostAndPort)
		{
			if (this._BypassOnLocal && Utilities.isPlainHostName(sHostAndPort))
			{
				return true;
			}
			if (this._RegExBypassList != null)
			{
				string sSchemeHostPort = sScheme + "://" + sHostAndPort;
				for (int i = 0; i < this._RegExBypassList.Count; i++)
				{
					if (this._RegExBypassList[i].IsMatch(sSchemeHostPort))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Convert the string representing the bypass list into an array of rules escaped and ready to be turned into regular expressions
		/// </summary>
		/// <param name="sBypassList"></param>
		// Token: 0x06000399 RID: 921 RVA: 0x00021DA8 File Offset: 0x0001FFA8
		private void AssignBypassList(string sBypassList)
		{
			this._BypassOnLocal = false;
			this._RegExBypassList = null;
			if (string.IsNullOrEmpty(sBypassList))
			{
				return;
			}
			if (CONFIG.bDebugSpew)
			{
				FiddlerApplication.DebugSpew("Build Bypass List from: {0}\n", new object[] { sBypassList });
			}
			string[] arrEntries = sBypassList.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (arrEntries.Length < 1)
			{
				return;
			}
			List<string> _slBypassRules = null;
			foreach (string strEntry in arrEntries)
			{
				string strTrimmedEntry = strEntry.Trim();
				if (strTrimmedEntry.Length != 0)
				{
					if (strTrimmedEntry.OICEquals("<local>"))
					{
						this._BypassOnLocal = true;
					}
					else if (!strTrimmedEntry.OICEquals("<-loopback>"))
					{
						if (!strTrimmedEntry.Contains("://"))
						{
							strTrimmedEntry = "*://" + strTrimmedEntry;
						}
						bool bNeedsPortWildcard = strTrimmedEntry.IndexOf(':') == strTrimmedEntry.LastIndexOf(':');
						strTrimmedEntry = Utilities.RegExEscape(strTrimmedEntry, true, !bNeedsPortWildcard);
						if (bNeedsPortWildcard)
						{
							strTrimmedEntry += "(:\\d+)?$";
						}
						if (_slBypassRules == null)
						{
							_slBypassRules = new List<string>();
						}
						if (!_slBypassRules.Contains(strTrimmedEntry))
						{
							_slBypassRules.Add(strTrimmedEntry);
						}
					}
				}
			}
			if (_slBypassRules == null)
			{
				return;
			}
			if (CONFIG.bDebugSpew)
			{
				FiddlerApplication.DebugSpew("Proxy Bypass List:\n{0}\n-----\n", new object[] { string.Join("  \n", _slBypassRules.ToArray()) });
			}
			this._RegExBypassList = new List<Regex>(_slBypassRules.Count);
			foreach (string sRule in _slBypassRules)
			{
				try
				{
					Regex oRE = new Regex(sRule, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
					this._RegExBypassList.Add(oRE);
				}
				catch
				{
					FiddlerApplication.Log.LogFormat("Invalid rule in Proxy Bypass list. '{0}'", new object[] { sRule });
				}
			}
			if (this._RegExBypassList.Count < 1)
			{
				this._RegExBypassList = null;
			}
		}

		/// <summary>
		/// List of regular expressions for matching against request Scheme://HostPort.
		/// NB: This list is either null or contains at least one item.
		/// </summary>
		// Token: 0x040001AE RID: 430
		private List<Regex> _RegExBypassList;

		/// <summary>
		/// Boolean flag indicating whether the bypass list contained a &lt;local&gt; token.
		/// </summary>
		// Token: 0x040001AF RID: 431
		private bool _BypassOnLocal;
	}
}
