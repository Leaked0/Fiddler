using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using FiddlerCore.Utilities;
using Microsoft.Win32;
using Telerik.NetworkConnections;
using Telerik.NetworkConnections.Linux;
using Telerik.NetworkConnections.Mac;
using Telerik.NetworkConnections.Windows;

namespace Fiddler
{
	/// <summary>
	/// The core proxy object which accepts connections from clients and creates session objects from those connections.
	/// </summary>
	// Token: 0x02000057 RID: 87
	public class Proxy : IDisposable
	{
		/// <summary>
		/// Returns a string of information about this instance and the ServerPipe reuse pool
		/// </summary>
		/// <returns>A multiline string</returns>
		// Token: 0x06000361 RID: 865 RVA: 0x0001FB10 File Offset: 0x0001DD10
		public override string ToString()
		{
			return string.Format("Proxy instance is listening for requests on Port #{0}. HTTPS SubjectCN: {1}\n\n{2}", this.ListenPort, this._sHTTPSHostname ?? "<None>", Proxy.htServerPipePool.InspectPool());
		}

		/// <summary>
		/// Returns true if the proxy is listening on a port.
		/// </summary>
		// Token: 0x170000A0 RID: 160
		// (get) Token: 0x06000362 RID: 866 RVA: 0x0001FB40 File Offset: 0x0001DD40
		public bool IsListening
		{
			get
			{
				return this.oAcceptor != null && this.oAcceptor.IsBound;
			}
		}

		/// <summary>
		/// The port on which this instance is listening
		/// </summary>
		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x06000363 RID: 867 RVA: 0x0001FB58 File Offset: 0x0001DD58
		public int ListenPort
		{
			get
			{
				if (this.oAcceptor != null)
				{
					IPEndPoint ipEP = this.oAcceptor.LocalEndPoint as IPEndPoint;
					if (ipEP != null)
					{
						return ipEP.Port;
					}
				}
				return 0;
			}
		}

		/// <summary>
		/// Returns true if Fiddler believes it is currently registered as the Local System proxy
		/// </summary>
		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x06000364 RID: 868 RVA: 0x0001FB89 File Offset: 0x0001DD89
		// (set) Token: 0x06000365 RID: 869 RVA: 0x0001FB91 File Offset: 0x0001DD91
		[Obsolete("Use Telerik.NetworkConnections.NetworkConnectionsManager.GetCurrentProxySettingsForConnection(*)/SetProxySettingsForConnections(*) to manipulate current proxy settings.")]
		public bool IsAttached
		{
			get
			{
				return this._bIsAttached;
			}
			set
			{
				if (value)
				{
					this.Attach();
					return;
				}
				this.Detach();
			}
		}

		/// <summary>
		/// This event handler fires when Fiddler detects that it is (unexpectedly) no longer the system's registered proxy
		/// </summary>
		// Token: 0x14000011 RID: 17
		// (add) Token: 0x06000366 RID: 870 RVA: 0x0001FBA8 File Offset: 0x0001DDA8
		// (remove) Token: 0x06000367 RID: 871 RVA: 0x0001FBE0 File Offset: 0x0001DDE0
		[Obsolete("Use Telerik.NetworkConnections.NetworkConnectionsManager.ProxySettingsChanged event instead.")]
		public event EventHandler DetachedUnexpectedly;

		// Token: 0x06000368 RID: 872 RVA: 0x0001FC18 File Offset: 0x0001DE18
		[Obsolete("Use Telerik.NetworkConnections.NetworkConnectionsManager.ProxySettingsChanged event instead.")]
		protected virtual void OnDetachedUnexpectedly()
		{
			EventHandler oHandlers = this.DetachedUnexpectedly;
			if (oHandlers != null)
			{
				oHandlers(this, EventArgs.Empty);
			}
		}

		// Token: 0x06000369 RID: 873 RVA: 0x0001FC3C File Offset: 0x0001DE3C
		internal Proxy(bool isPrimary, ProxySettings upstreamProxySettings)
		{
			this.upstreamProxySettings = upstreamProxySettings;
			this.connectionsManager = this.InitializeNetworkConnections();
			if (isPrimary)
			{
				NetworkChange.NetworkAvailabilityChanged += this.NetworkChange_NetworkAvailabilityChanged;
				NetworkChange.NetworkAddressChanged += this.NetworkChange_NetworkAddressChanged;
				try
				{
					this.watcherPrefNotify = new PreferenceBag.PrefWatcher?(FiddlerApplication.Prefs.AddWatcher("fiddler.network", new EventHandler<PrefChangeEventArgs>(this.onNetworkPrefsChange)));
					this.SetDefaultEgressEndPoint(FiddlerApplication.Prefs["fiddler.network.egress.ip"]);
					CONFIG.SetNoDecryptList(FiddlerApplication.Prefs["fiddler.network.https.NoDecryptionHosts"]);
					CONFIG.SetNoDecryptListInvert(FiddlerApplication.Prefs.GetBoolPref("fiddler.network.https.NoDecryptionHosts.Invert", false));
					CONFIG.sFiddlerListenHostPort = string.Format("{0}:{1}", FiddlerApplication.Prefs.GetStringPref("fiddler.network.proxy.RegistrationHostName", "127.0.0.1").ToLower(), CONFIG.ListenPort);
					ClientChatter.s_cbClientReadBuffer = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.sockets.ClientReadBufferSize", ClientChatter.s_cbClientReadBuffer);
					ServerChatter.s_cbServerReadBuffer = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.sockets.ServerReadBufferSize", ServerChatter.s_cbServerReadBuffer);
					ClientChatter.s_SO_SNDBUF_Option = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.sockets.Client_SO_SNDBUF", ClientChatter.s_SO_SNDBUF_Option);
					ClientChatter.s_SO_RCVBUF_Option = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.sockets.Client_SO_RCVBUF", ClientChatter.s_SO_RCVBUF_Option);
					ServerChatter.s_SO_SNDBUF_Option = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.sockets.Server_SO_SNDBUF", ServerChatter.s_SO_SNDBUF_Option);
					ServerChatter.s_SO_RCVBUF_Option = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.sockets.Server_SO_RCVBUF", ServerChatter.s_SO_RCVBUF_Option);
				}
				catch (Exception eX)
				{
				}
			}
		}

		// Token: 0x0600036A RID: 874 RVA: 0x0001FDD8 File Offset: 0x0001DFD8
		private NetworkConnectionsManager InitializeNetworkConnections()
		{
			List<INetworkConnectionsDetector> platformDetectors = new List<INetworkConnectionsDetector>();
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				platformDetectors.Add(new WinINetNetworkConnectionsDetector());
				platformDetectors.Add(new RasNetworkConnectionsDetector());
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				platformDetectors.Add(new MacNetworkConnectionsDetector());
			}
			else
			{
				if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					throw new PlatformNotSupportedException("Proxy cannot be used on '" + RuntimeInformation.OSDescription + "' platform.");
				}
				platformDetectors.Add(new LinuxNetworkConnectionsDetector());
			}
			return new NetworkConnectionsManager(platformDetectors);
		}

		/// <summary>
		/// Change the outbound IP address used to send traffic
		/// </summary>
		/// <param name="sEgressIP"></param>
		// Token: 0x0600036B RID: 875 RVA: 0x0001FE68 File Offset: 0x0001E068
		private void SetDefaultEgressEndPoint(string sEgressIP)
		{
			if (string.IsNullOrEmpty(sEgressIP))
			{
				this._DefaultEgressEndPoint = null;
				return;
			}
			IPAddress theIP;
			if (IPAddress.TryParse(sEgressIP, out theIP))
			{
				this._DefaultEgressEndPoint = new IPEndPoint(theIP, 0);
				return;
			}
			this._DefaultEgressEndPoint = null;
		}

		/// <summary>
		/// Watch for relevent changes on the Preferences object
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="oPCE"></param>
		// Token: 0x0600036C RID: 876 RVA: 0x0001FEA4 File Offset: 0x0001E0A4
		private void onNetworkPrefsChange(object sender, PrefChangeEventArgs oPCE)
		{
			if (oPCE.PrefName.OICStartsWith("fiddler.network.timeouts."))
			{
				if (oPCE.PrefName.OICEquals("fiddler.network.timeouts.serverpipe.send.initial"))
				{
					ServerPipe._timeoutSendInitial = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.serverpipe.send.initial", -1);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.timeouts.serverpipe.send.reuse"))
				{
					ServerPipe._timeoutSendReused = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.serverpipe.send.reuse", -1);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.timeouts.serverpipe.receive.initial"))
				{
					ServerPipe._timeoutReceiveInitial = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.serverpipe.receive.initial", -1);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.timeouts.serverpipe.receive.reuse"))
				{
					ServerPipe._timeoutReceiveReused = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.serverpipe.receive.reuse", -1);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.timeouts.serverpipe.reuse"))
				{
					PipePool.MSEC_PIPE_POOLED_LIFETIME = (uint)FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.serverpipe.reuse", 115000);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.timeouts.clientpipe.receive.initial"))
				{
					ClientPipe._timeoutFirstReceive = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.clientpipe.receive.initial", 45000);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.timeouts.clientpipe.receive.loop"))
				{
					ClientPipe._timeoutReceiveLoop = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.clientpipe.receive.loop", 60000);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.timeouts.clientpipe.idle"))
				{
					ClientPipe._timeoutIdle = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.clientpipe.idle", 115000);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.timeouts.dnscache"))
				{
					DNSResolver.MSEC_DNS_CACHE_LIFETIME = (ulong)((long)FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.dnscache", 150000));
					return;
				}
				return;
			}
			else
			{
				if (oPCE.PrefName.OICEquals("fiddler.network.sockets.ClientReadBufferSize"))
				{
					ClientChatter.s_cbClientReadBuffer = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.sockets.ClientReadBufferSize", 8192);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.sockets.ServerReadBufferSize"))
				{
					ServerChatter.s_cbServerReadBuffer = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.sockets.ServerReadBufferSize", 32768);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.sockets.Server_SO_SNDBUF"))
				{
					ServerChatter.s_SO_SNDBUF_Option = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.sockets.Server_SO_SNDBUF", -1);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.sockets.Server_SO_RCVBUF"))
				{
					ServerChatter.s_SO_RCVBUF_Option = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.sockets.Server_SO_RCVBUF", -1);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.sockets.Client_SO_SNDBUF"))
				{
					ClientChatter.s_SO_SNDBUF_Option = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.sockets.Client_SO_SNDBUF", -1);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.sockets.Client_SO_RCVBUF"))
				{
					ClientChatter.s_SO_RCVBUF_Option = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.sockets.Client_SO_RCVBUF", -1);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.egress.ip"))
				{
					this.SetDefaultEgressEndPoint(oPCE.ValueString);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.https.NoDecryptionHosts"))
				{
					CONFIG.SetNoDecryptList(oPCE.ValueString);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.https.NoDecryptionHosts.Invert"))
				{
					CONFIG.SetNoDecryptListInvert(oPCE.ValueBool);
					return;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.https.DropSNIAlerts"))
				{
					ServerPipe._bEatTLSAlerts = oPCE.ValueBool;
				}
				if (oPCE.PrefName.OICEquals("fiddler.network.proxy.RegistrationHostName"))
				{
					CONFIG.sFiddlerListenHostPort = string.Format("{0}:{1}", FiddlerApplication.Prefs.GetStringPref("fiddler.network.proxy.RegistrationHostName", "127.0.0.1").ToLower(), CONFIG.ListenPort);
					return;
				}
				return;
			}
		}

		/// <summary>
		/// Called whenever Windows reports that the system's NetworkAddress has changed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		// Token: 0x0600036D RID: 877 RVA: 0x000201FC File Offset: 0x0001E3FC
		private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
		{
			try
			{
				DNSResolver.ClearCache();
				FiddlerApplication.Log.LogString("NetworkAddressChanged.");
				if (this.oAutoProxy != null)
				{
					this.oAutoProxy.iAutoProxySuccessCount = 0;
				}
				this._DetermineGatewayIPEndPoints();
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogString(eX.ToString());
			}
		}

		/// <summary>
		/// Called by Windows whenever network availability goes up or down.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		// Token: 0x0600036E RID: 878 RVA: 0x00020260 File Offset: 0x0001E460
		private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
		{
			try
			{
				this.PurgeServerPipePool();
				FiddlerApplication.Log.LogFormat("fiddler.network.availability.change> Network Available: {0}", new object[] { e.IsAvailable });
				if (e.IsAvailable && this.IsAttached)
				{
					NetworkConnectionsManager manager = this.InitializeNetworkConnections();
					Connectoids connectoids = new Connectoids(manager, true);
					ProxySettings proxySettings = ((connectoids != null) ? connectoids.GetDefaultConnectionGatewayInfo(CONFIG.sHookConnectionNamespace, CONFIG.sHookConnectionNamed) : null);
					bool flag;
					if (((proxySettings != null) ? proxySettings.HttpProxyHost : null) == this.fiddlerProxySettings.HttpProxyHost)
					{
						ushort? num = ((proxySettings != null) ? new ushort?(proxySettings.HttpProxyPort) : null);
						int? num2 = ((num != null) ? new int?((int)num.GetValueOrDefault()) : null);
						int httpProxyPort = (int)this.fiddlerProxySettings.HttpProxyPort;
						flag = (num2.GetValueOrDefault() == httpProxyPort) & (num2 != null);
					}
					else
					{
						flag = false;
					}
					bool isFiddlerSetAsProxy = flag;
					FiddlerApplication.Log.LogFormat("fiddler.network.availability.change> Is Fiddler set as proxy: {0}", new object[] { isFiddlerSetAsProxy });
					if (!isFiddlerSetAsProxy)
					{
						FiddlerApplication.Log.LogFormat("fiddler.network.availability.change> Current proxy: {0}:{1}", new object[]
						{
							(proxySettings != null) ? proxySettings.HttpProxyHost : null,
							(proxySettings != null) ? new ushort?(proxySettings.HttpProxyPort) : null
						});
						FiddlerApplication.Log.LogFormat("fiddler.network.availability.change> Fiddler proxy: {0}:{1}", new object[]
						{
							this.fiddlerProxySettings.HttpProxyHost,
							this.fiddlerProxySettings.HttpProxyPort
						});
					}
				}
			}
			catch (Exception eX)
			{
			}
		}

		// Token: 0x0600036F RID: 879 RVA: 0x00020414 File Offset: 0x0001E614
		[CodeDescription("Send a custom request through the proxy, blocking until it completes (or aborts).")]
		public Session SendRequestAndWait(HTTPRequestHeaders oHeaders, byte[] arrRequestBodyBytes, StringDictionary oNewFlags, EventHandler<StateChangeEventArgs> onStateChange)
		{
			ManualResetEvent oMRE = new ManualResetEvent(false);
			EventHandler<StateChangeEventArgs> ehStateChange = delegate(object o, StateChangeEventArgs scea)
			{
				if (scea.newState >= SessionStates.Done)
				{
					FiddlerApplication.DebugSpew("SendRequestAndWait Session #{0} reached state {1}", new object[]
					{
						(o as Session).id,
						scea.newState
					});
					oMRE.Set();
				}
				if (onStateChange != null)
				{
					onStateChange(o, scea);
				}
			};
			Session oNewSession = this.SendRequest(oHeaders, arrRequestBodyBytes, oNewFlags, ehStateChange);
			oMRE.WaitOne();
			return oNewSession;
		}

		/// <summary>
		/// Directly inject a session into the Fiddler pipeline, returning a reference to it.
		/// NOTE: This method will THROW any exceptions to its caller.
		/// </summary>
		/// <param name="oHeaders">HTTP Request Headers</param>
		/// <param name="arrRequestBodyBytes">HTTP Request body (or null)</param>
		/// <param name="oNewFlags">StringDictionary of Session Flags (or null)</param>
		/// <returns>The new Session</returns>
		// Token: 0x06000370 RID: 880 RVA: 0x00020460 File Offset: 0x0001E660
		[CodeDescription("Send a custom request through the proxy. Hook the OnStateChanged event of the returned Session to monitor progress")]
		public Session SendRequest(HTTPRequestHeaders oHeaders, byte[] arrRequestBodyBytes, StringDictionary oNewFlags)
		{
			return this.SendRequest(oHeaders, arrRequestBodyBytes, oNewFlags, null);
		}

		/// <summary>
		/// Directly inject a session into the Fiddler pipeline, returning a reference to it.
		/// NOTE: This method will THROW any exceptions to its caller.
		/// </summary>
		/// <param name="oHeaders">HTTP Request Headers</param>
		/// <param name="arrRequestBodyBytes">HTTP Request body (or null)</param>
		/// <param name="oNewFlags">StringDictionary of Session Flags (or null)</param>
		/// <param name="onStateChange">Event Handler to notify when the session changes state</param>
		/// <returns>The new Session</returns>
		// Token: 0x06000371 RID: 881 RVA: 0x0002046C File Offset: 0x0001E66C
		[CodeDescription("Send a custom request through the proxy. Hook the OnStateChanged event of the returned Session to monitor progress")]
		public Session SendRequest(HTTPRequestHeaders oHeaders, byte[] arrRequestBodyBytes, StringDictionary oNewFlags, EventHandler<StateChangeEventArgs> onStateChange)
		{
			if (oHeaders.ExistsAndContains("Fiddler-Encoding", "base64"))
			{
				oHeaders.Remove("Fiddler-Encoding");
				if (!Utilities.IsNullOrEmpty(arrRequestBodyBytes))
				{
					arrRequestBodyBytes = Convert.FromBase64String(Encoding.ASCII.GetString(arrRequestBodyBytes));
					if (oNewFlags == null)
					{
						oNewFlags = new StringDictionary();
					}
					oNewFlags["x-Builder-FixContentLength"] = "CFE-required";
				}
			}
			if (oHeaders.Exists("Fiddler-Host"))
			{
				if (oNewFlags == null)
				{
					oNewFlags = new StringDictionary();
				}
				oNewFlags["x-OverrideHost"] = oHeaders["Fiddler-Host"];
				oNewFlags["X-IgnoreCertCNMismatch"] = "Overrode HOST";
				oHeaders.Remove("Fiddler-Host");
			}
			if (oNewFlags != null && oNewFlags.ContainsKey("x-Builder-FixContentLength"))
			{
				if (arrRequestBodyBytes != null && !oHeaders.ExistsAndContains("Transfer-Encoding", "chunked"))
				{
					if (!Utilities.HTTPMethodAllowsBody(oHeaders.HTTPMethod) && arrRequestBodyBytes.Length == 0)
					{
						oHeaders.Remove("Content-Length");
					}
					else
					{
						oHeaders["Content-Length"] = ((long)arrRequestBodyBytes.Length).ToString();
					}
				}
				else
				{
					oHeaders.Remove("Content-Length");
				}
			}
			Session newSession = new Session((HTTPRequestHeaders)oHeaders.Clone(), arrRequestBodyBytes, false);
			newSession.SetBitFlag(SessionFlags.RequestGeneratedByFiddler, true);
			if (onStateChange != null)
			{
				newSession.OnStateChanged += onStateChange;
			}
			if (oNewFlags != null && oNewFlags.Count > 0)
			{
				foreach (object obj in oNewFlags)
				{
					DictionaryEntry oDE = (DictionaryEntry)obj;
					newSession.oFlags[(string)oDE.Key] = oNewFlags[(string)oDE.Key];
				}
			}
			if (newSession.oFlags.ContainsKey("x-AutoAuth"))
			{
				string sAuthHeader = newSession.oRequest.headers["Authorization"];
				if (sAuthHeader.OICContains("NTLM") || sAuthHeader.OICContains("Negotiate") || sAuthHeader.OICContains("Digest"))
				{
					newSession.oRequest.headers.Remove("Authorization");
				}
				sAuthHeader = newSession.oRequest.headers["Proxy-Authorization"];
				if (sAuthHeader.OICContains("NTLM") || sAuthHeader.OICContains("Negotiate") || sAuthHeader.OICContains("Digest"))
				{
					newSession.oRequest.headers.Remove("Proxy-Authorization");
				}
			}
			newSession.ExecuteOnThreadPool();
			return newSession;
		}

		/// <summary>
		/// Directly inject a session into the Fiddler pipeline, returning a reference to it.
		/// NOTE: This method will THROW any exceptions to its caller.
		/// </summary>
		/// <param name="sRequest">String representing the HTTP request. If headers only, be sure to end with CRLFCRLF</param>
		/// <param name="oNewFlags">StringDictionary of Session Flags (or null)</param>
		/// <returns>The new session</returns>
		// Token: 0x06000372 RID: 882 RVA: 0x000206E4 File Offset: 0x0001E8E4
		public Session SendRequest(string sRequest, StringDictionary oNewFlags)
		{
			byte[] arrBytes = CONFIG.oHeaderEncoding.GetBytes(sRequest);
			int iHeaderLen;
			int iOffset;
			HTTPHeaderParseWarnings oHPW;
			if (!Parser.FindEntityBodyOffsetFromArray(arrBytes, out iHeaderLen, out iOffset, out oHPW))
			{
				throw new ArgumentException("sRequest did not represent a valid HTTP request", "sRequest");
			}
			string sHeaders = CONFIG.oHeaderEncoding.GetString(arrBytes, 0, iHeaderLen) + "\r\n\r\n";
			HTTPRequestHeaders oRH = new HTTPRequestHeaders();
			if (!oRH.AssignFromString(sHeaders))
			{
				throw new ArgumentException("sRequest did not contain valid HTTP headers", "sRequest");
			}
			byte[] arrBody;
			if (1 > arrBytes.Length - iOffset)
			{
				arrBody = Utilities.emptyByteArray;
			}
			else
			{
				arrBody = new byte[arrBytes.Length - iOffset];
				Buffer.BlockCopy(arrBytes, iOffset, arrBody, 0, arrBody.Length);
			}
			return this.SendRequest(oRH, arrBody, oNewFlags, null);
		}

		// Token: 0x06000373 RID: 883 RVA: 0x00020790 File Offset: 0x0001E990
		[Obsolete("This overload of InjectCustomRequest is obsolete. Use a different version.", true)]
		public void InjectCustomRequest(HTTPRequestHeaders oHeaders, byte[] arrRequestBodyBytes, bool bRunRequestRules, bool bViewResult)
		{
			StringDictionary oSD = new StringDictionary();
			oSD["x-From-Builder"] = "true";
			if (bViewResult)
			{
				oSD["x-Builder-Inspect"] = "1";
			}
			this.InjectCustomRequest(oHeaders, arrRequestBodyBytes, oSD);
		}

		/// <summary>
		/// [DEPRECATED] Directly inject a session into the Fiddler pipeline.
		/// NOTE: This method will THROW any exceptions to its caller.
		/// </summary>
		/// <see cref="M:Fiddler.Proxy.SendRequest(Fiddler.HTTPRequestHeaders,System.Byte[],System.Collections.Specialized.StringDictionary)" />
		/// <param name="oHeaders">HTTP Request Headers</param>
		/// <param name="arrRequestBodyBytes">HTTP Request body (or null)</param>
		/// <param name="oNewFlags">StringDictionary of Session Flags (or null)</param>
		// Token: 0x06000374 RID: 884 RVA: 0x000207D0 File Offset: 0x0001E9D0
		public void InjectCustomRequest(HTTPRequestHeaders oHeaders, byte[] arrRequestBodyBytes, StringDictionary oNewFlags)
		{
			this.SendRequest(oHeaders, arrRequestBodyBytes, oNewFlags);
		}

		/// <summary>
		/// [DEPRECATED] Directly inject a session into the Fiddler pipeline.
		/// NOTE: This method will THROW any exceptions to its caller.
		/// </summary>
		/// <see cref="M:Fiddler.Proxy.SendRequest(System.String,System.Collections.Specialized.StringDictionary)" />
		/// <param name="sRequest">String representing the HTTP request. If headers only, be sure to end with CRLFCRLF</param>
		/// <param name="oNewFlags">StringDictionary of Session Flags (or null)</param>
		// Token: 0x06000375 RID: 885 RVA: 0x000207DC File Offset: 0x0001E9DC
		public void InjectCustomRequest(string sRequest, StringDictionary oNewFlags)
		{
			this.SendRequest(sRequest, oNewFlags);
		}

		/// <summary>
		/// [DEPRECATED]: This version does no validation of the request data, and doesn't set SessionFlags.RequestGeneratedByFiddler
		/// Send a custom HTTP request to Fiddler's listening endpoint (127.0.0.1:8888 by default).
		/// NOTE: This method will THROW any exceptions to its caller and blocks the current thread.
		/// </summary>
		/// <see cref="M:Fiddler.Proxy.SendRequest(System.String,System.Collections.Specialized.StringDictionary)" />
		/// <param name="sRequest">String representing the HTTP request. If headers only, be sure to end with CRLFCRLF</param>
		// Token: 0x06000376 RID: 886 RVA: 0x000207E8 File Offset: 0x0001E9E8
		public void InjectCustomRequest(string sRequest)
		{
			if (this.oAcceptor == null)
			{
				this.InjectCustomRequest(sRequest, null);
				return;
			}
			Socket oInjector = new Socket(IPAddress.Loopback.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			oInjector.Connect(new IPEndPoint(IPAddress.Loopback, CONFIG.ListenPort));
			oInjector.Send(Encoding.UTF8.GetBytes(sRequest));
			oInjector.Shutdown(SocketShutdown.Both);
			oInjector.Close();
		}

		/// <summary>
		/// This function, when given a scheme host[:port], returns the gateway information of the proxy to forward requests to.
		/// </summary>
		/// <param name="sURIScheme">URIScheme: use http, https, or ftp</param>
		/// <param name="sHostAndPort">Host for which to return gateway information</param>
		/// <returns>IPEndPoint of gateway to use, or NULL</returns>
		// Token: 0x06000377 RID: 887 RVA: 0x0002084C File Offset: 0x0001EA4C
		public IPEndPoint FindGatewayForOrigin(string sURIScheme, string sHostAndPort)
		{
			if (string.IsNullOrEmpty(sURIScheme))
			{
				return null;
			}
			if (string.IsNullOrEmpty(sHostAndPort))
			{
				return null;
			}
			if (CONFIG.UpstreamGateway == GatewayType.None)
			{
				return null;
			}
			if (Utilities.isLocalhost(sHostAndPort))
			{
				return null;
			}
			if (sURIScheme.OICEquals("http") && sHostAndPort.EndsWith(":80", StringComparison.Ordinal))
			{
				sHostAndPort = sHostAndPort.Substring(0, sHostAndPort.Length - 3);
			}
			else if (sURIScheme.OICEquals("https") && sHostAndPort.EndsWith(":443", StringComparison.Ordinal))
			{
				sHostAndPort = sHostAndPort.Substring(0, sHostAndPort.Length - 4);
			}
			else if (sURIScheme.OICEquals("ftp") && sHostAndPort.EndsWith(":21", StringComparison.Ordinal))
			{
				sHostAndPort = sHostAndPort.Substring(0, sHostAndPort.Length - 3);
			}
			AutoProxy myAutoProxy = this.oAutoProxy;
			if (myAutoProxy != null && myAutoProxy.iAutoProxySuccessCount > -1)
			{
				IPEndPoint _ipepResult;
				if (myAutoProxy.GetAutoProxyForUrl(sURIScheme + "://" + sHostAndPort + "/", out _ipepResult))
				{
					myAutoProxy.iAutoProxySuccessCount = 1;
					return _ipepResult;
				}
				if (myAutoProxy.iAutoProxySuccessCount == 0 && !FiddlerApplication.Prefs.GetBoolPref("fiddler.network.gateway.UseFailedAutoProxy", false))
				{
					FiddlerApplication.Log.LogString("AutoProxy failed. Disabling for this network.");
					myAutoProxy.iAutoProxySuccessCount = -1;
				}
			}
			ProxyBypassList myBypassList = this.oBypassList;
			if (myBypassList != null && myBypassList.IsBypass(sURIScheme, sHostAndPort))
			{
				return null;
			}
			if (sURIScheme.OICEquals("http"))
			{
				return this._ipepHttpGateway;
			}
			if (sURIScheme.OICEquals("https"))
			{
				return this._ipepHttpsGateway;
			}
			if (sURIScheme.OICEquals("ftp"))
			{
				return this._ipepFtpGateway;
			}
			return null;
		}

		// Token: 0x06000378 RID: 888 RVA: 0x000209C4 File Offset: 0x0001EBC4
		public string GetWpadUrl()
		{
			AutoProxy autoProxy = this.oAutoProxy;
			if (autoProxy == null)
			{
				return null;
			}
			return autoProxy.GetWPADUrl();
		}

		/// <summary>
		/// Accept the connection and pass it off to a handler thread
		/// </summary>
		/// <param name="ar"></param>
		// Token: 0x06000379 RID: 889 RVA: 0x000209DC File Offset: 0x0001EBDC
		private void AcceptConnection(IAsyncResult ar)
		{
			try
			{
				ProxyExecuteParams oParams = new ProxyExecuteParams(this.oAcceptor.EndAccept(ar), this._oHTTPSCertificate);
				ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback(Session.CreateAndExecute), oParams);
			}
			catch (ObjectDisposedException exODE)
			{
				FiddlerApplication.Log.LogFormat("!ERROR - Fiddler Acceptor failed to AcceptConnection: {0}", new object[] { Utilities.DescribeException(exODE) });
				return;
			}
			catch (Exception e)
			{
				FiddlerApplication.Log.LogFormat("!WARNING - Fiddler Acceptor failed to AcceptConnection: {0}", new object[] { Utilities.DescribeException(e) });
			}
			try
			{
				this.oAcceptor.BeginAccept(new AsyncCallback(this.AcceptConnection), null);
			}
			catch (Exception e2)
			{
				FiddlerApplication.Log.LogFormat("!ERROR - Fiddler Acceptor failed to call BeginAccept: {0}", new object[] { Utilities.DescribeException(e2) });
			}
		}

		/// <summary>
		/// Register as the system proxy for WinINET and set the Dynamic registry key for other FiddlerHook
		/// </summary>
		/// <returns>True if the proxy registration was successful</returns>
		// Token: 0x0600037A RID: 890 RVA: 0x00020AC0 File Offset: 0x0001ECC0
		[Obsolete("Use Telerik.NetworkConnections.NetworkConnectionsManager.GetCurrentProxySettingsForConnection(*)/SetProxySettingsForConnections(*) to manipulate current proxy settings.")]
		public bool Attach()
		{
			return this.Attach(true);
		}

		/// <summary>
		/// If we get a notice that the proxy registry key has changed, wait 50ms and then check to see
		/// if the key is pointed at us. If not, raise the alarm.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		// Token: 0x0600037B RID: 891 RVA: 0x00020AC9 File Offset: 0x0001ECC9
		[Obsolete]
		private void ProxyRegistryKeysChanged(object sender, EventArgs e)
		{
			if (!this._bIsAttached || this._bDetaching)
			{
				return;
			}
			if (!FiddlerApplication.Prefs.GetBoolPref("fiddler.proxy.WatchRegistry", true))
			{
				return;
			}
			ScheduledTasks.ScheduleWork("VerifyAttached", 50U, new SimpleEventHandler(this.VerifyAttached));
		}

		/// <summary>
		/// If we are supposed to be "attached", we re-verify the registry keys, and if they are corrupt, notify
		/// our host of the discrepency.
		/// </summary>
		// Token: 0x0600037C RID: 892 RVA: 0x00020B08 File Offset: 0x0001ED08
		[Obsolete]
		internal void VerifyAttached()
		{
			FiddlerApplication.Log.LogString("WinINET Registry change detected. Verifying proxy keys are intact...");
			bool bRegistryOk = true;
			try
			{
				if (this.oAllConnectoids != null)
				{
					bRegistryOk = !this.oAllConnectoids.MarkUnhookedConnections(this.fiddlerProxySettings);
					if (!bRegistryOk)
					{
						FiddlerApplication.Log.LogString("WinINET API indicates that Fiddler is no longer attached.");
					}
				}
			}
			catch (Exception eX)
			{
			}
			if (bRegistryOk)
			{
				using (RegistryKey oReg = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", false))
				{
					if (oReg != null)
					{
						if (1 != Utilities.GetRegistryInt(oReg, "ProxyEnable", 0))
						{
							bRegistryOk = false;
						}
						string sProxy = oReg.GetValue("ProxyServer") as string;
						if (string.IsNullOrEmpty(sProxy))
						{
							bRegistryOk = false;
						}
						else
						{
							if (!sProxy.OICEquals(CONFIG.sFiddlerListenHostPort) && !sProxy.OICContains("http=" + CONFIG.sFiddlerListenHostPort))
							{
								bRegistryOk = false;
								FiddlerApplication.Log.LogFormat("WinINET Registry had config: '{0}'", new object[] { sProxy });
							}
							if (bRegistryOk)
							{
								string sProxyURL = oReg.GetValue("AutoConfigURL") as string;
								if (!string.IsNullOrEmpty(sProxyURL))
								{
									bRegistryOk = sProxyURL.OICContains(Proxy._GetPACScriptURL());
									if (!bRegistryOk)
									{
										FiddlerApplication.Log.LogFormat("WinINET Registry had config: 'URL={0}'", new object[] { sProxyURL });
									}
								}
							}
						}
					}
				}
			}
			if (!bRegistryOk)
			{
				this.OnDetachedUnexpectedly();
			}
		}

		// Token: 0x0600037D RID: 893 RVA: 0x00020C60 File Offset: 0x0001EE60
		[Obsolete]
		internal bool Attach(bool bCollectGWInfo)
		{
			if (this._bIsAttached)
			{
				return true;
			}
			if (CONFIG.bIsViewOnly)
			{
				return false;
			}
			if (bCollectGWInfo)
			{
				this.CollectConnectoidAndGatewayInfo(true);
			}
			string fiddlerHostname = FiddlerApplication.Prefs.GetStringPref("fiddler.network.proxy.RegistrationHostName", "127.0.0.1");
			bool useUpstreamSettings = bCollectGWInfo && (CONFIG.UpstreamGateway == GatewayType.System || CONFIG.UpstreamGateway == GatewayType.Manual);
			this.fiddlerProxySettings = new ProxySettings(false, CONFIG.HookWithPAC, CONFIG.HookWithPAC ? Proxy._GetPACScriptURL() : null, CONFIG.sHostsThatBypassFiddler, true, fiddlerHostname, (ushort)CONFIG.ListenPort, CONFIG.bCaptureCONNECT || (useUpstreamSettings && this.upstreamProxySettings.HttpsProxyEnabled), CONFIG.bCaptureCONNECT ? fiddlerHostname : (useUpstreamSettings ? this.upstreamProxySettings.HttpsProxyHost : null), (ushort)(CONFIG.bCaptureCONNECT ? CONFIG.ListenPort : ((int)(useUpstreamSettings ? this.upstreamProxySettings.HttpsProxyPort : 0))), CONFIG.CaptureFTP || (useUpstreamSettings && this.upstreamProxySettings.FtpProxyEnabled), CONFIG.CaptureFTP ? fiddlerHostname : (useUpstreamSettings ? this.upstreamProxySettings.FtpProxyHost : null), (ushort)(CONFIG.CaptureFTP ? CONFIG.ListenPort : ((int)(useUpstreamSettings ? this.upstreamProxySettings.FtpProxyPort : 0))), useUpstreamSettings && this.upstreamProxySettings.SocksProxyEnabled, useUpstreamSettings ? this.upstreamProxySettings.SocksProxyHost : null, useUpstreamSettings ? this.upstreamProxySettings.SocksProxyPort : 0);
			if (!bCollectGWInfo)
			{
				this.CollectConnectoidAndGatewayInfo(true);
			}
			if (this.oAllConnectoids.HookConnections(this.fiddlerProxySettings))
			{
				this._bIsAttached = true;
				FiddlerApplication.OnFiddlerAttach();
				if (FiddlerApplication.Prefs.GetBoolPref("fiddler.proxy.WatchRegistry", true) && this.connectionsManager != null)
				{
					this.connectionsManager.ProxySettingsChanged += new EventHandler<ProxySettingsChangedEventArgs>(this.ProxyRegistryKeysChanged);
				}
				return true;
			}
			FiddlerApplication.Log.LogString("Error: Failed to register Fiddler as the system proxy.");
			return false;
		}

		// Token: 0x0600037E RID: 894 RVA: 0x00020E34 File Offset: 0x0001F034
		private static string _GetPACScriptURL()
		{
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.proxy.pacfile.usefileprotocol", true))
			{
				return "file://" + CONFIG.GetPath("Pac");
			}
			return "http://" + CONFIG.sFiddlerListenHostPort + "/proxy.pac";
		}

		/// <summary>
		/// This method sets up the connectoid list and updates gateway information. Called by the Attach() method, or 
		/// called on startup if Fiddler isn't configured to attach automatically.
		/// </summary>
		// Token: 0x0600037F RID: 895 RVA: 0x00020E74 File Offset: 0x0001F074
		[Obsolete]
		internal void CollectConnectoidAndGatewayInfo(bool shouldRefreshUpstreamGatewayInfo)
		{
			try
			{
				this.connectionsManager = this.InitializeNetworkConnections();
				this.oAllConnectoids = new Connectoids(this.connectionsManager, false);
				if (shouldRefreshUpstreamGatewayInfo)
				{
					this.RefreshUpstreamGatewayInformation();
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogString(eX.ToString());
			}
		}

		/// <summary>
		/// Given an address list, walks the list until it's able to successfully make a connection.
		/// Used for finding an available Gateway when we have a list to choose from
		/// </summary>
		/// <param name="sHostPortList">A string, e.g. PROXY1:80</param>
		/// <returns>The IP:Port of the first alive endpoint for the specified host/port</returns>
		// Token: 0x06000380 RID: 896 RVA: 0x00020ED0 File Offset: 0x0001F0D0
		internal static IPEndPoint GetFirstRespondingEndpoint(string sHostPortList)
		{
			if (Utilities.IsNullOrWhiteSpace(sHostPortList))
			{
				return null;
			}
			sHostPortList = Utilities.TrimAfter(sHostPortList, ';');
			IPEndPoint ipepResult = null;
			int iGatewayPort = 80;
			string sGatewayHost;
			Utilities.CrackHostAndPort(sHostPortList, out sGatewayHost, ref iGatewayPort);
			IPAddress[] arrGatewayIPs;
			try
			{
				arrGatewayIPs = DNSResolver.GetIPAddressList(sGatewayHost, true, null);
			}
			catch
			{
				FiddlerApplication.Log.LogFormat("fiddler.network.gateway> Unable to resolve upstream proxy '{0}'... ignoring.", new object[] { sGatewayHost });
				return null;
			}
			IPEndPoint result;
			try
			{
				foreach (IPAddress addrCandidate in arrGatewayIPs)
				{
					try
					{
						using (Socket oSocket = new Socket(addrCandidate.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
						{
							oSocket.NoDelay = true;
							if (FiddlerApplication.oProxy._DefaultEgressEndPoint != null)
							{
								oSocket.Bind(FiddlerApplication.oProxy._DefaultEgressEndPoint);
							}
							oSocket.Connect(addrCandidate, iGatewayPort);
							ipepResult = new IPEndPoint(addrCandidate, iGatewayPort);
						}
						break;
					}
					catch (Exception eX)
					{
						if (!FiddlerApplication.Prefs.GetBoolPref("fiddler.network.dns.fallback", true))
						{
							break;
						}
						FiddlerApplication.Log.LogFormat("fiddler.network.gateway.connect>Connection to {0} failed. {1}. Will try DNS Failover if available.", new object[]
						{
							addrCandidate.ToString(),
							eX.Message
						});
					}
				}
				result = ipepResult;
			}
			catch (Exception eX2)
			{
				result = null;
			}
			return result;
		}

		/// <summary>
		/// Set internal fields pointing at upstream proxies.
		/// </summary>
		// Token: 0x06000381 RID: 897 RVA: 0x0002102C File Offset: 0x0001F22C
		private void _DetermineGatewayIPEndPoints()
		{
			if (null == this.upstreamProxySettings)
			{
				return;
			}
			if (this.upstreamProxySettings.HttpProxyEnabled)
			{
				this._ipepHttpGateway = Proxy.GetFirstRespondingEndpoint(string.Format("{0}:{1}", this.upstreamProxySettings.HttpProxyHost, this.upstreamProxySettings.HttpProxyPort));
			}
			if (this.upstreamProxySettings.HttpsProxyEnabled)
			{
				if (string.Format("{0}:{1}", this.upstreamProxySettings.HttpsProxyHost, this.upstreamProxySettings.HttpsProxyPort) == string.Format("{0}:{1}", this.upstreamProxySettings.HttpProxyHost, this.upstreamProxySettings.HttpProxyPort))
				{
					this._ipepHttpsGateway = this._ipepHttpGateway;
				}
				else
				{
					this._ipepHttpsGateway = Proxy.GetFirstRespondingEndpoint(string.Format("{0}:{1}", this.upstreamProxySettings.HttpsProxyHost, this.upstreamProxySettings.HttpsProxyPort));
				}
			}
			if (this.upstreamProxySettings.FtpProxyEnabled)
			{
				if (string.Format("{0}:{1}", this.upstreamProxySettings.FtpProxyHost, this.upstreamProxySettings.FtpProxyPort) == string.Format("{0}:{1}", this.upstreamProxySettings.HttpProxyHost, this.upstreamProxySettings.HttpProxyPort))
				{
					this._ipepFtpGateway = this._ipepHttpGateway;
				}
				else
				{
					this._ipepFtpGateway = Proxy.GetFirstRespondingEndpoint(string.Format("{0}:{1}", this.upstreamProxySettings.FtpProxyHost, this.upstreamProxySettings.FtpProxyPort));
				}
			}
			if (!string.IsNullOrEmpty(this.upstreamProxySettings.BypassHosts))
			{
				this.oBypassList = new ProxyBypassList(this.upstreamProxySettings.BypassHosts);
				if (!this.oBypassList.HasEntries)
				{
					this.oBypassList = null;
				}
			}
		}

		/// <summary>
		/// Detach the proxy by setting the registry keys and sending a Windows Message
		/// </summary>
		/// <returns>True if the proxy settings were successfully detached</returns>
		// Token: 0x06000382 RID: 898 RVA: 0x000211FF File Offset: 0x0001F3FF
		[Obsolete("Use Telerik.NetworkConnections.NetworkConnectionsManager.GetCurrentProxySettingsForConnection(*)/SetProxySettingsForConnections(*) to manipulate current proxy settings.")]
		public bool Detach()
		{
			return this.Detach(false);
		}

		/// <summary>
		/// Detach the proxy by setting the registry keys and sending a Windows Message
		/// </summary>
		/// <returns>True if the proxy settings were successfully detached</returns>
		// Token: 0x06000383 RID: 899 RVA: 0x00021208 File Offset: 0x0001F408
		[Obsolete]
		internal bool Detach(bool bSkipVerifyAttached)
		{
			if (!bSkipVerifyAttached && !this._bIsAttached)
			{
				return true;
			}
			if (CONFIG.bIsViewOnly)
			{
				return true;
			}
			try
			{
				this._bDetaching = true;
				if (!this.oAllConnectoids.UnhookAllConnections())
				{
					return false;
				}
				this._bIsAttached = false;
				FiddlerApplication.OnFiddlerDetach();
			}
			finally
			{
				this._bDetaching = false;
			}
			return true;
		}

		// Token: 0x06000384 RID: 900 RVA: 0x00021270 File Offset: 0x0001F470
		internal string _GetUpstreamPACScriptText()
		{
			return Proxy.sUpstreamPACScript;
		}

		// Token: 0x06000385 RID: 901 RVA: 0x00021278 File Offset: 0x0001F478
		internal string _GetPACScriptText()
		{
			string sJSFindProxyForURLBody = FiddlerApplication.Prefs.GetStringPref("fiddler.proxy.pacfile.text", "return 'PROXY " + CONFIG.sFiddlerListenHostPort + "';");
			return "// Autogenerated file; do not edit. Rewritten on attach and detach of Fiddler.\r\n\r\nfunction FindProxyForURL(url, host){\r\n  " + sJSFindProxyForURLBody + "\r\n}";
		}

		/// <summary>
		/// Stop the proxy by closing the socket.
		/// </summary>
		// Token: 0x06000386 RID: 902 RVA: 0x000212BC File Offset: 0x0001F4BC
		internal void Stop()
		{
			if (this.oAcceptor == null)
			{
				return;
			}
			try
			{
				this.oAcceptor.LingerState = new LingerOption(true, 0);
				this.oAcceptor.Close();
			}
			catch (Exception eX)
			{
				FiddlerApplication.DebugSpew("oProxy.Dispose threw an exception: " + eX.Message);
			}
		}

		/// <summary>
		/// Start the proxy by binding to the local port and accepting connections
		/// </summary>
		/// <param name="iPort">Port to listen on</param>
		/// <param name="bAllowRemote">TRUE to allow remote connections</param>
		/// <returns></returns>
		// Token: 0x06000387 RID: 903 RVA: 0x0002131C File Offset: 0x0001F51C
		internal bool Start(int iPort, bool bAllowRemote)
		{
			bool bBindIPv6 = false;
			try
			{
				bBindIPv6 = bAllowRemote && CONFIG.EnableIPv6 && Socket.OSSupportsIPv6;
			}
			catch (Exception eX)
			{
				if (eX is ConfigurationErrorsException)
				{
					string title = ".NET Configuration Error";
					string[] array = new string[16];
					array[0] = "A Microsoft .NET configuration file (listed below) is corrupt and contains invalid data. You can often correct this error by installing updates from WindowsUpdate and/or reinstalling the .NET Framework.\n\n";
					array[1] = eX.Message;
					array[2] = "\nSource: ";
					array[3] = eX.Source;
					array[4] = "\n";
					array[5] = eX.StackTrace;
					array[6] = "\n\n";
					int num = 7;
					Exception innerException = eX.InnerException;
					array[num] = ((innerException != null) ? innerException.ToString() : null);
					array[8] = "\nFiddler v";
					int num2 = 9;
					Version thisAssemblyVersion = Utilities.ThisAssemblyVersion;
					array[num2] = ((thisAssemblyVersion != null) ? thisAssemblyVersion.ToString() : null);
					array[10] = ((8 == IntPtr.Size) ? " (x64) " : " (x86) ");
					array[11] = " [.NET ";
					int num3 = 12;
					Version version = Environment.Version;
					array[num3] = ((version != null) ? version.ToString() : null);
					array[13] = " on ";
					array[14] = Environment.OSVersion.VersionString;
					array[15] = "] ";
					string message = string.Concat(array);
					FiddlerApplication.Log.LogFormat("{0}: {1}", new object[] { title, message });
					this.oAcceptor = null;
					return false;
				}
			}
			string sProcessListeningOnPort = FiddlerSock.GetListeningProcess(iPort);
			try
			{
				if (bBindIPv6)
				{
					this.oAcceptor = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
					if (Environment.OSVersion.Version.Major > 5)
					{
						this.oAcceptor.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
					}
				}
				else
				{
					this.oAcceptor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				}
				if (CONFIG.ForceExclusivePort)
				{
					this.oAcceptor.ExclusiveAddressUse = true;
				}
				try
				{
					if (!string.IsNullOrEmpty(sProcessListeningOnPort))
					{
						FiddlerApplication.Log.LogFormat("! WARNING: Port {0} is already in use (for at least some IP addresses) by '{1}'", new object[] { iPort, sProcessListeningOnPort });
					}
					this.oAcceptor.Bind(new IPEndPoint(bAllowRemote ? (bBindIPv6 ? IPAddress.IPv6Any : IPAddress.Any) : IPAddress.Loopback, iPort));
				}
				catch (SocketException eXS)
				{
				}
				this.oAcceptor.Listen(50);
			}
			catch (SocketException eXS2)
			{
				string sSpecificErrorString = string.Empty;
				string sSpecificErrorTitle = "Fiddler Cannot Listen";
				int errorCode = eXS2.ErrorCode;
				if (errorCode != 10013)
				{
					switch (errorCode)
					{
					case 10047:
					case 10049:
						if (bBindIPv6)
						{
							sSpecificErrorString = "An unsupported option was used. This often means that you've enabled IPv6 support inside Tools > Options, but your computer has IPv6 disabled.";
							goto IL_288;
						}
						goto IL_288;
					case 10048:
						break;
					default:
						goto IL_288;
					}
				}
				string sProcess = sProcessListeningOnPort;
				if (string.IsNullOrEmpty(sProcess))
				{
					sProcess = "use NETSTAT -AB at a command prompt to identify it.";
				}
				else
				{
					sProcess = "the process is '" + sProcess + "'.";
				}
				sSpecificErrorString = string.Format("Another service is using port {0}; {1}\n\n{2}", iPort, sProcess, string.Empty);
				sSpecificErrorTitle = "Port in Use";
				IL_288:
				this.oAcceptor = null;
				if (!string.IsNullOrEmpty(sSpecificErrorString))
				{
					sSpecificErrorString += "\n\n";
				}
				string format = "{2}Unable to bind to port [{0}]. ErrorCode: {1}.\n{3}\n\n{4}";
				object[] array2 = new object[5];
				array2[0] = iPort;
				array2[1] = eXS2.ErrorCode;
				array2[2] = sSpecificErrorString;
				array2[3] = eXS2.ToString();
				int num4 = 4;
				string[] array3 = new string[7];
				array3[0] = "Fiddler v";
				int num5 = 1;
				Version thisAssemblyVersion2 = Utilities.ThisAssemblyVersion;
				array3[num5] = ((thisAssemblyVersion2 != null) ? thisAssemblyVersion2.ToString() : null);
				array3[2] = " [.NET ";
				int num6 = 3;
				Version version2 = Environment.Version;
				array3[num6] = ((version2 != null) ? version2.ToString() : null);
				array3[4] = " on ";
				array3[5] = Environment.OSVersion.VersionString;
				array3[6] = "]";
				array2[num4] = string.Concat(array3);
				string message2 = string.Format(format, array2);
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[] { sSpecificErrorTitle, message2 });
				return false;
			}
			catch (Exception eX2)
			{
				this.oAcceptor = null;
				FiddlerApplication.Log.LogString(eX2.ToString());
				return false;
			}
			try
			{
				this.oAcceptor.BeginAccept(new AsyncCallback(this.AcceptConnection), null);
			}
			catch (Exception eX3)
			{
				this.oAcceptor = null;
				FiddlerApplication.Log.LogFormat("Fiddler BeginAccept() Exception: {0}", new object[] { eX3.Message });
				return false;
			}
			return true;
		}

		/// <summary>
		/// Dispose Fiddler's listening socket
		/// </summary>
		// Token: 0x06000388 RID: 904 RVA: 0x00021778 File Offset: 0x0001F978
		public void Dispose()
		{
			NetworkChange.NetworkAvailabilityChanged -= this.NetworkChange_NetworkAvailabilityChanged;
			NetworkChange.NetworkAddressChanged -= this.NetworkChange_NetworkAddressChanged;
			if (this.watcherPrefNotify != null)
			{
				FiddlerApplication.Prefs.RemoveWatcher(this.watcherPrefNotify.Value);
			}
			if (this.connectionsManager != null)
			{
				this.connectionsManager.ProxySettingsChanged -= new EventHandler<ProxySettingsChangedEventArgs>(this.ProxyRegistryKeysChanged);
			}
			this.Stop();
			if (this.oAutoProxy != null)
			{
				this.oAutoProxy.Dispose();
				this.oAutoProxy = null;
			}
		}

		/// <summary>
		/// Clear the pool of Server Pipes. May be called by extensions.
		/// </summary>
		// Token: 0x06000389 RID: 905 RVA: 0x0002180E File Offset: 0x0001FA0E
		public void PurgeServerPipePool()
		{
			Proxy.htServerPipePool.Clear();
		}

		/// <summary>
		/// Assign HTTPS Certificate for this endpoint
		/// </summary>
		/// <param name="certHTTPS">Certificate to return to clients who connect</param>
		// Token: 0x0600038A RID: 906 RVA: 0x0002181A File Offset: 0x0001FA1A
		public void AssignEndpointCertificate(X509Certificate2 certHTTPS)
		{
			this._oHTTPSCertificate = certHTTPS;
			if (certHTTPS != null)
			{
				this._sHTTPSHostname = certHTTPS.Subject;
				return;
			}
			this._sHTTPSHostname = null;
		}

		// Token: 0x0600038B RID: 907 RVA: 0x0002183C File Offset: 0x0001FA3C
		internal void RefreshUpstreamGatewayInformation()
		{
			this._ipepFtpGateway = (this._ipepHttpGateway = (this._ipepHttpsGateway = null));
			this.upstreamProxySettings = null;
			this.oBypassList = null;
			if (this.oAutoProxy != null)
			{
				this.oAutoProxy.Dispose();
				this.oAutoProxy = null;
			}
			switch (CONFIG.UpstreamGateway)
			{
			case GatewayType.None:
				FiddlerApplication.Log.LogString("Setting upstream gateway to none");
				return;
			case GatewayType.Manual:
			{
				string proxyServerString = FiddlerApplication.Prefs.GetStringPref("fiddler.network.gateway.proxies", string.Empty);
				ProxySettings proxySettings = new ProxySettings(false, false, string.Empty, FiddlerApplication.Prefs.GetStringPref("fiddler.network.gateway.exceptions", string.Empty), Proxy.GetProtocolProxyEnabled("http", proxyServerString), Proxy.GetProtocolProxyHost("http", proxyServerString), Proxy.GetProtocolProxyPort("http", proxyServerString), Proxy.GetProtocolProxyEnabled("https", proxyServerString), Proxy.GetProtocolProxyHost("https", proxyServerString), Proxy.GetProtocolProxyPort("https", proxyServerString), Proxy.GetProtocolProxyEnabled("ftp", proxyServerString), Proxy.GetProtocolProxyHost("ftp", proxyServerString), Proxy.GetProtocolProxyPort("ftp", proxyServerString), Proxy.GetProtocolProxyEnabled("socks", proxyServerString), Proxy.GetProtocolProxyHost("socks", proxyServerString), Proxy.GetProtocolProxyPort("socks", proxyServerString));
				this.AssignGateway(proxySettings);
				return;
			}
			case GatewayType.System:
			{
				ProxySettings proxySettings2 = this.GetDefaultConnectionUpstreamProxy();
				this.AssignGateway(proxySettings2);
				return;
			}
			case GatewayType.WPAD:
				FiddlerApplication.Log.LogString("Setting upstream gateway to WPAD");
				this.oAutoProxy = new AutoProxy(true, null);
				return;
			default:
				return;
			}
		}

		// Token: 0x0600038C RID: 908 RVA: 0x000219B0 File Offset: 0x0001FBB0
		internal ProxySettings GetDefaultConnectionUpstreamProxy()
		{
			Connectoids connectoids = this.oAllConnectoids;
			return (connectoids != null) ? connectoids.GetDefaultConnectionGatewayInfo(CONFIG.sHookConnectionNamespace, CONFIG.sHookConnectionNamed) : null;
		}

		// Token: 0x0600038D RID: 909 RVA: 0x000219DC File Offset: 0x0001FBDC
		private static bool GetProtocolProxyEnabled(string protocol, string proxyServerString)
		{
			string protocolProxyListing = Proxy.GetProtocolProxyListing(protocol, proxyServerString);
			return !string.IsNullOrEmpty(protocolProxyListing);
		}

		// Token: 0x0600038E RID: 910 RVA: 0x000219FC File Offset: 0x0001FBFC
		private static string GetProtocolProxyHost(string protocol, string proxyServerString)
		{
			string protocolProxyListing = Proxy.GetProtocolProxyListing(protocol, proxyServerString);
			if (string.IsNullOrEmpty(protocolProxyListing))
			{
				return null;
			}
			string protocolProxyHostWithPort = protocolProxyListing.Replace(protocol + "=", string.Empty);
			return Proxy.PortRegex.Replace(protocolProxyHostWithPort, string.Empty);
		}

		// Token: 0x0600038F RID: 911 RVA: 0x00021A44 File Offset: 0x0001FC44
		private static ushort GetProtocolProxyPort(string protocol, string proxyServerString)
		{
			string protocolProxyListing = Proxy.GetProtocolProxyListing(protocol, proxyServerString);
			if (string.IsNullOrEmpty(protocolProxyListing))
			{
				return 0;
			}
			Match portMatch = Proxy.PortRegex.Match(protocolProxyListing);
			if (portMatch.Success)
			{
				ushort port;
				ushort.TryParse(portMatch.Result("$1"), out port);
				return port;
			}
			if (protocol == "http")
			{
				return 80;
			}
			if (protocol == "https")
			{
				return 443;
			}
			if (protocol == "ftp")
			{
				return 21;
			}
			if (!(protocol == "socks"))
			{
				return 0;
			}
			return 1080;
		}

		// Token: 0x06000390 RID: 912 RVA: 0x00021AD8 File Offset: 0x0001FCD8
		private static string GetProtocolProxyListing(string protocol, string proxyServerString)
		{
			if (string.IsNullOrWhiteSpace(proxyServerString))
			{
				return null;
			}
			return (from psl in proxyServerString.Split(new char[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
				select psl.Trim() into psl
				orderby psl.Contains('=') descending
				select psl).FirstOrDefault((string psl) => !psl.Contains('=') || psl.StartsWith(protocol + "="));
		}

		/// <summary>
		/// Sets the upstream gateway to match the specified ProxySettings
		/// </summary>
		/// <param name="upstreamProxySettings"></param>
		// Token: 0x06000391 RID: 913 RVA: 0x00021B6C File Offset: 0x0001FD6C
		private void AssignGateway(ProxySettings upstreamProxySettings)
		{
			if (upstreamProxySettings == null)
			{
				this.upstreamProxySettings = new ProxySettings();
			}
			else
			{
				this.upstreamProxySettings = upstreamProxySettings;
				if (this.upstreamProxySettings.UseWebProxyAutoDiscovery || (this.upstreamProxySettings.ProxyAutoConfigEnabled && !string.IsNullOrWhiteSpace(this.upstreamProxySettings.ProxyAutoConfigUrl)))
				{
					this.oAutoProxy = new AutoProxy(this.upstreamProxySettings.UseWebProxyAutoDiscovery, this.upstreamProxySettings.ProxyAutoConfigUrl);
				}
			}
			this._DetermineGatewayIPEndPoints();
		}

		/// <summary>
		/// Generate or find a certificate for this endpoint
		/// </summary>
		/// <param name="sHTTPSHostname">Subject FQDN</param>
		/// <returns>TRUE if the certificate could be found/generated, false otherwise</returns>
		// Token: 0x06000392 RID: 914 RVA: 0x00021BEC File Offset: 0x0001FDEC
		internal bool ActAsHTTPSEndpointForHostname(string sHTTPSHostname)
		{
			try
			{
				if (string.IsNullOrEmpty(sHTTPSHostname))
				{
					throw new ArgumentException();
				}
				this._oHTTPSCertificate = CertMaker.FindCert(sHTTPSHostname);
				this._sHTTPSHostname = this._oHTTPSCertificate.Subject;
				return true;
			}
			catch (Exception eX)
			{
				this._oHTTPSCertificate = null;
				this._sHTTPSHostname = null;
			}
			return false;
		}

		/// <summary>
		/// Return a simple string indicating what upstream proxy/gateway is in use.
		/// </summary>
		/// <returns></returns>
		// Token: 0x06000393 RID: 915 RVA: 0x00021C4C File Offset: 0x0001FE4C
		internal string GetGatewayInformation()
		{
			if (FiddlerApplication.oProxy.oAutoProxy != null)
			{
				return string.Format("Gateway: Auto-Config\n{0}", FiddlerApplication.oProxy.oAutoProxy.ToString());
			}
			IPEndPoint ipepGateway = this.FindGatewayForOrigin("http", "fiddler2.com");
			if (ipepGateway != null)
			{
				return string.Format("Gateway: {0}:{1}\n", ipepGateway.Address.ToString(), ipepGateway.Port.ToString());
			}
			return string.Format("Gateway: No Gateway\n", Array.Empty<object>());
		}

		// Token: 0x0400019A RID: 410
		internal static string sUpstreamPACScript;

		// Token: 0x0400019B RID: 411
		private NetworkConnectionsManager connectionsManager;

		/// <summary>
		/// Hostname if this Proxy Endpoint is terminating HTTPS connections
		/// </summary>
		// Token: 0x0400019C RID: 412
		private string _sHTTPSHostname;

		/// <summary>
		/// Certificate if this Proxy Endpoint is terminating HTTPS connections
		/// </summary>
		// Token: 0x0400019D RID: 413
		private X509Certificate2 _oHTTPSCertificate;

		/// <summary>
		/// Per-connectoid information about each WinINET connectoid
		/// </summary>
		// Token: 0x0400019E RID: 414
		internal Connectoids oAllConnectoids;

		// Token: 0x0400019F RID: 415
		private ProxySettings fiddlerProxySettings;

		/// <summary>
		/// The upstream proxy settings.
		/// </summary>
		// Token: 0x040001A0 RID: 416
		private ProxySettings upstreamProxySettings;

		/// <summary>
		/// The AutoProxy object, created if we're using WPAD or a PAC Script as a gateway
		/// </summary>
		// Token: 0x040001A1 RID: 417
		private volatile AutoProxy oAutoProxy;

		// Token: 0x040001A2 RID: 418
		private IPEndPoint _ipepFtpGateway;

		// Token: 0x040001A3 RID: 419
		private IPEndPoint _ipepHttpGateway;

		// Token: 0x040001A4 RID: 420
		private IPEndPoint _ipepHttpsGateway;

		/// <summary>
		/// Allow binding to a specific egress adapter: "fiddler.network.egress.ip"
		/// </summary>
		// Token: 0x040001A5 RID: 421
		internal IPEndPoint _DefaultEgressEndPoint;

		/// <summary>
		/// Watcher for Notification of Preference changes
		/// </summary>
		// Token: 0x040001A6 RID: 422
		private PreferenceBag.PrefWatcher? watcherPrefNotify;

		/// <summary>
		/// Server connections may be pooled for performance reasons.
		/// </summary>
		// Token: 0x040001A7 RID: 423
		internal static PipePool htServerPipePool = new PipePool();

		/// <summary>
		/// The Socket Endpoint on which this proxy receives requests
		/// </summary>
		// Token: 0x040001A8 RID: 424
		private Socket oAcceptor;

		// Token: 0x040001A9 RID: 425
		[Obsolete]
		private bool _bIsAttached;

		/// <summary>
		/// Flag indicating that Fiddler is in the process of detaching...
		/// </summary>
		// Token: 0x040001AA RID: 426
		private bool _bDetaching;

		/// <summary>
		/// List of hosts which should bypass the upstream gateway
		/// </summary>
		// Token: 0x040001AB RID: 427
		private ProxyBypassList oBypassList;

		// Token: 0x040001AD RID: 429
		private static readonly Regex PortRegex = new Regex(":(\\d+)$");
	}
}
