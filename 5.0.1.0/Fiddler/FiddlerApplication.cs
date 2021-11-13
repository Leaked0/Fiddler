using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using FiddlerCore.PlatformExtensions;
using FiddlerCore.PlatformExtensions.API;
using FiddlerCore.SazProvider;
using FiddlerCore.Utilities;

namespace Fiddler
{
	/// <summary>
	/// This class acts as the central point for script/extensions to interact with Fiddler components.
	/// </summary>
	// Token: 0x02000036 RID: 54
	public static class FiddlerApplication
	{
		// Token: 0x17000061 RID: 97
		// (get) Token: 0x060001FA RID: 506 RVA: 0x0001520D File Offset: 0x0001340D
		// (set) Token: 0x060001FB RID: 507 RVA: 0x00015214 File Offset: 0x00013414
		public static ISAZProvider oSAZProvider
		{
			get
			{
				return FiddlerApplication.sazProvider;
			}
			set
			{
				FiddlerApplication.sazProvider = value;
			}
		}

		/// <summary>
		/// Fiddler's logging system
		/// </summary>
		// Token: 0x17000062 RID: 98
		// (get) Token: 0x060001FC RID: 508 RVA: 0x0001521C File Offset: 0x0001341C
		[CodeDescription("Fiddler's logging subsystem; displayed on the LOG tab by default.")]
		public static Logger Log
		{
			get
			{
				return FiddlerApplication._Log;
			}
		}

		/// <summary>
		/// Fiddler's Preferences collection. Learn more at http://fiddler.wikidot.com/prefs
		/// </summary>
		// Token: 0x17000063 RID: 99
		// (get) Token: 0x060001FD RID: 509 RVA: 0x00015223 File Offset: 0x00013423
		[CodeDescription("Fiddler's Preferences collection. http://fiddler.wikidot.com/prefs")]
		public static IFiddlerPreferences Prefs
		{
			get
			{
				return CONFIG.RawPrefs;
			}
		}

		/// <summary>
		/// Gets Fiddler* version info
		/// </summary>
		/// <returns>A string indicating the build/flavor of the Fiddler* assembly</returns>
		// Token: 0x060001FE RID: 510 RVA: 0x0001522C File Offset: 0x0001342C
		public static string GetVersionString()
		{
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
			string sExtraFeatures = string.Empty;
			string sSKU = "FiddlerCore";
			return string.Format("{0}/{1}.{2}.{3}.{4}{5}", new object[] { sSKU, fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart, sExtraFeatures });
		}

		/// <summary>
		/// Set the DisplayName for the application
		/// </summary>
		/// <param name="sAppName">1 to 64 character name to be displayed in error messages, etc</param>
		// Token: 0x060001FF RID: 511 RVA: 0x000152A5 File Offset: 0x000134A5
		public static void SetAppDisplayName(string sAppName)
		{
			if (string.IsNullOrEmpty(sAppName) || sAppName.Length > 64)
			{
				throw new ArgumentException("AppName must be 1 to 64 characters");
			}
		}

		/// <summary>
		/// By setting this property you can provide Telerik Fiddler Core with custom MIME-type-to-file-extension mappings.
		/// </summary>
		// Token: 0x17000064 RID: 100
		// (set) Token: 0x06000200 RID: 512 RVA: 0x000152C4 File Offset: 0x000134C4
		public static IEnumerable<MimeMap> CustomMimeMappings
		{
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value", "CustomMimeMappings cannot be null");
				}
				MimeMappingsProvider.Instance.LoadCustomMimeMappings(FiddlerApplication.Log, value);
			}
		}

		/// <summary>
		/// Fiddler's AutoResponder object.
		/// </summary>
		// Token: 0x17000065 RID: 101
		// (get) Token: 0x06000201 RID: 513 RVA: 0x000152E9 File Offset: 0x000134E9
		[CodeDescription("Fiddler's AutoResponder object.")]
		public static AutoResponder oAutoResponder
		{
			get
			{
				return FiddlerApplication._AutoResponder;
			}
		}

		/// <summary>
		/// This event fires when the user instructs Fiddler to clear the cache or cookies
		/// </summary>
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x06000202 RID: 514 RVA: 0x000152F0 File Offset: 0x000134F0
		// (remove) Token: 0x06000203 RID: 515 RVA: 0x00015324 File Offset: 0x00013524
		[CodeDescription("This event fires when the user instructs Fiddler to clear the cache or cookies.")]
		public static event EventHandler<CacheClearEventArgs> OnClearCache;

		/// <summary>
		/// This event fires each time FiddlerCore reads data from network for the server's response. Note that this data
		/// is not formatted in any way, and must be parsed by the recipient.
		/// </summary>
		// Token: 0x14000002 RID: 2
		// (add) Token: 0x06000204 RID: 516 RVA: 0x00015358 File Offset: 0x00013558
		// (remove) Token: 0x06000205 RID: 517 RVA: 0x0001538C File Offset: 0x0001358C
		public static event EventHandler<RawReadEventArgs> OnReadResponseBuffer;

		/// <summary>
		/// This event fires each time FiddlerCore reads data from network for the client's request. Note that this data
		/// is not formatted in any way, and must be parsed by the recipient.
		/// </summary>
		// Token: 0x14000003 RID: 3
		// (add) Token: 0x06000206 RID: 518 RVA: 0x000153C0 File Offset: 0x000135C0
		// (remove) Token: 0x06000207 RID: 519 RVA: 0x000153F4 File Offset: 0x000135F4
		public static event EventHandler<RawReadEventArgs> OnReadRequestBuffer;

		/// <summary>
		/// This event fires when a client request is received by Fiddler
		/// </summary>
		// Token: 0x14000004 RID: 4
		// (add) Token: 0x06000208 RID: 520 RVA: 0x00015428 File Offset: 0x00013628
		// (remove) Token: 0x06000209 RID: 521 RVA: 0x0001545C File Offset: 0x0001365C
		public static event SessionStateHandler BeforeRequest;

		/// <summary>
		/// This event fires when a server response is received by Fiddler
		/// </summary>
		// Token: 0x14000005 RID: 5
		// (add) Token: 0x0600020A RID: 522 RVA: 0x00015490 File Offset: 0x00013690
		// (remove) Token: 0x0600020B RID: 523 RVA: 0x000154C4 File Offset: 0x000136C4
		public static event SessionStateHandler BeforeResponse;

		/// <summary>
		/// This event fires when Request Headers are available
		/// </summary>
		// Token: 0x14000006 RID: 6
		// (add) Token: 0x0600020C RID: 524 RVA: 0x000154F8 File Offset: 0x000136F8
		// (remove) Token: 0x0600020D RID: 525 RVA: 0x0001552C File Offset: 0x0001372C
		public static event SessionStateHandler RequestHeadersAvailable;

		/// <summary>
		/// This event fires when Response Headers are available
		/// </summary>
		// Token: 0x14000007 RID: 7
		// (add) Token: 0x0600020E RID: 526 RVA: 0x00015560 File Offset: 0x00013760
		// (remove) Token: 0x0600020F RID: 527 RVA: 0x00015594 File Offset: 0x00013794
		public static event SessionStateHandler ResponseHeadersAvailable;

		/// <summary>
		/// This event fires when an error response is generated by Fiddler
		/// </summary>
		// Token: 0x14000008 RID: 8
		// (add) Token: 0x06000210 RID: 528 RVA: 0x000155C8 File Offset: 0x000137C8
		// (remove) Token: 0x06000211 RID: 529 RVA: 0x000155FC File Offset: 0x000137FC
		public static event SessionStateHandler BeforeReturningError;

		/// <summary>
		/// This event fires when Fiddler captures a WebSocket message
		/// </summary>
		// Token: 0x14000009 RID: 9
		// (add) Token: 0x06000212 RID: 530 RVA: 0x00015630 File Offset: 0x00013830
		// (remove) Token: 0x06000213 RID: 531 RVA: 0x00015664 File Offset: 0x00013864
		public static event EventHandler<WebSocketMessageEventArgs> OnWebSocketMessage;

		/// <summary>
		/// This event fires when a session has been completed
		/// </summary>
		// Token: 0x1400000A RID: 10
		// (add) Token: 0x06000214 RID: 532 RVA: 0x00015698 File Offset: 0x00013898
		// (remove) Token: 0x06000215 RID: 533 RVA: 0x000156CC File Offset: 0x000138CC
		public static event SessionStateHandler AfterSessionComplete;

		/// <summary>
		/// This event fires when Fiddler evaluates the validity of a server-provided certificate. Adjust the value of the ValidityState property if desired.
		/// </summary>
		// Token: 0x1400000B RID: 11
		// (add) Token: 0x06000216 RID: 534 RVA: 0x00015700 File Offset: 0x00013900
		// (remove) Token: 0x06000217 RID: 535 RVA: 0x00015734 File Offset: 0x00013934
		[CodeDescription("This event fires a HTTPS certificate is validated.")]
		public static event EventHandler<ValidateServerCertificateEventArgs> OnValidateServerCertificate;

		/// <summary>
		/// Sync this event to be notified when FiddlerCore has attached as the system proxy.")]
		/// </summary>
		// Token: 0x1400000C RID: 12
		// (add) Token: 0x06000218 RID: 536 RVA: 0x00015768 File Offset: 0x00013968
		// (remove) Token: 0x06000219 RID: 537 RVA: 0x0001579C File Offset: 0x0001399C
		[CodeDescription("Sync this event to be notified when FiddlerCore has attached as the system proxy.")]
		public static event SimpleEventHandler FiddlerAttach;

		/// <summary>
		/// Sync this event to be notified when FiddlerCore has detached as the system proxy.
		/// </summary>
		// Token: 0x1400000D RID: 13
		// (add) Token: 0x0600021A RID: 538 RVA: 0x000157D0 File Offset: 0x000139D0
		// (remove) Token: 0x0600021B RID: 539 RVA: 0x00015804 File Offset: 0x00013A04
		[CodeDescription("Sync this event to be notified when FiddlerCore has detached as the system proxy.")]
		public static event SimpleEventHandler FiddlerDetach;

		/// <summary>
		/// Checks if FiddlerCore is running.
		/// </summary>
		/// <returns>TRUE if FiddlerCore is started/listening; FALSE otherwise.</returns>
		// Token: 0x0600021C RID: 540 RVA: 0x00015837 File Offset: 0x00013A37
		public static bool IsStarted()
		{
			return FiddlerApplication.oProxy != null;
		}

		/// <summary>
		/// Checks if FiddlerCore is running and registered as the System Proxy.
		/// </summary>
		/// <returns>TRUE if FiddlerCore IsStarted AND registered as the system proxy; FALSE otherwise.</returns>
		// Token: 0x0600021D RID: 541 RVA: 0x00015841 File Offset: 0x00013A41
		public static bool IsSystemProxy()
		{
			return FiddlerApplication.oProxy != null && FiddlerApplication.oProxy.IsAttached;
		}

		/// <summary>
		/// Allow working with the connectoids info.
		/// E.g. when you need info for the system proxies before starting the Fiddler one.
		/// </summary>
		// Token: 0x0600021E RID: 542 RVA: 0x00015856 File Offset: 0x00013A56
		public static void InitConnectoids()
		{
			FiddlerApplication.oProxy = new Proxy(false, null);
			FiddlerApplication.oProxy.CollectConnectoidAndGatewayInfo(false);
		}

		/// <summary>
		/// Recommended way to Start FiddlerCore.
		/// </summary>
		/// <param name="startupSettings"><see cref="T:Fiddler.FiddlerCoreStartupSettings" /></param>
		// Token: 0x0600021F RID: 543 RVA: 0x00015870 File Offset: 0x00013A70
		public static void Startup(FiddlerCoreStartupSettings startupSettings)
		{
			if (FiddlerApplication.oProxy != null)
			{
				throw new InvalidOperationException("Calling startup twice without calling shutdown is not permitted.");
			}
			FiddlerApplication.AttachToPlatformExtensionsEvents();
			CONFIG.ListenPort = (int)startupSettings.ListenPort;
			CONFIG.bAllowRemoteConnections = startupSettings.AllowRemoteClients;
			CONFIG.DecryptHTTPS = startupSettings.DecryptSSL;
			CONFIG.bCaptureCONNECT = true;
			if (startupSettings.HookUsingPACFile)
			{
				FiddlerApplication.Prefs.SetBoolPref("fiddler.proxy.pacfile.usefileprotocol", false);
				CONFIG.HookWithPAC = true;
			}
			CONFIG.CaptureFTP = startupSettings.CaptureFTP;
			if (startupSettings.CaptureLocalhostTraffic && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				CONFIG.sHostsThatBypassFiddler = CONFIG.DontBypassLocalhost(CONFIG.sHostsThatBypassFiddler);
			}
			CONFIG.HookAllConnections = startupSettings.MonitorAllConnections;
			if (startupSettings.UpstreamProxySettings != null)
			{
				CONFIG.UpstreamGateway = GatewayType.Manual;
				FiddlerApplication.oProxy = new Proxy(true, startupSettings.UpstreamProxySettings);
				FiddlerApplication.SetUpstreamGatewayPreferences(startupSettings.UpstreamProxySettings.GetProxyServerString(), startupSettings.UpstreamProxySettings.BypassHosts);
				FiddlerApplication.oProxy.CollectConnectoidAndGatewayInfo(false);
				if (FiddlerApplication.oProxy.Start(CONFIG.ListenPort, CONFIG.bAllowRemoteConnections) && startupSettings.ListenPort == 0)
				{
					CONFIG.ListenPort = FiddlerApplication.oProxy.ListenPort;
				}
				return;
			}
			if (startupSettings.ChainToUpstreamGateway)
			{
				CONFIG.UpstreamGateway = GatewayType.System;
			}
			else if (startupSettings.UpstreamProxySettings != null)
			{
				FiddlerApplication.SetUpstreamGatewayPreferences(startupSettings.UpstreamProxySettings.GetProxyServerString(), startupSettings.UpstreamProxySettings.BypassHosts);
				CONFIG.UpstreamGateway = GatewayType.Manual;
			}
			else
			{
				CONFIG.UpstreamGateway = GatewayType.None;
			}
			FiddlerApplication.oProxy = new Proxy(true, null);
			if (FiddlerApplication.oProxy.Start(CONFIG.ListenPort, CONFIG.bAllowRemoteConnections))
			{
				if (startupSettings.ListenPort == 0)
				{
					CONFIG.ListenPort = FiddlerApplication.oProxy.ListenPort;
				}
				if (startupSettings.RegisterAsSystemProxy)
				{
					FiddlerApplication.oProxy.Attach(true);
					return;
				}
				if (startupSettings.ChainToUpstreamGateway)
				{
					FiddlerApplication.oProxy.CollectConnectoidAndGatewayInfo(true);
					return;
				}
				FiddlerApplication.oProxy.RefreshUpstreamGatewayInformation();
			}
		}

		// Token: 0x06000220 RID: 544 RVA: 0x00015A32 File Offset: 0x00013C32
		private static void SetUpstreamGatewayPreferences(string upstreamGateway, string bypassList)
		{
			FiddlerApplication.Prefs.SetStringPref("fiddler.network.gateway.proxies", upstreamGateway);
			FiddlerApplication.Prefs.SetStringPref("fiddler.network.gateway.exceptions", bypassList);
		}

		/// <summary>
		/// Start a new proxy endpoint instance, listening on the specified port
		/// </summary>
		/// <param name="iPort">The port to listen on</param>
		/// <param name="bAllowRemote">TRUE if remote clients should be permitted to connect to this endpoint</param>
		/// <param name="sHTTPSHostname">A Hostname (e.g. EXAMPLE.com) if this endpoint should be treated as a HTTPS Server</param>
		/// <returns>A Proxy object, or null if unsuccessful</returns>
		// Token: 0x06000221 RID: 545 RVA: 0x00015A54 File Offset: 0x00013C54
		public static Proxy CreateProxyEndpoint(int iPort, bool bAllowRemote, string sHTTPSHostname)
		{
			Proxy oNewProxy = new Proxy(false, null);
			if (!string.IsNullOrEmpty(sHTTPSHostname))
			{
				oNewProxy.ActAsHTTPSEndpointForHostname(sHTTPSHostname);
			}
			bool bSucceeded = oNewProxy.Start(iPort, bAllowRemote);
			if (bSucceeded)
			{
				return oNewProxy;
			}
			oNewProxy.Dispose();
			return null;
		}

		/// <summary>
		/// Start a new proxy endpoint instance, listening on the specified port
		/// </summary>
		/// <param name="iPort">The port to listen on</param>
		/// <param name="bAllowRemote">TRUE if remote clients should be permitted to connect to this endpoint</param>
		/// <param name="certHTTPS">A certificate to return when clients connect, or null</param>
		/// <returns>A Proxy object, or null if unsuccessful</returns>
		// Token: 0x06000222 RID: 546 RVA: 0x00015A90 File Offset: 0x00013C90
		public static Proxy CreateProxyEndpoint(int iPort, bool bAllowRemote, X509Certificate2 certHTTPS)
		{
			Proxy oNewProxy = new Proxy(false, null);
			if (certHTTPS != null)
			{
				oNewProxy.AssignEndpointCertificate(certHTTPS);
			}
			bool bSucceeded = oNewProxy.Start(iPort, bAllowRemote);
			if (bSucceeded)
			{
				return oNewProxy;
			}
			oNewProxy.Dispose();
			return null;
		}

		/// <summary>
		/// Shuts down the FiddlerCore proxy and disposes it. Note: If there's any traffic in progress while you're calling this method,
		/// your background threads are likely to blow up with ObjectDisposedExceptions or NullReferenceExceptions. In many cases, you're
		/// better off simply calling oProxy.Detach() and letting the garbage collector clean up when your program exits.
		/// </summary>
		// Token: 0x06000223 RID: 547 RVA: 0x00015AC4 File Offset: 0x00013CC4
		public static void Shutdown()
		{
			if (FiddlerApplication.oProxy != null)
			{
				FiddlerApplication.oProxy.Detach();
				FiddlerApplication.oProxy.Dispose();
				FiddlerApplication.oProxy = null;
				FiddlerApplication.DetachFromPlatformExtensionsEvents();
			}
		}

		/// <summary>
		/// Notify a listener that a block of a response was read.
		/// </summary>
		/// <param name="oS">The session for which the response is being read</param>
		/// <param name="arrBytes">byte buffer (not completely full)</param>
		/// <param name="cBytes">bytes set.</param>
		/// <returns>FALSE if AbortReading was set</returns>
		// Token: 0x06000224 RID: 548 RVA: 0x00015AF0 File Offset: 0x00013CF0
		internal static bool DoReadResponseBuffer(Session oS, byte[] arrBytes, int cBytes)
		{
			if (FiddlerApplication.OnReadResponseBuffer == null)
			{
				return true;
			}
			if (oS.isFlagSet(SessionFlags.Ignored))
			{
				return true;
			}
			RawReadEventArgs oRREA = new RawReadEventArgs(oS, arrBytes, cBytes);
			FiddlerApplication.OnReadResponseBuffer(oS, oRREA);
			return !oRREA.AbortReading;
		}

		/// <summary>
		/// Notify a listener that a block of a request was read. Note that this event may fire with overlapping blocks of data but
		/// different sessions if the client uses HTTP Pipelining.
		/// </summary>
		/// <param name="oS">The session for which the response is being read</param>
		/// <param name="arrBytes">byte buffer (not completely full)</param>
		/// <param name="cBytes">bytes set.</param>
		/// <returns>FALSE if AbortReading was set</returns>
		// Token: 0x06000225 RID: 549 RVA: 0x00015B30 File Offset: 0x00013D30
		internal static bool DoReadRequestBuffer(Session oS, byte[] arrBytes, int cBytes)
		{
			if (FiddlerApplication.OnReadRequestBuffer == null)
			{
				return true;
			}
			if (oS.isFlagSet(SessionFlags.Ignored))
			{
				return true;
			}
			RawReadEventArgs oRREA = new RawReadEventArgs(oS, arrBytes, cBytes);
			FiddlerApplication.OnReadRequestBuffer(oS, oRREA);
			return !oRREA.AbortReading;
		}

		// Token: 0x06000226 RID: 550 RVA: 0x00015B70 File Offset: 0x00013D70
		internal static bool DoClearCache(bool bClearFiles, bool bClearCookies)
		{
			EventHandler<CacheClearEventArgs> oToNotify = FiddlerApplication.OnClearCache;
			if (oToNotify == null)
			{
				return true;
			}
			CacheClearEventArgs oCCEA = new CacheClearEventArgs(bClearFiles, bClearCookies);
			oToNotify(null, oCCEA);
			return !oCCEA.Cancel;
		}

		// Token: 0x06000227 RID: 551 RVA: 0x00015BA4 File Offset: 0x00013DA4
		internal static void CheckOverrideCertificatePolicy(Session oS, string sExpectedCN, X509Certificate ServerCertificate, X509Chain ServerCertificateChain, SslPolicyErrors sslPolicyErrors, ref CertificateValidity oValidity)
		{
			EventHandler<ValidateServerCertificateEventArgs> oToNotify = FiddlerApplication.OnValidateServerCertificate;
			if (oToNotify == null)
			{
				return;
			}
			ValidateServerCertificateEventArgs oEA = new ValidateServerCertificateEventArgs(oS, sExpectedCN, ServerCertificate, ServerCertificateChain, sslPolicyErrors);
			oToNotify(oS, oEA);
			oValidity = oEA.ValidityState;
		}

		// Token: 0x06000228 RID: 552 RVA: 0x00015BD8 File Offset: 0x00013DD8
		internal static void DoBeforeRequest(Session oSession)
		{
			if (oSession.isFlagSet(SessionFlags.Ignored))
			{
				return;
			}
			if (FiddlerApplication.BeforeRequest != null)
			{
				FiddlerApplication.BeforeRequest(oSession);
			}
		}

		// Token: 0x06000229 RID: 553 RVA: 0x00015BF6 File Offset: 0x00013DF6
		internal static void DoBeforeResponse(Session oSession)
		{
			if (oSession.isFlagSet(SessionFlags.Ignored))
			{
				return;
			}
			if (FiddlerApplication.BeforeResponse != null)
			{
				FiddlerApplication.BeforeResponse(oSession);
			}
		}

		// Token: 0x0600022A RID: 554 RVA: 0x00015C14 File Offset: 0x00013E14
		internal static void DoResponseHeadersAvailable(Session oSession)
		{
			if (oSession.isFlagSet(SessionFlags.Ignored))
			{
				return;
			}
			if (FiddlerApplication.ResponseHeadersAvailable != null)
			{
				FiddlerApplication.ResponseHeadersAvailable(oSession);
			}
		}

		// Token: 0x0600022B RID: 555 RVA: 0x00015C32 File Offset: 0x00013E32
		internal static void DoRequestHeadersAvailable(Session oSession)
		{
			if (oSession.isFlagSet(SessionFlags.Ignored))
			{
				return;
			}
			if (FiddlerApplication.RequestHeadersAvailable != null)
			{
				FiddlerApplication.RequestHeadersAvailable(oSession);
			}
		}

		// Token: 0x0600022C RID: 556 RVA: 0x00015C50 File Offset: 0x00013E50
		internal static void DoOnWebSocketMessage(Session oS, WebSocketMessage oWSM)
		{
			if (oS.isFlagSet(SessionFlags.Ignored))
			{
				return;
			}
			EventHandler<WebSocketMessageEventArgs> oToNotify = FiddlerApplication.OnWebSocketMessage;
			if (oToNotify != null)
			{
				oToNotify(oS, new WebSocketMessageEventArgs(oWSM));
			}
		}

		// Token: 0x0600022D RID: 557 RVA: 0x00015C7D File Offset: 0x00013E7D
		internal static void DoBeforeReturningError(Session oSession)
		{
			if (oSession.isFlagSet(SessionFlags.Ignored))
			{
				return;
			}
			if (FiddlerApplication.BeforeReturningError != null)
			{
				FiddlerApplication.BeforeReturningError(oSession);
			}
		}

		// Token: 0x0600022E RID: 558 RVA: 0x00015C9B File Offset: 0x00013E9B
		internal static void DoAfterSessionComplete(Session oSession)
		{
			if (oSession.isFlagSet(SessionFlags.Ignored))
			{
				return;
			}
			if (FiddlerApplication.AfterSessionComplete != null)
			{
				FiddlerApplication.AfterSessionComplete(oSession);
			}
		}

		// Token: 0x0600022F RID: 559 RVA: 0x00015CB9 File Offset: 0x00013EB9
		internal static void OnFiddlerAttach()
		{
			if (FiddlerApplication.FiddlerAttach != null)
			{
				FiddlerApplication.FiddlerAttach();
			}
		}

		// Token: 0x06000230 RID: 560 RVA: 0x00015CCC File Offset: 0x00013ECC
		internal static void OnFiddlerDetach()
		{
			if (FiddlerApplication.FiddlerDetach != null)
			{
				FiddlerApplication.FiddlerDetach();
			}
		}

		/// <summary>
		/// Export Sessions in the specified format
		/// </summary>
		/// <param name="sExportFormat">Shortname of desired format</param>
		/// <param name="oSessions">Sessions to export</param>
		/// <param name="dictOptions">Options to pass to the ISessionExport interface</param>
		/// <param name="ehPCEA">Your callback event handler, or NULL to allow Fiddler to handle</param>
		/// <returns>TRUE if successful, FALSE if desired format doesn't exist or other error occurs</returns>
		// Token: 0x06000231 RID: 561 RVA: 0x00015CE0 File Offset: 0x00013EE0
		public static bool DoExport(string sExportFormat, Session[] oSessions, Dictionary<string, object> dictOptions, EventHandler<ProgressCallbackEventArgs> ehPCEA)
		{
			if (string.IsNullOrEmpty(sExportFormat))
			{
				return false;
			}
			TranscoderTuple ttExport = FiddlerApplication.oTranscoders.GetExporter(sExportFormat);
			if (ttExport == null)
			{
				FiddlerApplication.Log.LogFormat("No exporter for the format '{0}' was available.", new object[] { sExportFormat });
				return false;
			}
			bool bResult = false;
			try
			{
				ISessionExporter oseExporter = (ISessionExporter)Activator.CreateInstance(ttExport.typeFormatter);
				if (ehPCEA == null)
				{
					ehPCEA = delegate(object sender, ProgressCallbackEventArgs oPCE)
					{
						string sCompletePercent = ((oPCE.PercentComplete > 0) ? ("Export is " + oPCE.PercentComplete.ToString() + "% complete; ") : string.Empty);
						FiddlerApplication.Log.LogFormat("{0}{1}", new object[] { sCompletePercent, oPCE.ProgressText });
					};
				}
				bResult = oseExporter.ExportSessions(sExportFormat, oSessions, dictOptions, ehPCEA);
				oseExporter.Dispose();
			}
			catch (Exception eX)
			{
				FiddlerApplication.LogAddonException(eX, "Exporter for " + sExportFormat + " failed.");
				bResult = false;
			}
			return bResult;
		}

		/// <summary>
		/// Calls a Fiddler Session Importer and returns the list of loaded Sessions.
		/// </summary>
		/// <param name="sImportFormat">String naming the Import format, e.g. HTTPArchive</param>
		/// <param name="bAddToSessionList">Should sessions be added to WebSessions list? (Not meaningful for FiddlerCore)</param>
		/// <param name="dictOptions">Dictionary of Options to pass to the Transcoder</param>
		/// <param name="ehPCEA">Your callback event handler, or NULL to allow Fiddler to handle</param>
		/// <param name="passwordCallback">Callback that is used to request passwords from the host if needed</param>
		/// <returns>Loaded Session[], or null on Failure</returns>
		// Token: 0x06000232 RID: 562 RVA: 0x00015D98 File Offset: 0x00013F98
		public static Session[] DoImport(string sImportFormat, bool bAddToSessionList, Dictionary<string, object> dictOptions, EventHandler<ProgressCallbackEventArgs> ehPCEA, GetPasswordDelegate passwordCallback = null, bool skipNewSessionEvent = false)
		{
			if (string.IsNullOrEmpty(sImportFormat))
			{
				return null;
			}
			TranscoderTuple ttImport = FiddlerApplication.oTranscoders.GetImporter(sImportFormat);
			if (ttImport == null)
			{
				return null;
			}
			Session[] oSessions = null;
			try
			{
				ISessionImporter oseImporter = (ISessionImporter)Activator.CreateInstance(ttImport.typeFormatter);
				if (ehPCEA == null)
				{
					ehPCEA = delegate(object sender, ProgressCallbackEventArgs oPCE)
					{
						string sCompletePercent = ((oPCE.PercentComplete > 0) ? ("Import is " + oPCE.PercentComplete.ToString() + "% complete; ") : string.Empty);
						FiddlerApplication.Log.LogFormat("{0}{1}", new object[] { sCompletePercent, oPCE.ProgressText });
					};
				}
				bool alreadyImported = false;
				IPasswordProtectedSessionImporter passwordProtectedSessionImporter = oseImporter as IPasswordProtectedSessionImporter;
				if (passwordProtectedSessionImporter != null)
				{
					oSessions = passwordProtectedSessionImporter.ImportSessions(sImportFormat, dictOptions, ehPCEA, passwordCallback, skipNewSessionEvent);
					alreadyImported = true;
				}
				if (!alreadyImported)
				{
					oSessions = oseImporter.ImportSessions(sImportFormat, dictOptions, ehPCEA, skipNewSessionEvent);
				}
				oseImporter.Dispose();
				if (oSessions == null)
				{
					return null;
				}
				foreach (Session session in oSessions)
				{
					session.SetBitFlag(SessionFlags.ImportedFromOtherTool, true);
					if (session.HTTPMethodIs("CONNECT"))
					{
						session.isTunnel = true;
					}
					if (session.id == 0)
					{
						session._AssignID();
					}
					if (!skipNewSessionEvent)
					{
						session.RaiseSessionFieldChanged();
					}
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.LogAddonException(eX, "Importer for " + sImportFormat + " failed.");
				oSessions = null;
			}
			return oSessions;
		}

		/// <summary>
		/// Reset the SessionID counter to 0. This method can lead to confusing UI, so call sparingly.
		/// </summary>
		// Token: 0x06000233 RID: 563 RVA: 0x00015EC4 File Offset: 0x000140C4
		[CodeDescription("Reset the SessionID counter to 0. This method can lead to confusing UI, so call sparingly.")]
		public static void ResetSessionCounter()
		{
			Session.ResetSessionCounter();
		}

		/// <summary>
		/// Show the user a message when an HTTP Error was encountered
		/// </summary>
		/// <param name="oSession">Session with error</param>
		/// <param name="bPoisonClientConnection">Set to true to prevent pooling/reuse of client connection</param>
		/// <param name="flagViolation">The SessionFlag which should be set to log this violation</param>
		/// <param name="bPoisonServerConnection">Set to true to prevent pooling/reuse of server connection</param>
		/// <param name="sMessage">Information about the problem</param>
		// Token: 0x06000234 RID: 564 RVA: 0x00015ECC File Offset: 0x000140CC
		internal static void HandleHTTPError(Session oSession, SessionFlags flagViolation, bool bPoisonClientConnection, bool bPoisonServerConnection, string sMessage)
		{
			oSession.EnsureID();
			if (bPoisonClientConnection)
			{
				oSession.PoisonClientPipe();
			}
			if (bPoisonServerConnection)
			{
				oSession.PoisonServerPipe();
			}
			oSession.SetBitFlag(flagViolation, true);
			oSession["ui-backcolor"] = "LightYellow";
			FiddlerApplication.Log.LogFormat("{0} - [#{1}] {2}", new object[]
			{
				"Fiddler.Network.ProtocolViolation",
				oSession.id.ToString(),
				sMessage
			});
			sMessage = "[ProtocolViolation] " + sMessage;
			if (oSession["x-HTTPProtocol-Violation"] == null || !oSession["x-HTTPProtocol-Violation"].Contains(sMessage))
			{
				oSession["x-HTTPProtocol-Violation"] = oSession["x-HTTPProtocol-Violation"] + sMessage;
			}
		}

		// Token: 0x06000235 RID: 565 RVA: 0x00015F8A File Offset: 0x0001418A
		internal static void DebugSpew(string sMessage)
		{
			bool bDebugSpew = CONFIG.bDebugSpew;
		}

		// Token: 0x06000236 RID: 566 RVA: 0x00015F92 File Offset: 0x00014192
		internal static void DebugSpew(string sMessage, params object[] args)
		{
			bool bDebugSpew = CONFIG.bDebugSpew;
		}

		// Token: 0x06000237 RID: 567 RVA: 0x00015F9C File Offset: 0x0001419C
		static FiddlerApplication()
		{
			FiddlerApplication._SetXceedLicenseKeys();
			try
			{
				Process oMe = Process.GetCurrentProcess();
				FiddlerApplication.iPID = oMe.Id;
				FiddlerApplication.sProcessInfo = string.Format("{0}:{1}", oMe.ProcessName.ToLower(), FiddlerApplication.iPID);
				oMe.Dispose();
			}
			catch (Exception eX)
			{
			}
			FiddlerApplication.platformExtensions = PlatformExtensionsFactory.Instance.CreatePlatformExtensions();
		}

		/// <summary>
		/// We really don't want this method to get inlined, because that would cause the Xceed DLLs to get loaded in the Main() function instead
		/// of when _SetXceedLicenseKeys is called; that, in turn, would delay the SplashScreen.
		/// </summary>
		// Token: 0x06000238 RID: 568 RVA: 0x0001604C File Offset: 0x0001424C
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static void _SetXceedLicenseKeys()
		{
		}

		/// <summary>
		/// Used to track errors with addons.
		/// </summary>
		/// <param name="eX"></param>
		/// <param name="sTitle"></param>
		// Token: 0x06000239 RID: 569 RVA: 0x00016050 File Offset: 0x00014250
		internal static void LogAddonException(Exception eX, string sTitle)
		{
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.debug.extensions.showerrors", false) || FiddlerApplication.Prefs.GetBoolPref("fiddler.debug.extensions.verbose", false))
			{
				FiddlerApplication.Log.LogFormat("!Exception from Extension: {0}", new object[] { Utilities.DescribeException(eX) });
			}
		}

		/// <summary>
		/// Record that a temporary file was created and handed to an external tool. We'll do our best to delete this file on exit.
		/// </summary>
		/// <param name="sTempFile">The filename of the file to be deleted</param>
		// Token: 0x0600023A RID: 570 RVA: 0x000160A0 File Offset: 0x000142A0
		public static void LogLeakedFile(string sTempFile)
		{
			List<string> obj = FiddlerApplication.slLeakedFiles;
			lock (obj)
			{
				FiddlerApplication.slLeakedFiles.Add(sTempFile);
			}
		}

		/// <summary>
		/// Clean up any Temporary files that were created
		/// </summary>
		// Token: 0x0600023B RID: 571 RVA: 0x000160E4 File Offset: 0x000142E4
		internal static void WipeLeakedFiles()
		{
			try
			{
				if (FiddlerApplication.slLeakedFiles.Count >= 1)
				{
					List<string> obj = FiddlerApplication.slLeakedFiles;
					lock (obj)
					{
						foreach (string sFilename in FiddlerApplication.slLeakedFiles)
						{
							try
							{
								File.Delete(sFilename);
							}
							catch (Exception eX)
							{
							}
						}
						FiddlerApplication.slLeakedFiles.Clear();
					}
				}
			}
			catch (Exception eX2)
			{
			}
		}

		/// <summary>
		/// Fired each time Fiddler successfully establishes a TCP/IP connection
		/// </summary>
		// Token: 0x1400000E RID: 14
		// (add) Token: 0x0600023C RID: 572 RVA: 0x0001619C File Offset: 0x0001439C
		// (remove) Token: 0x0600023D RID: 573 RVA: 0x000161D0 File Offset: 0x000143D0
		public static event EventHandler<ConnectionEventArgs> AfterSocketConnect;

		/// <summary>
		/// Fired each time Fiddler successfully accepts a TCP/IP connection
		/// </summary>
		// Token: 0x1400000F RID: 15
		// (add) Token: 0x0600023E RID: 574 RVA: 0x00016204 File Offset: 0x00014404
		// (remove) Token: 0x0600023F RID: 575 RVA: 0x00016238 File Offset: 0x00014438
		public static event EventHandler<ConnectionEventArgs> AfterSocketAccept;

		// Token: 0x06000240 RID: 576 RVA: 0x0001626C File Offset: 0x0001446C
		internal static void DoAfterSocketConnect(Session oSession, Socket sockServer)
		{
			EventHandler<ConnectionEventArgs> oHandler = FiddlerApplication.AfterSocketConnect;
			if (oHandler == null)
			{
				return;
			}
			ConnectionEventArgs oEA = new ConnectionEventArgs(oSession, sockServer);
			oHandler(oSession, oEA);
		}

		// Token: 0x06000241 RID: 577 RVA: 0x00016294 File Offset: 0x00014494
		internal static void DoAfterSocketAccept(Session oSession, Socket sockClient)
		{
			EventHandler<ConnectionEventArgs> oHandler = FiddlerApplication.AfterSocketAccept;
			if (oHandler == null)
			{
				return;
			}
			ConnectionEventArgs oEA = new ConnectionEventArgs(oSession, sockClient);
			oHandler(oSession, oEA);
		}

		/// <summary>
		/// Does this Fiddler instance support the specified feature?
		/// </summary>
		/// <param name="sFeatureName">Feature name (e.g. "bzip2")</param>
		/// <returns>TRUE if the specified feature is supported; false otherwise</returns>
		// Token: 0x06000242 RID: 578 RVA: 0x000162BB File Offset: 0x000144BB
		public static bool Supports(string sFeatureName)
		{
			if (sFeatureName == "bzip2")
			{
				return false;
			}
			if (!(sFeatureName == "xpress"))
			{
				return sFeatureName == "br";
			}
			return Utilities.IsWin8OrLater();
		}

		// Token: 0x06000243 RID: 579 RVA: 0x000162F4 File Offset: 0x000144F4
		internal static void AttachToPlatformExtensionsEvents()
		{
			FiddlerApplication.platformExtensions.DebugSpew += FiddlerApplication.OnDebugSpew;
			FiddlerApplication.platformExtensions.Error += FiddlerApplication.OnError;
			FiddlerApplication.platformExtensions.Log += FiddlerApplication.OnLog;
		}

		// Token: 0x06000244 RID: 580 RVA: 0x00016344 File Offset: 0x00014544
		internal static void DetachFromPlatformExtensionsEvents()
		{
			FiddlerApplication.platformExtensions.DebugSpew -= FiddlerApplication.OnDebugSpew;
			FiddlerApplication.platformExtensions.Error -= FiddlerApplication.OnError;
			FiddlerApplication.platformExtensions.Log -= FiddlerApplication.OnLog;
		}

		// Token: 0x06000245 RID: 581 RVA: 0x00016393 File Offset: 0x00014593
		private static void OnDebugSpew(object sender, MessageEventArgs args)
		{
			FiddlerApplication.DebugSpew(args.Message);
		}

		// Token: 0x06000246 RID: 582 RVA: 0x000163A0 File Offset: 0x000145A0
		private static void OnError(object sender, MessageEventArgs args)
		{
			FiddlerApplication.Log.LogString(args.Message);
		}

		// Token: 0x06000247 RID: 583 RVA: 0x000163B2 File Offset: 0x000145B2
		private static void OnLog(object sender, MessageEventArgs args)
		{
			FiddlerApplication.Log.LogString(args.Message);
		}

		/// <summary>
		/// TRUE if Fiddler is currently shutting down. Suspend all work that won't have side-effects.
		/// </summary>
		// Token: 0x040000E1 RID: 225
		public static bool isClosing;

		/// <summary>
		/// The default certificate used for client authentication
		/// </summary>
		// Token: 0x040000E2 RID: 226
		public static X509Certificate oDefaultClientCertificate;

		// Token: 0x040000E3 RID: 227
		public static LocalCertificateSelectionCallback ClientCertificateProvider;

		// Token: 0x040000E4 RID: 228
		private static ISAZProvider sazProvider = new SazProvider();

		// Token: 0x040000E5 RID: 229
		internal static readonly Logger _Log = new Logger();

		/// <summary>
		/// Fiddler's "Janitor" clears up unneeded resources (e.g. server sockets, DNS entries)
		/// </summary>
		// Token: 0x040000E6 RID: 230
		internal static readonly PeriodicWorker Janitor = new PeriodicWorker();

		/// <summary>
		/// Fiddler's core proxy object.
		/// </summary>
		// Token: 0x040000E7 RID: 231
		[CodeDescription("Fiddler's core proxy engine.")]
		public static Proxy oProxy;

		// Token: 0x040000E8 RID: 232
		internal static AutoResponder _AutoResponder = new AutoResponder();

		/// <summary>
		/// Fiddler Import/Export Transcoders
		/// </summary>
		// Token: 0x040000E9 RID: 233
		public static FiddlerTranscoders oTranscoders = new FiddlerTranscoders();

		/// <summary>
		/// List of "leaked" temporary files to be deleted as Fiddler exits.
		/// </summary>
		// Token: 0x040000F7 RID: 247
		private static readonly List<string> slLeakedFiles = new List<string>();

		/// <summary>
		/// Process ID of this Fiddler instance
		/// </summary>
		// Token: 0x040000F8 RID: 248
		internal static readonly int iPID;

		/// <summary>
		/// processname:PID of Fiddler
		/// </summary>
		// Token: 0x040000F9 RID: 249
		internal static readonly string sProcessInfo;

		// Token: 0x040000FA RID: 250
		private static IPlatformExtensions platformExtensions;
	}
}
