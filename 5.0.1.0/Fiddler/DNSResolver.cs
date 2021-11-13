using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
using FiddlerCore.Utilities;

namespace Fiddler
{
	// Token: 0x0200001E RID: 30
	internal class DNSResolver
	{
		// Token: 0x06000180 RID: 384 RVA: 0x00013714 File Offset: 0x00011914
		static DNSResolver()
		{
			DNSResolver.MSEC_DNS_CACHE_LIFETIME = (ulong)((long)FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.dnscache", 150000));
			DNSResolver.dictAddresses = new Dictionary<string, DNSResolver.DNSCacheEntry>();
			FiddlerApplication.Janitor.assignWork(new SimpleEventHandler(DNSResolver.ScavengeCache), 30000U);
		}

		/// <summary>
		/// Clear the DNS Cache. Called by the NetworkChange event handler in the oProxy object
		/// </summary>
		// Token: 0x06000181 RID: 385 RVA: 0x00013778 File Offset: 0x00011978
		internal static void ClearCache()
		{
			Dictionary<string, DNSResolver.DNSCacheEntry> obj = DNSResolver.dictAddresses;
			lock (obj)
			{
				DNSResolver.dictAddresses.Clear();
			}
		}

		/// <summary>
		/// Remove all expired DNSCache entries; called by the Janitor
		/// </summary>
		// Token: 0x06000182 RID: 386 RVA: 0x000137BC File Offset: 0x000119BC
		public static void ScavengeCache()
		{
			if (DNSResolver.dictAddresses.Count < 1)
			{
				return;
			}
			if (CONFIG.bDebugSpew)
			{
				FiddlerApplication.DebugSpew("Scavenging DNS Cache...");
			}
			List<string> entriesToExpire = new List<string>();
			Dictionary<string, DNSResolver.DNSCacheEntry> obj = DNSResolver.dictAddresses;
			lock (obj)
			{
				foreach (KeyValuePair<string, DNSResolver.DNSCacheEntry> oDE in DNSResolver.dictAddresses)
				{
					if (oDE.Value.iLastLookup < Utilities.GetTickCount() - DNSResolver.MSEC_DNS_CACHE_LIFETIME)
					{
						entriesToExpire.Add(oDE.Key);
					}
				}
				if (CONFIG.bDebugSpew)
				{
					FiddlerApplication.DebugSpew(string.Concat(new string[]
					{
						"Expiring ",
						entriesToExpire.Count.ToString(),
						" of ",
						DNSResolver.dictAddresses.Count.ToString(),
						" DNS Records."
					}));
				}
				foreach (string sKey in entriesToExpire)
				{
					DNSResolver.dictAddresses.Remove(sKey);
				}
			}
			if (CONFIG.bDebugSpew)
			{
				FiddlerApplication.DebugSpew("Done scavenging DNS Cache...");
			}
		}

		/// <summary>
		/// Show the contents of the DNS Resolver cache
		/// </summary>
		/// <returns></returns>
		// Token: 0x06000183 RID: 387 RVA: 0x00013928 File Offset: 0x00011B28
		public static string InspectCache()
		{
			StringBuilder sbResult = new StringBuilder(8192);
			sbResult.AppendFormat("DNSResolver Cache\nfiddler.network.timeouts.dnscache: {0}ms\nContents\n--------\n", DNSResolver.MSEC_DNS_CACHE_LIFETIME);
			Dictionary<string, DNSResolver.DNSCacheEntry> obj = DNSResolver.dictAddresses;
			lock (obj)
			{
				foreach (KeyValuePair<string, DNSResolver.DNSCacheEntry> oDE in DNSResolver.dictAddresses)
				{
					StringBuilder sbAddressList = new StringBuilder();
					sbAddressList.Append(" [");
					foreach (IPAddress ipAddr in oDE.Value.arrAddressList)
					{
						sbAddressList.Append(ipAddr.ToString());
						sbAddressList.Append(", ");
					}
					sbAddressList.Remove(sbAddressList.Length - 2, 2);
					sbAddressList.Append("]");
					sbResult.AppendFormat("\tHostName: {0}, Age: {1}ms, AddressList:{2}\n", oDE.Key, Utilities.GetTickCount() - oDE.Value.iLastLookup, sbAddressList.ToString());
				}
			}
			sbResult.Append("--------\n");
			return sbResult.ToString();
		}

		/// <summary>
		/// Gets first available IP Address from DNS. Throws if address not found!
		/// </summary>
		/// <param name="sRemoteHost">String containing the host</param>
		/// <param name="bCheckCache">True to use Fiddler's DNS cache.</param>
		/// <returns>IPAddress of target, if found.</returns>
		// Token: 0x06000184 RID: 388 RVA: 0x00013A7C File Offset: 0x00011C7C
		public static IPAddress GetIPAddress(string sRemoteHost, bool bCheckCache)
		{
			return DNSResolver.GetIPAddressList(sRemoteHost, bCheckCache, null)[0];
		}

		// Token: 0x06000185 RID: 389 RVA: 0x00013A88 File Offset: 0x00011C88
		private static void AssignIPEPList(ServerChatter.MakeConnectionExecutionState _esState, IPAddress[] _arrIPs)
		{
			List<IPEndPoint> oDests = new List<IPEndPoint>(_arrIPs.Length);
			foreach (IPAddress ipA in _arrIPs)
			{
				oDests.Add(new IPEndPoint(ipA, _esState.iServerPort));
			}
			_esState.arrIPEPDest = oDests.ToArray();
		}

		// Token: 0x06000186 RID: 390 RVA: 0x00013AD0 File Offset: 0x00011CD0
		internal static bool ResolveWentAsync(ServerChatter.MakeConnectionExecutionState _es, SessionTimers oTimers, AsyncCallback callbackAsync)
		{
			if (_es == null)
			{
				throw new ArgumentNullException("_es");
			}
			if (callbackAsync == null)
			{
				throw new ArgumentNullException("callbackAsync");
			}
			if (_es.sServerHostname == null)
			{
				throw new InvalidOperationException("_es.sServerHostname must not be null");
			}
			string sRemoteHost = _es.sServerHostname;
			IPAddress[] arrResults = null;
			Stopwatch oSW = Stopwatch.StartNew();
			IPAddress ipDest = Utilities.IPFromString(sRemoteHost);
			if (ipDest != null)
			{
				arrResults = new IPAddress[] { ipDest };
				if (oTimers != null)
				{
					oTimers.DNSTime = (int)oSW.ElapsedMilliseconds;
				}
				DNSResolver.AssignIPEPList(_es, arrResults);
				return false;
			}
			sRemoteHost = sRemoteHost.ToLower();
			Dictionary<string, DNSResolver.DNSCacheEntry> obj = DNSResolver.dictAddresses;
			lock (obj)
			{
				DNSResolver.DNSCacheEntry oCacheEntry;
				if (DNSResolver.dictAddresses.TryGetValue(sRemoteHost, out oCacheEntry))
				{
					if (oCacheEntry.iLastLookup > Utilities.GetTickCount() - DNSResolver.MSEC_DNS_CACHE_LIFETIME)
					{
						arrResults = oCacheEntry.arrAddressList;
					}
					else
					{
						DNSResolver.dictAddresses.Remove(sRemoteHost);
					}
				}
			}
			if (arrResults != null)
			{
				if (oTimers != null)
				{
					oTimers.DNSTime = (int)oSW.ElapsedMilliseconds;
				}
				DNSResolver.AssignIPEPList(_es, arrResults);
				Interlocked.Increment(ref COUNTERS.DNSCACHE_HITS);
				return false;
			}
			if ((sRemoteHost.OICEndsWith(".onion") || sRemoteHost.OICEndsWith(".i2p")) && !FiddlerApplication.Prefs.GetBoolPref("fiddler.network.dns.ResolveOnionHosts", false))
			{
				throw new SecurityException("Hostnames ending in '.onion' and '.i2p' cannot be resolved by DNS. You must send such requests through a TOR or i2p gateway, e.g. oSession[\"X-OverrideGateway\"] = \"socks=127.0.0.1:9150\";");
			}
			Interlocked.Increment(ref COUNTERS.ASYNC_DNS);
			Interlocked.Increment(ref COUNTERS.TOTAL_ASYNC_DNS);
			Dns.BeginGetHostAddresses(sRemoteHost, delegate(IAsyncResult iar)
			{
				Interlocked.Decrement(ref COUNTERS.ASYNC_DNS);
				try
				{
					SessionTimers oST = iar.AsyncState as SessionTimers;
					if (oST != null)
					{
						oST.DNSTime = (int)oSW.ElapsedMilliseconds;
						Interlocked.Add(ref COUNTERS.TOTAL_ASYNC_DNS_MS, oSW.ElapsedMilliseconds);
					}
					IPAddress[] arrRes = Dns.EndGetHostAddresses(iar);
					arrRes = DNSResolver.trimAddressList(arrRes);
					if (arrRes.Length < 1)
					{
						throw new Exception("No valid addresses were found for this hostname");
					}
					Dictionary<string, DNSResolver.DNSCacheEntry> obj2 = DNSResolver.dictAddresses;
					lock (obj2)
					{
						if (!DNSResolver.dictAddresses.ContainsKey(sRemoteHost))
						{
							DNSResolver.dictAddresses.Add(sRemoteHost, new DNSResolver.DNSCacheEntry(arrRes));
						}
					}
					DNSResolver.AssignIPEPList(_es, arrRes);
				}
				catch (Exception eX)
				{
					_es.lastException = eX;
				}
				callbackAsync(iar);
			}, oTimers);
			return true;
		}

		/// <summary>
		/// Gets IP Addresses for host from DNS. Throws if address not found!
		/// </summary>
		/// <param name="sRemoteHost">String containing the host</param>
		/// <param name="bCheckCache">True to use Fiddler's DNS cache.</param>
		/// <param name="oTimers">The Timers object to which the DNS lookup time should be stored, or null</param>
		/// <returns>List of IPAddresses of target, if any found.</returns>
		// Token: 0x06000187 RID: 391 RVA: 0x00013CB0 File Offset: 0x00011EB0
		public static IPAddress[] GetIPAddressList(string sRemoteHost, bool bCheckCache, SessionTimers oTimers)
		{
			IPAddress[] arrResult = null;
			Stopwatch oSW = Stopwatch.StartNew();
			IPAddress ipDest = Utilities.IPFromString(sRemoteHost);
			if (ipDest != null)
			{
				arrResult = new IPAddress[] { ipDest };
				if (oTimers != null)
				{
					oTimers.DNSTime = (int)oSW.ElapsedMilliseconds;
				}
				return arrResult;
			}
			sRemoteHost = sRemoteHost.ToLower();
			if (bCheckCache)
			{
				Dictionary<string, DNSResolver.DNSCacheEntry> obj = DNSResolver.dictAddresses;
				lock (obj)
				{
					DNSResolver.DNSCacheEntry oCacheEntry;
					if (DNSResolver.dictAddresses.TryGetValue(sRemoteHost, out oCacheEntry))
					{
						if (oCacheEntry.iLastLookup > Utilities.GetTickCount() - DNSResolver.MSEC_DNS_CACHE_LIFETIME)
						{
							arrResult = oCacheEntry.arrAddressList;
						}
						else
						{
							DNSResolver.dictAddresses.Remove(sRemoteHost);
						}
					}
				}
			}
			if (arrResult == null)
			{
				if ((sRemoteHost.OICEndsWith(".onion") || sRemoteHost.OICEndsWith(".i2p")) && !FiddlerApplication.Prefs.GetBoolPref("fiddler.network.dns.ResolveOnionHosts", false))
				{
					throw new SecurityException("Hostnames ending in '.onion' and '.i2p' cannot be resolved by DNS. You must send such requests through a TOR or i2p gateway, e.g. oSession[\"X-OverrideGateway\"] = \"socks=127.0.0.1:9150\";");
				}
				try
				{
					arrResult = Dns.GetHostAddresses(sRemoteHost);
				}
				catch
				{
					if (oTimers != null)
					{
						oTimers.DNSTime = (int)oSW.ElapsedMilliseconds;
					}
					throw;
				}
				arrResult = DNSResolver.trimAddressList(arrResult);
				if (arrResult.Length < 1)
				{
					throw new Exception("No valid IPv4 addresses were found for this host.");
				}
				if (arrResult.Length != 0)
				{
					Dictionary<string, DNSResolver.DNSCacheEntry> obj2 = DNSResolver.dictAddresses;
					lock (obj2)
					{
						if (!DNSResolver.dictAddresses.ContainsKey(sRemoteHost))
						{
							DNSResolver.dictAddresses.Add(sRemoteHost, new DNSResolver.DNSCacheEntry(arrResult));
						}
					}
				}
			}
			if (oTimers != null)
			{
				oTimers.DNSTime = (int)oSW.ElapsedMilliseconds;
			}
			return arrResult;
		}

		/// <summary>
		/// Trim an address list, removing the duplicate entries, any IPv6-entries if IPv6 is disabled, 
		/// and entries beyond the COUNT_MAX_A_RECORDS limit.
		/// </summary>
		/// <param name="arrResult">The list to filter</param>
		/// <returns>A filtered address list</returns>
		// Token: 0x06000188 RID: 392 RVA: 0x00013E3C File Offset: 0x0001203C
		private static IPAddress[] trimAddressList(IPAddress[] arrResult)
		{
			List<IPAddress> listFinalAddrs = new List<IPAddress>();
			for (int i = 0; i < arrResult.Length; i++)
			{
				if (!listFinalAddrs.Contains(arrResult[i]) && (CONFIG.EnableIPv6 || arrResult[i].AddressFamily == AddressFamily.InterNetwork))
				{
					listFinalAddrs.Add(arrResult[i]);
					if (DNSResolver.COUNT_MAX_A_RECORDS == listFinalAddrs.Count)
					{
						break;
					}
				}
			}
			return listFinalAddrs.ToArray();
		}

		// Token: 0x06000189 RID: 393 RVA: 0x00013E98 File Offset: 0x00012098
		internal static string GetCanonicalName(string sHostname)
		{
			string result;
			try
			{
				IPHostEntry iphe = Dns.GetHostEntry(sHostname);
				result = iphe.HostName;
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("Failed to retrieve CNAME for \"{0}\", because '{1}'", new object[]
				{
					sHostname,
					Utilities.DescribeException(eX)
				});
				result = string.Empty;
			}
			return result;
		}

		// Token: 0x0600018A RID: 394 RVA: 0x00013EF4 File Offset: 0x000120F4
		internal static string GetAllInfo(string sHostname)
		{
			IPHostEntry iphe;
			try
			{
				iphe = Dns.GetHostEntry(sHostname);
			}
			catch (Exception eX)
			{
				return string.Format("FiddlerDNS> DNS Lookup for \"{0}\" failed because '{1}'\n", sHostname, Utilities.DescribeException(eX));
			}
			StringBuilder sbResult = new StringBuilder();
			sbResult.AppendFormat("FiddlerDNS> DNS Lookup for \"{0}\":\r\n", sHostname);
			sbResult.AppendFormat("CNAME:\t{0}\n", iphe.HostName);
			sbResult.AppendFormat("Aliases:\t{0}\n", string.Join(";", iphe.Aliases));
			sbResult.AppendLine("Addresses:");
			foreach (IPAddress ipAddr in iphe.AddressList)
			{
				sbResult.AppendFormat("\t{0}\r\n", ipAddr.ToString());
			}
			return sbResult.ToString();
		}

		/// <summary>
		/// Cache of Hostname-&gt;Address mappings
		/// </summary>
		// Token: 0x040000BC RID: 188
		private static readonly Dictionary<string, DNSResolver.DNSCacheEntry> dictAddresses;

		/// <summary>
		/// Number of milliseconds that a DNS cache entry may be reused without validation.
		/// </summary>
		// Token: 0x040000BD RID: 189
		internal static ulong MSEC_DNS_CACHE_LIFETIME;

		/// <summary>
		/// Maximum number of A/AAAA records to cache for DNS entries.
		/// Beware: Changing this number changes how many IP-failovers Fiddler will perform if fiddler.network.dns.fallback is set,
		/// and increasing the number will consume more memory in the cache.
		/// </summary>
		// Token: 0x040000BE RID: 190
		private static readonly int COUNT_MAX_A_RECORDS = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.dns.MaxAddressCount", 5);

		/// <summary>
		/// A DNSCacheEntry holds a cached resolution from the DNS
		/// </summary>
		// Token: 0x020000C6 RID: 198
		private class DNSCacheEntry
		{
			/// <summary>
			/// Construct a new cache entry
			/// </summary>
			/// <param name="arrIPs">The address information to add to the cache</param>
			// Token: 0x06000705 RID: 1797 RVA: 0x00038A45 File Offset: 0x00036C45
			internal DNSCacheEntry(IPAddress[] arrIPs)
			{
				this.iLastLookup = Utilities.GetTickCount();
				this.arrAddressList = arrIPs;
			}

			/// <summary>
			/// TickCount of this record's creation
			/// </summary>
			// Token: 0x0400034F RID: 847
			internal ulong iLastLookup;

			/// <summary>
			/// IPAddresses for this hostname
			/// </summary>
			// Token: 0x04000350 RID: 848
			internal IPAddress[] arrAddressList;
		}
	}
}
