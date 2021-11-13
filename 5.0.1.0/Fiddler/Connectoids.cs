using System;
using System.Collections.Generic;
using System.Linq;
using FiddlerCore.Utilities;
using Telerik.NetworkConnections;

namespace Fiddler
{
	// Token: 0x02000015 RID: 21
	internal class Connectoids
	{
		// Token: 0x060000E7 RID: 231 RVA: 0x0000EAA4 File Offset: 0x0000CCA4
		internal Connectoids(NetworkConnectionsManager connectionsManager, bool viewOnly = false)
		{
			this.connectionsManager = connectionsManager;
			IEnumerable<NetworkConnectionFullName> connectionNames = connectionsManager.GetAllConnectionFullNames();
			foreach (NetworkConnectionFullName connectionName in connectionNames)
			{
				if (CONFIG.bDebugSpew)
				{
					FiddlerApplication.DebugSpew("Collecting information for connection '{0}'", new object[] { connectionName });
				}
				if (!this.ConnectionNamesToInitialProxySettingsMap.ContainsKey(connectionName))
				{
					try
					{
						ProxySettings initialProxySettings = connectionsManager.GetCurrentProxySettingsForConnection(connectionName);
						if (null == initialProxySettings)
						{
							FiddlerApplication.Log.LogFormat("!WARNING: Failed to get proxy information for Connection '{0}'.", new object[] { connectionName });
						}
						else
						{
							if (!string.IsNullOrEmpty(initialProxySettings.HttpProxyHost) && !CONFIG.bIsViewOnly && !viewOnly && string.Format("{0}:{1}", initialProxySettings.HttpProxyHost, initialProxySettings.HttpProxyPort).Contains(CONFIG.sFiddlerListenHostPort))
							{
								FiddlerApplication.Log.LogString("When connecting, upstream proxy settings were already pointed at Fiddler. Clearing upstream proxy.");
								initialProxySettings = new ProxySettings();
							}
							if (!string.IsNullOrEmpty(initialProxySettings.ProxyAutoConfigUrl) && (initialProxySettings.ProxyAutoConfigUrl.OICEquals("file://" + CONFIG.GetPath("Pac")) || initialProxySettings.ProxyAutoConfigUrl.OICEquals("file:///" + Utilities.UrlPathEncode(CONFIG.GetPath("Pac").Replace('\\', '/'))) || initialProxySettings.ProxyAutoConfigUrl.OICEquals("http://" + CONFIG.sFiddlerListenHostPort + "/proxy.pac")))
							{
								FiddlerApplication.Log.LogString("When connecting, upstream proxy script was already pointed at Fiddler. Clearing upstream proxy.");
								initialProxySettings = new ProxySettings();
							}
							this.ConnectionNamesToInitialProxySettingsMap.Add(connectionName, initialProxySettings);
						}
					}
					catch (Exception eX)
					{
						FiddlerApplication.Log.LogFormat("!WARNING: Failed to get proxy information for Connection '{0}' due to {1}", new object[]
						{
							connectionName,
							Utilities.DescribeException(eX)
						});
					}
				}
			}
		}

		/// <summary>
		/// Dictionary of all Connectoids, indexed by the Connectoid's Name
		/// </summary>
		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060000E8 RID: 232 RVA: 0x0000ECAC File Offset: 0x0000CEAC
		internal Dictionary<NetworkConnectionFullName, ProxySettings> ConnectionNamesToInitialProxySettingsMap { get; } = new Dictionary<NetworkConnectionFullName, ProxySettings>();

		/// <summary>
		/// Return the configured default connectoid's proxy information.
		/// </summary>
		/// <returns>Either proxy information from "DefaultLAN" or the user-specified connectoid</returns>
		// Token: 0x060000E9 RID: 233 RVA: 0x0000ECB4 File Offset: 0x0000CEB4
		internal virtual ProxySettings GetDefaultConnectionGatewayInfo(string connectionNamespace, string connectionName)
		{
			string sConnName = CONFIG.sHookConnectionNamed;
			if (string.IsNullOrEmpty(sConnName))
			{
				sConnName = connectionName;
			}
			if (!this.ConnectionNamesToInitialProxySettingsMap.ContainsKey(new NetworkConnectionFullName(connectionNamespace, connectionName)))
			{
				sConnName = connectionName;
				if (!this.ConnectionNamesToInitialProxySettingsMap.ContainsKey(new NetworkConnectionFullName(connectionNamespace, sConnName)))
				{
					FiddlerApplication.Log.LogString(string.Format("!WARNING: The {0} Gateway information could not be obtained.", connectionName));
					return new ProxySettings();
				}
			}
			return this.ConnectionNamesToInitialProxySettingsMap[new NetworkConnectionFullName(connectionNamespace, sConnName)];
		}

		/// <summary>
		/// Enumerates all of the connectoids and determines if the bIsHooked field is incorrect. If so, correct the value 
		/// and return TRUE to indicate that work was done.
		/// </summary>
		/// <param name="fiddlerProxySettings">The Proxy:Port string to look for (e.g. Config.FiddlerListenHostPort)</param>
		/// <returns>TRUE if any of the connectoids' Hook state was inaccurate.</returns>
		// Token: 0x060000EA RID: 234 RVA: 0x0000ED28 File Offset: 0x0000CF28
		internal virtual bool MarkUnhookedConnections(ProxySettings fiddlerProxySettings)
		{
			if (CONFIG.bIsViewOnly)
			{
				return false;
			}
			bool bAnyMismatch = false;
			foreach (ProxySettings proxySettings in this.ConnectionNamesToInitialProxySettingsMap.Keys.Where(new Func<NetworkConnectionFullName, bool>(this.ShouldBeHooked)).Select(new Func<NetworkConnectionFullName, ProxySettings>(this.connectionsManager.GetCurrentProxySettingsForConnection)))
			{
				if (proxySettings != fiddlerProxySettings)
				{
					bAnyMismatch = true;
				}
			}
			return bAnyMismatch;
		}

		/// <summary>
		/// Updates all (or CONFIG.sHookConnectionNamed-specified) connectoids to point at the argument-provided proxy information.
		/// </summary>
		/// <param name="proxySettings">The proxy info to set into the Connectoid</param>
		/// <returns>TRUE if updating at least one connectoid was successful</returns>
		// Token: 0x060000EB RID: 235 RVA: 0x0000EDB4 File Offset: 0x0000CFB4
		internal virtual bool HookConnections(ProxySettings proxySettings)
		{
			if (CONFIG.bIsViewOnly)
			{
				return false;
			}
			bool bResult = false;
			foreach (NetworkConnectionFullName oC in this.ConnectionNamesToInitialProxySettingsMap.Keys)
			{
				if (this.ShouldBeHooked(oC))
				{
					this.connectionsManager.SetProxySettingsForConnections(proxySettings, new NetworkConnectionFullName[] { oC });
					bResult = true;
				}
			}
			return bResult;
		}

		// Token: 0x060000EC RID: 236 RVA: 0x0000EE34 File Offset: 0x0000D034
		internal bool ShouldBeHooked(NetworkConnectionFullName connectionName)
		{
			return CONFIG.HookAllConnections || (connectionName.Namespace == CONFIG.sHookConnectionNamespace && connectionName.Name == CONFIG.sHookConnectionNamed);
		}

		/// <summary>
		/// Restore original proxy settings for any connectoid we changed.
		/// </summary>
		/// <returns>FALSE if any connectoids failed to unhook</returns>
		// Token: 0x060000ED RID: 237 RVA: 0x0000EE64 File Offset: 0x0000D064
		internal virtual bool UnhookAllConnections()
		{
			if (CONFIG.bIsViewOnly)
			{
				return true;
			}
			foreach (NetworkConnectionFullName oC in this.ConnectionNamesToInitialProxySettingsMap.Keys.Where(new Func<NetworkConnectionFullName, bool>(this.ShouldBeHooked)))
			{
				this.connectionsManager.SetProxySettingsForConnections(this.ConnectionNamesToInitialProxySettingsMap[oC], new NetworkConnectionFullName[] { oC });
			}
			return true;
		}

		// Token: 0x04000054 RID: 84
		private readonly NetworkConnectionsManager connectionsManager;
	}
}
