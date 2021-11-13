using System;
using System.Collections.Generic;
using System.Text;

namespace Fiddler
{
	/// <summary>
	/// The HostList allows fast determination of whether a given host is in the list. It supports leading wildcards (e.g. *.foo.com), and the special tokens  &lt;local&gt; &lt;nonlocal&gt; and &lt;loopback&gt;.
	/// Note: List is *not* threadsafe; instead of updating it, construct a new one.
	/// </summary>
	// Token: 0x0200003F RID: 63
	public class HostList
	{
		/// <summary>
		/// Generate an empty HostList
		/// </summary>
		// Token: 0x06000272 RID: 626 RVA: 0x000172A8 File Offset: 0x000154A8
		public HostList()
		{
		}

		/// <summary>
		/// Create a hostlist and assign it an initial set of sites
		/// </summary>
		/// <param name="sInitialList">List of hostnames, including leading wildcards, and optional port specifier. Special tokens are *, &lt;local&gt;, &lt;nonlocal&gt;, and &lt;loopback&gt;.</param>
		// Token: 0x06000273 RID: 627 RVA: 0x000172C6 File Offset: 0x000154C6
		public HostList(string sInitialList)
			: this()
		{
			if (!string.IsNullOrEmpty(sInitialList))
			{
				this.AssignFromString(sInitialList);
			}
		}

		/// <summary>
		/// Clear the HostList
		/// </summary>
		// Token: 0x06000274 RID: 628 RVA: 0x000172E0 File Offset: 0x000154E0
		public void Clear()
		{
			this.bLoopbackMatches = (this.bPlainHostnameMatches = (this.bNonPlainHostnameMatches = (this.bEverythingMatches = false)));
			this.slSimpleHosts.Clear();
			this.hplComplexRules.Clear();
		}

		/// <summary>
		/// Clear the List and assign the new string as the contents of the list.
		/// </summary>
		/// <param name="sIn">List of hostnames, including leading wildcards, and optional port specifier. Special tokens are *, &lt;local&gt;, &lt;nonlocal&gt;, and &lt;loopback&gt;.</param>
		/// <returns>TRUE if the list was constructed without errors</returns>
		// Token: 0x06000275 RID: 629 RVA: 0x00017328 File Offset: 0x00015528
		public bool AssignFromString(string sIn)
		{
			string sDontCare;
			return this.AssignFromString(sIn, out sDontCare);
		}

		/// <summary>
		/// Clear the list and assign the new string as the contents of the list.
		/// </summary>
		/// <param name="sIn">List of hostnames, including leading wildcards, and optional port specifier. Special tokens are *, &lt;local&gt;, &lt;nonlocal&gt;, and &lt;loopback&gt;.</param>
		/// <param name="sErrors">Outparam string containing list of parsing errors</param>
		/// <returns>TRUE if the list was constructed without errors</returns>
		// Token: 0x06000276 RID: 630 RVA: 0x00017340 File Offset: 0x00015540
		public bool AssignFromString(string sIn, out string sErrors)
		{
			sErrors = string.Empty;
			this.Clear();
			if (sIn == null)
			{
				return true;
			}
			sIn = sIn.Trim();
			if (sIn.Length < 1)
			{
				return true;
			}
			string[] sRules = sIn.ToLower().Split(new char[] { ',', ';', '\t', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string sRule in sRules)
			{
				if (sRule.Equals("*"))
				{
					this.bEverythingMatches = true;
				}
				else
				{
					if (sRule.StartsWith("<"))
					{
						if (sRule.Equals("<loopback>"))
						{
							this.bLoopbackMatches = true;
							goto IL_15B;
						}
						if (sRule.Equals("<local>"))
						{
							this.bPlainHostnameMatches = true;
							goto IL_15B;
						}
						if (sRule.Equals("<nonlocal>"))
						{
							this.bNonPlainHostnameMatches = true;
							goto IL_15B;
						}
					}
					if (sRule.Length >= 1)
					{
						if (sRule.Contains("?"))
						{
							sErrors += string.Format("Ignored invalid rule '{0}'-- ? may not appear.\n", sRule);
						}
						else if (sRule.LastIndexOf("*") > 0)
						{
							sErrors += string.Format("Ignored invalid rule '{0}'-- * may only appear once, at the front of the string.\n", sRule);
						}
						else
						{
							int iPort = -1;
							string sHostOnly;
							Utilities.CrackHostAndPort(sRule, out sHostOnly, ref iPort);
							if (-1 == iPort && !sHostOnly.StartsWith("*"))
							{
								this.slSimpleHosts.Add(sRule);
							}
							else
							{
								HostList.HostPortTuple oHP = new HostList.HostPortTuple(sHostOnly, iPort);
								this.hplComplexRules.Add(oHP);
							}
						}
					}
				}
				IL_15B:;
			}
			if (this.bNonPlainHostnameMatches && this.bPlainHostnameMatches)
			{
				this.bEverythingMatches = true;
			}
			return string.IsNullOrEmpty(sErrors);
		}

		/// <summary>
		/// Return the current list of rules as a string
		/// </summary>
		/// <returns>String containing current rules, using "; " as a delimiter between entries</returns>
		// Token: 0x06000277 RID: 631 RVA: 0x000174D4 File Offset: 0x000156D4
		public override string ToString()
		{
			StringBuilder sbOutput = new StringBuilder();
			if (this.bEverythingMatches)
			{
				sbOutput.Append("*; ");
			}
			if (this.bPlainHostnameMatches)
			{
				sbOutput.Append("<local>; ");
			}
			if (this.bNonPlainHostnameMatches)
			{
				sbOutput.Append("<nonlocal>; ");
			}
			if (this.bLoopbackMatches)
			{
				sbOutput.Append("<loopback>; ");
			}
			foreach (string sRule in this.slSimpleHosts)
			{
				sbOutput.Append(sRule);
				sbOutput.Append("; ");
			}
			foreach (HostList.HostPortTuple hpt in this.hplComplexRules)
			{
				if (hpt._bTailMatch)
				{
					sbOutput.Append("*");
				}
				sbOutput.Append(hpt._sHostname);
				if (hpt._iPort > -1)
				{
					sbOutput.Append(":");
					sbOutput.Append(hpt._iPort.ToString());
				}
				sbOutput.Append("; ");
			}
			if (sbOutput.Length > 1)
			{
				sbOutput.Remove(sbOutput.Length - 1, 1);
			}
			return sbOutput.ToString();
		}

		/// <summary>
		/// Determine if a given Host is in the list
		/// </summary>
		/// <param name="sHost">A Host string, potentially including a port</param>
		/// <returns>TRUE if the Host's hostname matches a rule in the list</returns>
		// Token: 0x06000278 RID: 632 RVA: 0x00017640 File Offset: 0x00015840
		public bool ContainsHost(string sHost)
		{
			int iOut = -1;
			string sHostname;
			Utilities.CrackHostAndPort(sHost, out sHostname, ref iOut);
			return this.ContainsHost(sHostname, iOut);
		}

		/// <summary>
		/// Determine if a given Hostname is in the list
		/// </summary>
		/// <param name="sHostname">A hostname, NOT including a port</param>
		/// <returns>TRUE if the hostname matches a rule in the list</returns>
		// Token: 0x06000279 RID: 633 RVA: 0x00017661 File Offset: 0x00015861
		public bool ContainsHostname(string sHostname)
		{
			return this.ContainsHost(sHostname, -1);
		}

		/// <summary>
		/// Determine if a given Host:Port pair matches an entry in the list
		/// </summary>
		/// <param name="sHostname">A hostname, NOT including the port</param>
		/// <param name="iPort">The port</param>
		/// <returns>TRUE if the hostname matches a rule in the list</returns>
		// Token: 0x0600027A RID: 634 RVA: 0x0001766C File Offset: 0x0001586C
		public bool ContainsHost(string sHostname, int iPort)
		{
			if (this.bEverythingMatches)
			{
				return true;
			}
			if (this.bPlainHostnameMatches || this.bNonPlainHostnameMatches)
			{
				bool bIsPlain = Utilities.isPlainHostName(sHostname);
				if (this.bPlainHostnameMatches && bIsPlain)
				{
					return true;
				}
				if (this.bNonPlainHostnameMatches && !bIsPlain)
				{
					return true;
				}
			}
			if (this.bLoopbackMatches && Utilities.isLocalhostname(sHostname))
			{
				return true;
			}
			sHostname = sHostname.ToLower();
			if (this.slSimpleHosts.Contains(sHostname))
			{
				return true;
			}
			foreach (HostList.HostPortTuple hpt in this.hplComplexRules)
			{
				if (iPort == hpt._iPort || -1 == hpt._iPort)
				{
					if (hpt._bTailMatch && sHostname.EndsWith(hpt._sHostname))
					{
						return true;
					}
					if (hpt._sHostname == sHostname)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x0400011B RID: 283
		private HashSet<string> slSimpleHosts = new HashSet<string>();

		// Token: 0x0400011C RID: 284
		private List<HostList.HostPortTuple> hplComplexRules = new List<HostList.HostPortTuple>();

		// Token: 0x0400011D RID: 285
		private bool bEverythingMatches;

		// Token: 0x0400011E RID: 286
		private bool bNonPlainHostnameMatches;

		// Token: 0x0400011F RID: 287
		private bool bPlainHostnameMatches;

		// Token: 0x04000120 RID: 288
		private bool bLoopbackMatches;

		/// <summary>
		/// This private tuple allows us to associate a Hostname and a Port
		/// </summary>
		// Token: 0x020000CB RID: 203
		private class HostPortTuple
		{
			/// <summary>
			/// Create a new HostPortTuple
			/// </summary>
			// Token: 0x06000713 RID: 1811 RVA: 0x00038D5D File Offset: 0x00036F5D
			internal HostPortTuple(string sHostname, int iPort)
			{
				this._iPort = iPort;
				if (sHostname.StartsWith("*"))
				{
					this._bTailMatch = true;
					this._sHostname = sHostname.Substring(1);
					return;
				}
				this._sHostname = sHostname;
			}

			/// <summary>
			/// Port specified in the rule
			/// </summary>
			// Token: 0x0400035F RID: 863
			public int _iPort;

			/// <summary>
			/// Hostname specified in the rule
			/// </summary>
			// Token: 0x04000360 RID: 864
			public string _sHostname;

			// Token: 0x04000361 RID: 865
			public bool _bTailMatch;
		}
	}
}
