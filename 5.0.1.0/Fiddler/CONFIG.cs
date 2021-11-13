using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using FiddlerCore.Utilities;

namespace Fiddler
{
	/// <summary>
	/// The CONFIG object is Fiddler's legacy settings object, introduced before the advent of the Preferences system.
	/// </summary>
	// Token: 0x0200001A RID: 26
	public static class CONFIG
	{
		/// <summary>
		/// Generally, callers should use FiddlerApplication.Prefs, but RawPrefs allows use of the PreferenceBag members that
		/// are not a part of IFiddlerPreferences
		/// </summary>
		// Token: 0x17000035 RID: 53
		// (get) Token: 0x0600011F RID: 287 RVA: 0x00010BFF File Offset: 0x0000EDFF
		internal static PreferenceBag RawPrefs
		{
			get
			{
				return CONFIG._Prefs;
			}
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x06000120 RID: 288 RVA: 0x00010C06 File Offset: 0x0000EE06
		// (set) Token: 0x06000121 RID: 289 RVA: 0x00010C0D File Offset: 0x0000EE0D
		internal static List<int> InstrumentedBrowserProcessIDs
		{
			get
			{
				return CONFIG._instrumentedBrowserProcessIDs;
			}
			set
			{
				CONFIG._instrumentedBrowserProcessIDs = value;
			}
		}

		/// <summary>
		/// Control which processes have HTTPS traffic decryption enabled
		/// </summary>
		// Token: 0x17000037 RID: 55
		// (get) Token: 0x06000122 RID: 290 RVA: 0x00010C15 File Offset: 0x0000EE15
		// (set) Token: 0x06000123 RID: 291 RVA: 0x00010C1C File Offset: 0x0000EE1C
		public static ProcessFilterCategories DecryptWhichProcesses
		{
			get
			{
				return CONFIG._pfcDecyptFilter;
			}
			set
			{
				CONFIG._pfcDecyptFilter = value;
			}
		}

		/// <summary>
		/// Controls whether Fiddler should attempt to decrypt HTTPS Traffic
		/// </summary>
		// Token: 0x17000038 RID: 56
		// (get) Token: 0x06000124 RID: 292 RVA: 0x00010C24 File Offset: 0x0000EE24
		// (set) Token: 0x06000125 RID: 293 RVA: 0x00010C2B File Offset: 0x0000EE2B
		public static bool DecryptHTTPS
		{
			get
			{
				return CONFIG.bMITM_HTTPS;
			}
			set
			{
				CONFIG.bMITM_HTTPS = value;
			}
		}

		/// <summary>
		/// Should Audio/Video types automatically stream by default?
		/// </summary>
		// Token: 0x17000039 RID: 57
		// (get) Token: 0x06000126 RID: 294 RVA: 0x00010C33 File Offset: 0x0000EE33
		// (set) Token: 0x06000127 RID: 295 RVA: 0x00010C3A File Offset: 0x0000EE3A
		public static bool StreamAudioVideo
		{
			get
			{
				return CONFIG.bStreamAudioVideo;
			}
			set
			{
				CONFIG.bStreamAudioVideo = value;
			}
		}

		/// <summary>
		/// Gets a value indicating what mechanism, if any, will be used to find the upstream proxy/gateway.
		/// </summary>
		// Token: 0x1700003A RID: 58
		// (get) Token: 0x06000128 RID: 296 RVA: 0x00010C42 File Offset: 0x0000EE42
		// (set) Token: 0x06000129 RID: 297 RVA: 0x00010C49 File Offset: 0x0000EE49
		[Obsolete("Use Telerik.NetworkConnections.NetworkConnectionsManager.GetCurrentProxySettingsForConnection(*) to get information for the current proxy settings.")]
		public static GatewayType UpstreamGateway
		{
			get
			{
				return CONFIG._UpstreamGateway;
			}
			internal set
			{
				if (value < GatewayType.None || value > GatewayType.WPAD)
				{
					value = GatewayType.System;
				}
				CONFIG._UpstreamGateway = value;
				CONFIG.bForwardToGateway = value > GatewayType.None;
			}
		}

		/// <summary>
		/// Controls whether Fiddler will reuse server connections for multiple sessions
		/// </summary>
		// Token: 0x1700003B RID: 59
		// (get) Token: 0x0600012A RID: 298 RVA: 0x00010C65 File Offset: 0x0000EE65
		// (set) Token: 0x0600012B RID: 299 RVA: 0x00010C6C File Offset: 0x0000EE6C
		public static bool ReuseServerSockets
		{
			get
			{
				return CONFIG.bReuseServerSockets;
			}
			set
			{
				CONFIG.bReuseServerSockets = value;
			}
		}

		/// <summary>
		/// Controls whether Fiddler will reuse client connections for multiple sessions
		/// </summary>
		// Token: 0x1700003C RID: 60
		// (get) Token: 0x0600012C RID: 300 RVA: 0x00010C74 File Offset: 0x0000EE74
		// (set) Token: 0x0600012D RID: 301 RVA: 0x00010C7B File Offset: 0x0000EE7B
		public static bool ReuseClientSockets
		{
			get
			{
				return CONFIG.bReuseClientSockets;
			}
			set
			{
				CONFIG.bReuseClientSockets = value;
			}
		}

		/// <summary>
		/// Controls whether Fiddler should register as the FTP proxy
		/// </summary>
		// Token: 0x1700003D RID: 61
		// (get) Token: 0x0600012E RID: 302 RVA: 0x00010C83 File Offset: 0x0000EE83
		// (set) Token: 0x0600012F RID: 303 RVA: 0x00010C8A File Offset: 0x0000EE8A
		public static bool CaptureFTP
		{
			get
			{
				return CONFIG.bCaptureFTP;
			}
			set
			{
				CONFIG.bCaptureFTP = value;
			}
		}

		/// <summary>
		/// Controls whether Fiddler will attempt to connect to IPv6 addresses
		/// </summary>
		// Token: 0x1700003E RID: 62
		// (get) Token: 0x06000130 RID: 304 RVA: 0x00010C92 File Offset: 0x0000EE92
		// (set) Token: 0x06000131 RID: 305 RVA: 0x00010C99 File Offset: 0x0000EE99
		public static bool EnableIPv6
		{
			get
			{
				return CONFIG.bEnableIPv6;
			}
			set
			{
				CONFIG.bEnableIPv6 = value;
			}
		}

		// Token: 0x06000132 RID: 306 RVA: 0x00010CA1 File Offset: 0x0000EEA1
		private static string GetConnectionName()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return "GSettings";
			}
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return "Default";
			}
			return "DefaultLAN";
		}

		/// <summary>
		/// Name of connection to which Fiddler should autoattach if MonitorAllConnections is not set
		/// </summary>
		// Token: 0x1700003F RID: 63
		// (get) Token: 0x06000133 RID: 307 RVA: 0x00010CCC File Offset: 0x0000EECC
		// (set) Token: 0x06000134 RID: 308 RVA: 0x00010CD3 File Offset: 0x0000EED3
		[Obsolete("Use Telerik.NetworkConnections.NetworkConnectionsManager.GetCurrentProxySettingsForConnection(*)/SetProxySettingsForConnections(*) to get/set proxy settings for a network connection.")]
		public static string sHookConnectionNamed { get; set; } = CONFIG.GetConnectionName();

		// Token: 0x06000135 RID: 309 RVA: 0x00010CDB File Offset: 0x0000EEDB
		private static string GetConnectionNamespace()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return "Linux";
			}
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return "OSX";
			}
			return "WinINet";
		}

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x06000136 RID: 310 RVA: 0x00010D06 File Offset: 0x0000EF06
		// (set) Token: 0x06000137 RID: 311 RVA: 0x00010D0D File Offset: 0x0000EF0D
		[Obsolete("Use Telerik.NetworkConnections.NetworkConnectionsManager.GetAllConnectionFullNames() to get information for the current connections.")]
		public static string sHookConnectionNamespace { get; set; } = CONFIG.GetConnectionNamespace();

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x06000138 RID: 312 RVA: 0x00010D15 File Offset: 0x0000EF15
		// (set) Token: 0x06000139 RID: 313 RVA: 0x00010D1C File Offset: 0x0000EF1C
		internal static bool HookAllConnections
		{
			get
			{
				return CONFIG.bHookAllConnections;
			}
			set
			{
				CONFIG.bHookAllConnections = value;
			}
		}

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x0600013A RID: 314 RVA: 0x00010D24 File Offset: 0x0000EF24
		// (set) Token: 0x0600013B RID: 315 RVA: 0x00010D2B File Offset: 0x0000EF2B
		internal static bool HookWithPAC
		{
			get
			{
				return CONFIG.bHookWithPAC;
			}
			set
			{
				CONFIG.bHookWithPAC = value;
			}
		}

		/// <summary>
		/// Port to which Fiddler should forward inbound requests when configured to run as a Reverse Proxy
		/// </summary>
		// Token: 0x17000043 RID: 67
		// (get) Token: 0x0600013C RID: 316 RVA: 0x00010D33 File Offset: 0x0000EF33
		// (set) Token: 0x0600013D RID: 317 RVA: 0x00010D3A File Offset: 0x0000EF3A
		public static int iReverseProxyForPort
		{
			get
			{
				return CONFIG._iReverseProxyForPort;
			}
			set
			{
				if (value > -1 && value <= 65535 && value != CONFIG.m_ListenPort)
				{
					CONFIG._iReverseProxyForPort = value;
					return;
				}
				FiddlerApplication.Log.LogFormat("!Invalid configuration. ReverseProxyForPort may not be set to {0}", new object[] { value });
			}
		}

		/// <summary>
		/// On attach, will configure WinINET to bypass Fiddler for these hosts.
		/// </summary>
		// Token: 0x17000044 RID: 68
		// (get) Token: 0x0600013E RID: 318 RVA: 0x00010D75 File Offset: 0x0000EF75
		// (set) Token: 0x0600013F RID: 319 RVA: 0x00010D7C File Offset: 0x0000EF7C
		[Obsolete("Use Telerik.NetworkConnections.NetworkConnectionsManager.GetCurrentProxySettingsForConnection(*)/SetProxySettingsForConnections(*) to get/set ProxySettings.BypassHosts value for a network connection.")]
		public static string sHostsThatBypassFiddler
		{
			get
			{
				return CONFIG.m_sHostsThatBypassFiddler;
			}
			set
			{
				CONFIG.m_sHostsThatBypassFiddler = ((value == null) ? string.Empty : value);
			}
		}

		// Token: 0x06000140 RID: 320 RVA: 0x00010D90 File Offset: 0x0000EF90
		internal static string DontBypassLocalhost(string hosts)
		{
			if (!hosts.OICContains("<-loopback>") && !hosts.OICContains("<loopback>"))
			{
				hosts = string.Format("{0}{1}{2}", "<-loopback>", string.IsNullOrEmpty(hosts) ? string.Empty : ";", hosts);
			}
			return hosts;
		}

		// Token: 0x06000141 RID: 321 RVA: 0x00010DDE File Offset: 0x0000EFDE
		public static void SetNoDecryptList(string sNewList)
		{
			if (string.IsNullOrEmpty(sNewList))
			{
				CONFIG.oHLSkipDecryption = null;
				return;
			}
			CONFIG.oHLSkipDecryption = new HostList();
			CONFIG.oHLSkipDecryption.AssignFromString(sNewList);
		}

		// Token: 0x06000142 RID: 322 RVA: 0x00010E05 File Offset: 0x0000F005
		public static void SetNoDecryptListInvert(bool bInvert)
		{
			CONFIG.bHLSkipDecryptionInvert = bInvert;
		}

		/// <summary>
		/// Boolean indicating whether Fiddler will open the listening port exclusively
		/// </summary>
		// Token: 0x17000045 RID: 69
		// (get) Token: 0x06000143 RID: 323 RVA: 0x00010E0D File Offset: 0x0000F00D
		// (set) Token: 0x06000144 RID: 324 RVA: 0x00010E14 File Offset: 0x0000F014
		public static bool ForceExclusivePort
		{
			get
			{
				return CONFIG.m_bForceExclusivePort;
			}
			internal set
			{
				CONFIG.m_bForceExclusivePort = value;
			}
		}

		/// <summary>
		/// Controls whether server certificate errors are ignored when decrypting HTTPS traffic.
		/// </summary>
		// Token: 0x17000046 RID: 70
		// (get) Token: 0x06000145 RID: 325 RVA: 0x00010E1C File Offset: 0x0000F01C
		// (set) Token: 0x06000146 RID: 326 RVA: 0x00010E23 File Offset: 0x0000F023
		public static bool IgnoreServerCertErrors
		{
			get
			{
				return CONFIG.bIgnoreServerCertErrors;
			}
			set
			{
				CONFIG.bIgnoreServerCertErrors = value;
			}
		}

		/// <summary>
		/// The port upon which Fiddler is configured to listen.
		/// </summary>
		// Token: 0x17000047 RID: 71
		// (get) Token: 0x06000147 RID: 327 RVA: 0x00010E2B File Offset: 0x0000F02B
		// (set) Token: 0x06000148 RID: 328 RVA: 0x00010E32 File Offset: 0x0000F032
		public static int ListenPort
		{
			get
			{
				return CONFIG.m_ListenPort;
			}
			internal set
			{
				if (value >= 0 && value < 65536)
				{
					CONFIG.m_ListenPort = value;
					CONFIG.sFiddlerListenHostPort = Utilities.TrimAfter(CONFIG.sFiddlerListenHostPort, ':') + ":" + CONFIG.m_ListenPort.ToString();
				}
			}
		}

		/// <summary>
		/// Return a Special URL.
		/// </summary>
		/// <param name="sWhatUrl">String constant describing the URL to return. CASE-SENSITIVE!</param>
		/// <returns>Returns target URL</returns>
		// Token: 0x06000149 RID: 329 RVA: 0x00010E6C File Offset: 0x0000F06C
		[CodeDescription("Return a special Url.")]
		public static string GetUrl(string sWhatUrl)
		{
			uint num = <PrivateImplementationDetails>.ComputeStringHash(sWhatUrl);
			if (num <= 1473777120U)
			{
				if (num <= 652440633U)
				{
					if (num != 328308156U)
					{
						if (num == 652440633U)
						{
							if (sWhatUrl == "ShopAmazon")
							{
								return "http://www.fiddlerbook.com/r/?shop";
							}
						}
					}
					else if (sWhatUrl == "PrioritySupport")
					{
						return "http://www.telerik.com/purchase/fiddler";
					}
				}
				else if (num != 1324432130U)
				{
					if (num == 1473777120U)
					{
						if (sWhatUrl == "HelpContents")
						{
							return CONFIG.sRootUrl + "help/?ver=";
						}
					}
				}
				else if (sWhatUrl == "VerCheck")
				{
					return (FiddlerApplication.Prefs.GetBoolPref("fiddler.updater.UseHTTPS", Environment.OSVersion.Version.Major > 5) ? "https" : "http") + "://www.telerik.com/UpdateCheck.aspx?isBeta=";
				}
			}
			else if (num <= 1970641703U)
			{
				if (num != 1601318023U)
				{
					if (num == 1970641703U)
					{
						if (sWhatUrl == "AutoResponderHelp")
						{
							return CONFIG.sRootUrl + "help/AutoResponder.asp";
						}
					}
				}
				else if (sWhatUrl == "REDIR")
				{
					return "http://fiddler2.com/r/?";
				}
			}
			else if (num != 2943306507U)
			{
				if (num != 3376585647U)
				{
					if (num == 3432944473U)
					{
						if (sWhatUrl == "ChangeList")
						{
							return "http://www.telerik.com/support/whats-new/fiddler/release-history/fiddler-v2.x?";
						}
					}
				}
				else if (sWhatUrl == "FiltersHelp")
				{
					return CONFIG.sRootUrl + "help/Filters.asp";
				}
			}
			else if (sWhatUrl == "InstallLatest")
			{
				if (!CONFIG.bIsBeta)
				{
					return CONFIG.sSecureRootUrl + "r/?GetFiddler4";
				}
				return CONFIG.sSecureRootUrl + "r/?GetFiddler4Beta";
			}
			return CONFIG.sRootUrl;
		}

		// Token: 0x0600014A RID: 330 RVA: 0x00011071 File Offset: 0x0000F271
		public static string GetRedirUrl(string sKeyword)
		{
			return string.Format("{0}{1}", CONFIG.GetUrl("REDIR"), sKeyword);
		}

		/// <summary>
		/// Get a registry path for a named constant
		/// </summary>
		/// <param name="sWhatPath">The path to retrieve [Root, UI, Dynamic, Prefs]</param>
		/// <returns>The registry path</returns>
		// Token: 0x0600014B RID: 331 RVA: 0x00011088 File Offset: 0x0000F288
		public static string GetRegPath(string sWhatPath)
		{
			if (sWhatPath == "Root")
			{
				return CONFIG.sRootKey;
			}
			if (sWhatPath == "MenuExt")
			{
				return CONFIG.sRootKey + "MenuExt\\";
			}
			if (sWhatPath == "UI")
			{
				return CONFIG.sRootKey + "UI\\";
			}
			if (sWhatPath == "Dynamic")
			{
				return CONFIG.sRootKey + "Dynamic\\";
			}
			if (!(sWhatPath == "Prefs"))
			{
				return CONFIG.sRootKey;
			}
			return CONFIG.sRootKey + "Prefs\\";
		}

		/// <summary>
		/// Return an app path (ending in Path.DirectorySeparatorChar) or a filename
		/// </summary>
		/// <param name="sWhatPath">CASE-SENSITIVE</param>
		/// <returns>The specified filesystem path</returns>
		// Token: 0x0600014C RID: 332 RVA: 0x00011124 File Offset: 0x0000F324
		[CodeDescription("Return a filesystem path.")]
		public static string GetPath(string sWhatPath)
		{
			uint num = <PrivateImplementationDetails>.ComputeStringHash(sWhatPath);
			if (num <= 1725465591U)
			{
				if (num <= 748951495U)
				{
					if (num <= 608020060U)
					{
						if (num != 188393196U)
						{
							if (num != 348694444U)
							{
								if (num == 608020060U)
								{
									if (sWhatPath == "CustomMimeMappingsXmlFile")
									{
										return CONFIG.sUserPath + "CustomMimeMappings.xml";
									}
								}
							}
							else if (sWhatPath == "MyDocs")
							{
								string sFolder = "C:\\";
								try
								{
									sFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal, Environment.SpecialFolderOption.DoNotVerify);
								}
								catch (Exception e)
								{
									FiddlerApplication.Log.LogFormat("!!Initialization Error: Failed to retrieve path to your Documents folder.\nThis generally means you have a relative environment variable.\nDefaulting to {0}\n\n{1}", new object[] { sFolder, e.Message });
								}
								return sFolder;
							}
						}
						else if (sWhatPath == "Filters")
						{
							return CONFIG.sUserPath + "Filters" + Path.DirectorySeparatorChar.ToString();
						}
					}
					else if (num != 712005485U)
					{
						if (num != 714006897U)
						{
							if (num == 748951495U)
							{
								if (sWhatPath == "Inspectors")
								{
									return PathsHelper.RootDirectory + Path.DirectorySeparatorChar.ToString() + "Inspectors" + Path.DirectorySeparatorChar.ToString();
								}
							}
						}
						else if (sWhatPath == "TemplateResponses")
						{
							return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.templateresponses", PathsHelper.RootDirectory + Path.DirectorySeparatorChar.ToString() + "ResponseTemplates" + Path.DirectorySeparatorChar.ToString());
						}
					}
					else if (sWhatPath == "FiddlerRootCert")
					{
						return CONFIG.sUserPath + "DO_NOT_TRUST_FiddlerRoot.cer";
					}
				}
				else if (num <= 1028111172U)
				{
					if (num != 872777459U)
					{
						if (num != 905254486U)
						{
							if (num == 1028111172U)
							{
								if (sWhatPath == "AutoResponderDefaultRules")
								{
									return CONFIG.sUserPath + "AutoResponder.xml";
								}
							}
						}
						else if (sWhatPath == "Captures")
						{
							return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.captures", CONFIG.sUserPath + "Captures" + Path.DirectorySeparatorChar.ToString());
						}
					}
					else if (sWhatPath == "Requests")
					{
						return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.requests", string.Concat(new object[]
						{
							CONFIG.sUserPath,
							"Captures",
							Path.DirectorySeparatorChar,
							"Requests",
							Path.DirectorySeparatorChar
						}));
					}
				}
				else if (num <= 1211781061U)
				{
					if (num != 1097813392U)
					{
						if (num == 1211781061U)
						{
							if (sWhatPath == "MakeCert")
							{
								string sFolder = FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.makecert", PathsHelper.RootDirectory + Path.DirectorySeparatorChar.ToString() + "MakeCert.exe");
								if (!File.Exists(sFolder))
								{
									sFolder = "MakeCert.exe";
								}
								return sFolder;
							}
						}
					}
					else if (sWhatPath == "PerMachine-ISA-Config")
					{
						string sFolder = "C:\\";
						try
						{
							sFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
						}
						catch (Exception eX)
						{
						}
						return sFolder + "\\microsoft\\firewall client 2004\\management.ini";
					}
				}
				else if (num != 1411097086U)
				{
					if (num == 1725465591U)
					{
						if (sWhatPath == "Responses")
						{
							return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.responses", string.Concat(new object[]
							{
								CONFIG.sUserPath,
								"Captures",
								Path.DirectorySeparatorChar,
								"Responses",
								Path.DirectorySeparatorChar
							}));
						}
					}
				}
				else if (sWhatPath == "SafeTemp")
				{
					string sFolder = "C:\\";
					try
					{
						sFolder = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
						if (sFolder[sFolder.Length - 1] != Path.DirectorySeparatorChar)
						{
							sFolder += Path.DirectorySeparatorChar.ToString();
						}
					}
					catch (Exception e2)
					{
						string title = "GetPath(SafeTemp) Failed";
						string message = "Failed to retrieve path to your Internet Cache folder.\nThis generally means you have a relative environment variable.\nDefaulting to C:\\\n\n" + e2.Message;
						FiddlerApplication.Log.LogFormat("{0}: {1}", new object[] { title, message });
					}
					return sFolder;
				}
			}
			else if (num <= 3109746539U)
			{
				if (num <= 2499909372U)
				{
					if (num != 2180324044U)
					{
						if (num != 2237507437U)
						{
							if (num == 2499909372U)
							{
								if (sWhatPath == "Tools")
								{
									return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.Tools", PathsHelper.RootDirectory + Path.DirectorySeparatorChar.ToString() + "Tools" + Path.DirectorySeparatorChar.ToString());
								}
							}
						}
						else if (sWhatPath == "Transcoders_Machine")
						{
							return PathsHelper.RootDirectory + Path.DirectorySeparatorChar.ToString() + "ImportExport" + Path.DirectorySeparatorChar.ToString();
						}
					}
					else if (sWhatPath == "App")
					{
						return PathsHelper.RootDirectory + Path.DirectorySeparatorChar.ToString();
					}
				}
				else if (num <= 2728932643U)
				{
					if (num != 2703841893U)
					{
						if (num == 2728932643U)
						{
							if (sWhatPath == "AutoFiddlers_User")
							{
								return CONFIG.sUserPath + "Scripts" + Path.DirectorySeparatorChar.ToString();
							}
						}
					}
					else if (sWhatPath == "Root")
					{
						return CONFIG.sUserPath;
					}
				}
				else if (num != 2870502859U)
				{
					if (num == 3109746539U)
					{
						if (sWhatPath == "Pac")
						{
							return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.pac", string.Concat(new object[]
							{
								CONFIG.sUserPath,
								"Scripts",
								Path.DirectorySeparatorChar,
								"BrowserPAC.js"
							}));
						}
					}
				}
				else if (sWhatPath == "Inspectors_User")
				{
					return CONFIG.sUserPath + "Inspectors" + Path.DirectorySeparatorChar.ToString();
				}
			}
			else if (num <= 3475885944U)
			{
				if (num != 3139112171U)
				{
					if (num != 3421695547U)
					{
						if (num == 3475885944U)
						{
							if (sWhatPath == "DefaultClientCertificate")
							{
								return FiddlerApplication.Prefs.GetStringPref("fiddler.config.path.defaultclientcert", CONFIG.sUserPath + "ClientCertificate.cer");
							}
						}
					}
					else if (sWhatPath == "Scripts")
					{
						return CONFIG.sUserPath + "Scripts" + Path.DirectorySeparatorChar.ToString();
					}
				}
				else if (sWhatPath == "FilterNowRulesXmlFile")
				{
					return CONFIG.sUserPath + "FilterNowRules.xml";
				}
			}
			else if (num <= 3528574080U)
			{
				if (num != 3526810661U)
				{
					if (num == 3528574080U)
					{
						if (sWhatPath == "DefaultScriptEditor")
						{
							return CONFIG.GetPath("App") + "ScriptEditor" + Path.DirectorySeparatorChar.ToString() + "FSE2.exe";
						}
					}
				}
				else if (sWhatPath == "AutoFiddlers_Machine")
				{
					return PathsHelper.RootDirectory + Path.DirectorySeparatorChar.ToString() + "Scripts" + Path.DirectorySeparatorChar.ToString();
				}
			}
			else if (num != 3809333786U)
			{
				if (num == 4115474139U)
				{
					if (sWhatPath == "Transcoders_User")
					{
						return CONFIG.sUserPath + "ImportExport" + Path.DirectorySeparatorChar.ToString();
					}
				}
			}
			else if (sWhatPath == "PerUser-ISA-Config")
			{
				string sFolder = "C:\\";
				try
				{
					sFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				}
				catch (Exception eX2)
				{
				}
				return sFolder + "\\microsoft\\firewall client 2004\\management.ini";
			}
			return "C:\\";
		}

		/// <summary>
		/// Returns the path and filename of the editor used to edit the Rules script file.
		/// </summary>
		// Token: 0x17000048 RID: 72
		// (get) Token: 0x0600014D RID: 333 RVA: 0x00011978 File Offset: 0x0000FB78
		// (set) Token: 0x0600014E RID: 334 RVA: 0x0001199A File Offset: 0x0000FB9A
		[CodeDescription("Return path to user's FiddlerScript editor.")]
		public static string JSEditor
		{
			get
			{
				if (string.IsNullOrEmpty(CONFIG.m_JSEditor))
				{
					CONFIG.m_JSEditor = CONFIG.GetPath("DefaultScriptEditor");
				}
				return CONFIG.m_JSEditor;
			}
			set
			{
				CONFIG.m_JSEditor = value;
			}
		}

		/// <summary>
		/// Returns true if Fiddler should permit remote connections. Requires restart.
		/// </summary>
		// Token: 0x17000049 RID: 73
		// (get) Token: 0x0600014F RID: 335 RVA: 0x000119A2 File Offset: 0x0000FBA2
		// (set) Token: 0x06000150 RID: 336 RVA: 0x000119A9 File Offset: 0x0000FBA9
		[CodeDescription("Returns true if Fiddler is configured to accept remote clients.")]
		public static bool bAllowRemoteConnections
		{
			get
			{
				return CONFIG.allowRemoteConnections;
			}
			internal set
			{
				CONFIG.allowRemoteConnections = value;
			}
		}

		/// <summary>
		/// Ensure that the per-user folders used by Fiddler are present.
		/// </summary>
		// Token: 0x06000151 RID: 337 RVA: 0x000119B4 File Offset: 0x0000FBB4
		internal static void EnsureFoldersExist()
		{
			try
			{
				if (!Directory.Exists(CONFIG.GetPath("Captures")))
				{
					Directory.CreateDirectory(CONFIG.GetPath("Captures"));
				}
				if (!Directory.Exists(CONFIG.GetPath("Requests")))
				{
					Directory.CreateDirectory(CONFIG.GetPath("Requests"));
				}
				if (!Directory.Exists(CONFIG.GetPath("Responses")))
				{
					Directory.CreateDirectory(CONFIG.GetPath("Responses"));
				}
				if (!Directory.Exists(CONFIG.GetPath("Scripts")))
				{
					Directory.CreateDirectory(CONFIG.GetPath("Scripts"));
				}
			}
			catch (Exception eX)
			{
				string title = "Folder Creation Failed";
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[]
				{
					title,
					eX.ToString()
				});
			}
		}

		// Token: 0x06000152 RID: 338 RVA: 0x00011A84 File Offset: 0x0000FC84
		static CONFIG()
		{
			try
			{
				try
				{
					IPGlobalProperties oIPGP = IPGlobalProperties.GetIPGlobalProperties();
					CONFIG.sMachineDomain = oIPGP.DomainName.ToLowerInvariant();
					CONFIG.sMachineName = oIPGP.HostName.ToLowerInvariant();
				}
				catch (Exception eX)
				{
				}
				CONFIG.m_ListenPort = 8866;
				CONFIG._LoadPreferences();
				if (Environment.OSVersion.Version.Major < 6 && Environment.OSVersion.Version.Minor < 1)
				{
					CONFIG.bMapSocketToProcess = false;
				}
			}
			catch (Exception eX2)
			{
				string title = "Initialization of CONFIG Prefs Failed";
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[]
				{
					title,
					eX2.ToString()
				});
			}
		}

		/// <summary>
		/// Loads Preferences from the Registry and fills appropriate fields
		/// </summary>
		// Token: 0x06000153 RID: 339 RVA: 0x00011D64 File Offset: 0x0000FF64
		private static void _LoadPreferences()
		{
			CONFIG._Prefs = new PreferenceBag(null);
			CONFIG.bReloadSessionIDAsFlag = FiddlerApplication.Prefs.GetBoolPref("fiddler.saz.ReloadIDAsFlag", CONFIG.bReloadSessionIDAsFlag);
			CONFIG.bDebugCertificateGeneration = FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.Debug", CONFIG.bDebugCertificateGeneration);
			CONFIG.bUseSNIForCN = FiddlerApplication.Prefs.GetBoolPref("fiddler.network.https.SetCNFromSNI", false);
			string sList = FiddlerApplication.Prefs.GetStringPref("fiddler.network.https.SupportedClientProtocolVersions", null);
			if (!string.IsNullOrEmpty(sList))
			{
				SslProtocols sslChoices = Utilities.ParseSSLProtocolString(sList);
				if (sslChoices != SslProtocols.None)
				{
					CONFIG.oAcceptedClientHTTPSProtocols = sslChoices;
				}
			}
			sList = FiddlerApplication.Prefs.GetStringPref("fiddler.network.https.SupportedServerProtocolVersions", null);
			if (!string.IsNullOrEmpty(sList))
			{
				SslProtocols sslChoices2 = Utilities.ParseSSLProtocolString(sList);
				if (sslChoices2 != SslProtocols.None)
				{
					CONFIG.oAcceptedServerHTTPSProtocols = sslChoices2;
				}
				CONFIG.bMimicClientHTTPSProtocols = sList.OICContains("<client>");
			}
			CONFIG._cb_STREAM_LARGE_FILES = FiddlerApplication.Prefs.GetInt32Pref("fiddler.memory.DropIfOver", CONFIG._cb_STREAM_LARGE_FILES);
			int cb = FiddlerApplication.Prefs.GetInt32Pref("fiddler.memory.StreamAndForgetIfOver", -1);
			if (cb < 0)
			{
				cb = 2147483591;
			}
			CONFIG.cbAutoStreamAndForget = cb;
		}

		// Token: 0x06000154 RID: 340 RVA: 0x00011E60 File Offset: 0x00010060
		internal static bool ShouldSkipDecryption(string sHost)
		{
			if (CONFIG.oHLSkipDecryption == null)
			{
				return false;
			}
			bool bOnList = CONFIG.oHLSkipDecryption.ContainsHost(sHost);
			if (CONFIG.bHLSkipDecryptionInvert)
			{
				bOnList = !bOnList;
			}
			return bOnList;
		}

		/// <summary>
		/// Underlying Preferences container whose IFiddlerPreferences interface is 
		/// exposed by the FiddlerApplication.Prefs property.
		/// </summary>
		// Token: 0x04000061 RID: 97
		private static PreferenceBag _Prefs = null;

		// Token: 0x04000062 RID: 98
		private static List<int> _instrumentedBrowserProcessIDs = new List<int>();

		/// <summary>
		/// Response files larger than this (2^28 = ~262mb) will NOT be loaded into memory when using LoadResponseFromFile
		/// </summary>
		// Token: 0x04000063 RID: 99
		internal static int _cb_STREAM_LARGE_FILES = 536870912;

		// Token: 0x04000064 RID: 100
		internal static int cbAutoStreamAndForget = 2147483591;

		// Token: 0x04000065 RID: 101
		internal static string sDefaultBrowserExe = "iexplore.exe";

		// Token: 0x04000066 RID: 102
		internal static string sDefaultBrowserParams = string.Empty;

		// Token: 0x04000067 RID: 103
		internal static bool bRunningOnCLRv4 = true;

		// Token: 0x04000068 RID: 104
		private static ProcessFilterCategories _pfcDecyptFilter = ProcessFilterCategories.All;

		/// <summary>
		/// Cached layout info for columns.
		/// </summary>
		// Token: 0x04000069 RID: 105
		private static string sLVColInfo = null;

		// Token: 0x0400006A RID: 106
		internal static bool bReloadSessionIDAsFlag = false;

		/// <summary>
		/// True if this is a "Viewer" instance of Fiddler that will not persist its settings. Exposed as FiddlerApplication.IsViewerMode
		/// </summary>
		/// <remarks>
		/// TODO: ARCH: This setting shouldn't exist in FiddlerCore, but it's used in a dozen places</remarks>
		// Token: 0x0400006B RID: 107
		internal static bool bIsViewOnly = false;

		/// <summary>
		/// TODO: Why is this defaulted to FALSE? Has been since 2009, probably due to some bug. Should keep better records. (Sigh).
		/// </summary>
		// Token: 0x0400006C RID: 108
		internal static bool bUseXceedDecompressForGZIP = false;

		// Token: 0x0400006D RID: 109
		internal static bool bUseXceedDecompressForDeflate = false;

		/// <summary>
		/// Boolean controls whether Fiddler should map inbound connections to their original process using IPHLPAPI
		/// </summary>
		// Token: 0x0400006E RID: 110
		public static bool bMapSocketToProcess = true;

		// Token: 0x0400006F RID: 111
		[Obsolete("Please, use the 'DecryptHTTPS' property instead.")]
		public static bool bMITM_HTTPS = false;

		/// <summary>
		/// Boolean controls whether Fiddler will attempt to use the Server Name Indicator TLS extension to generate the SubjectCN for certificates
		/// </summary>
		// Token: 0x04000070 RID: 112
		public static bool bUseSNIForCN = false;

		// Token: 0x04000071 RID: 113
		private static bool bIgnoreServerCertErrors = false;

		// Token: 0x04000072 RID: 114
		[Obsolete("Please, use 'StreamAudioVideo' property instead.")]
		public static bool bStreamAudioVideo = false;

		// Token: 0x04000073 RID: 115
		internal static bool bCheckCompressionIntegrity = false;

		// Token: 0x04000074 RID: 116
		internal static bool bShowDefaultClientCertificateNeededPrompt = true;

		/// <summary>
		/// Returns 127.0.0.1:{ListenPort} or fiddler.network.proxy.RegistrationHostName:{ListenPort}
		/// </summary>
		// Token: 0x04000075 RID: 117
		internal static string sFiddlerListenHostPort = "127.0.0.1:8888";

		// Token: 0x04000076 RID: 118
		internal static string sMakeCertParamsRoot = "-r -ss my -n \"CN={0}{1}\" -sky signature -eku 1.3.6.1.5.5.7.3.1 -h 1 -cy authority -a {3} -m 132 -b {4} {5}";

		// Token: 0x04000077 RID: 119
		internal static string sMakeCertParamsEE = "-pe -ss my -n \"CN={0}{1}\" -sky exchange -in {2} -is my -eku 1.3.6.1.5.5.7.3.1 -cy end -a {3} -m 132 -b {4} {5}";

		// Token: 0x04000078 RID: 120
		internal static string sMakeCertRootCN = "DO_NOT_TRUST_FiddlerRoot";

		// Token: 0x04000079 RID: 121
		internal static string sMakeCertSubjectO = ", O=DO_NOT_TRUST, OU=Created by http://www.fiddler2.com";

		// Token: 0x0400007A RID: 122
		private static string sRootUrl = "http://fiddler2.com/fiddlercore/";

		// Token: 0x0400007B RID: 123
		private static string sSecureRootUrl = "https://fiddler2.com/";

		// Token: 0x0400007C RID: 124
		internal static string sRootKey = "SOFTWARE\\Microsoft\\FiddlerCore\\";

		// Token: 0x0400007D RID: 125
		private static string sUserPath = CONFIG.GetPath("MyDocs") + Path.DirectorySeparatorChar.ToString() + "FiddlerCore" + Path.DirectorySeparatorChar.ToString();

		/// <summary>
		/// Use 128bit AES Encryption when password-protecting .SAZ files. Note that, while this 
		/// encryption is much stronger than the default encryption algorithm, it is significantly
		/// slower to save and load these files, and the Windows Explorer ZIP utility cannot open them.
		/// </summary>
		// Token: 0x0400007E RID: 126
		public static bool bUseAESForSAZ = true;

		/// <summary>
		/// SSL/TLS Protocols we allow the client to choose from (when we call AuthenticateAsServer)
		/// We allow all TLS protocols by default (Tls1, Tls1.1, Tls1.2). We 'Bitwise OR' in the constants for TLS1.1 and TLS1.2 because we still build for .NET4.0.
		/// </summary>
		// Token: 0x0400007F RID: 127
		public static SslProtocols oAcceptedClientHTTPSProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

		/// <summary>
		/// SSL/TLS Protocols we request the server use (when we call AuthenticateAsClient). By default, TLS1, TLS1.1 and TLS1.2 are accepted.
		/// </summary>
		// Token: 0x04000080 RID: 128
		public static SslProtocols oAcceptedServerHTTPSProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

		/// <summary>
		/// When True, Fiddler will offer the latest TLS protocol version offered by the client in its request
		/// </summary>
		// Token: 0x04000081 RID: 129
		internal static bool bMimicClientHTTPSProtocols = true;

		/// <summary>
		/// Version information for the Fiddler/FiddlerCore assembly
		/// </summary>
		// Token: 0x04000082 RID: 130
		public static Version FiddlerVersionInfo = Assembly.GetExecutingAssembly().GetName().Version;

		// Token: 0x04000083 RID: 131
		internal const int I_MAX_CONNECTION_QUEUE = 50;

		// Token: 0x04000084 RID: 132
		internal static bool bIsBeta = false;

		/// <summary>
		/// Will send traffic to an upstream proxy?
		/// OBSOLETE -- DO NOT USE. see <see cref="P:Fiddler.CONFIG.UpstreamGateway" /> instead.
		/// </summary>
		// Token: 0x04000085 RID: 133
		[Obsolete]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static bool bForwardToGateway = true;

		// Token: 0x04000086 RID: 134
		[Obsolete]
		internal static GatewayType _UpstreamGateway = GatewayType.System;

		// Token: 0x04000087 RID: 135
		public static bool bDebugSpew = false;

		/// <summary>
		/// The encoding with which HTTP Headers should be parsed. Defaults to UTF8, but may be overridden by specifying a REG_SZ containing the encoding name in the registry key \Fiddler2\HeaderEncoding
		/// </summary>
		// Token: 0x04000088 RID: 136
		public static Encoding oHeaderEncoding = Encoding.UTF8;

		// Token: 0x04000089 RID: 137
		public static Encoding oBodyEncoding = Encoding.UTF8;

		// Token: 0x0400008A RID: 138
		[Obsolete("Please, use the 'ReuseServerSockets' property instead.")]
		public static bool bReuseServerSockets = true;

		// Token: 0x0400008B RID: 139
		[Obsolete("Please, use the 'ReuseClientSockets' property instead.")]
		public static bool bReuseClientSockets = true;

		/// <summary>
		/// Controls whether Fiddler should register as the HTTPS proxy
		/// </summary>
		// Token: 0x0400008C RID: 140
		public static bool bCaptureCONNECT = true;

		// Token: 0x0400008D RID: 141
		[Obsolete("Please, use 'CaptureFTP' property instead.")]
		public static bool bCaptureFTP = false;

		/// <summary>
		/// Controls whether Fiddler will try to write exceptions to the System Event log. Note: Usually fails due to ACLs on the Event Log.
		/// </summary>
		// Token: 0x0400008E RID: 142
		public static bool bUseEventLogForExceptions = false;

		/// <summary>
		/// Controls whether Fiddler will attempt to log on to the upstream proxy server to download the proxy configuration script
		/// </summary>
		// Token: 0x0400008F RID: 143
		public static bool bAutoProxyLogon = false;

		// Token: 0x04000090 RID: 144
		[Obsolete("Please, use the 'EnableIPv6' property instead.")]
		public static bool bEnableIPv6 = Environment.OSVersion.Version.Major > 5;

		// Token: 0x04000093 RID: 147
		private static bool bHookAllConnections = true;

		// Token: 0x04000094 RID: 148
		private static bool bHookWithPAC = false;

		// Token: 0x04000095 RID: 149
		[Obsolete]
		private static string m_sHostsThatBypassFiddler = string.Empty;

		// Token: 0x04000096 RID: 150
		private static string m_JSEditor;

		/// <summary>
		/// The username to send to the upstream gateway if the Version Checking webservice request requires authentication
		/// </summary>
		// Token: 0x04000097 RID: 151
		public static string sGatewayUsername;

		/// <summary>
		/// The password to send to the upstream gateway if the Version Checking webservice request requires authentication
		/// </summary>
		// Token: 0x04000098 RID: 152
		public static string sGatewayPassword;

		// Token: 0x04000099 RID: 153
		private static bool m_bCheckForISA = true;

		// Token: 0x0400009A RID: 154
		private static int m_ListenPort = 8888;

		/// <summary>
		/// Set this flag if m_ListenPort is a "temporary" port (E.g. specified on command-line) and it shouldn't be overridden in the registry
		/// </summary>
		// Token: 0x0400009B RID: 155
		internal static bool bUsingPortOverride = false;

		// Token: 0x0400009C RID: 156
		private static bool m_bForceExclusivePort;

		/// <summary>
		/// Controls whether Certificate-Generation output will be spewed to the Fiddler Log
		/// </summary>
		// Token: 0x0400009D RID: 157
		public static bool bDebugCertificateGeneration = true;

		// Token: 0x0400009E RID: 158
		private static int _iReverseProxyForPort;

		/// <summary>
		/// Alternative hostname which Fiddler should recognize as an alias for the local machine. The
		/// default value of ? will never be usable, as it's the QueryString delimiter
		/// </summary>
		// Token: 0x0400009F RID: 159
		public static string sAlternateHostname = "?";

		// Token: 0x040000A0 RID: 160
		internal static string sReverseProxyHostname = "localhost";

		/// <summary>
		/// (Lowercase) Machine Name
		/// </summary>
		// Token: 0x040000A1 RID: 161
		internal static string sMachineName = string.Empty;

		/// <summary>
		/// (Lowercase) Machine Domain Name
		/// </summary>
		// Token: 0x040000A2 RID: 162
		internal static string sMachineDomain = string.Empty;

		/// <summary>
		/// List of hostnames for which HTTPS decryption (if enabled) should be skipped
		/// </summary>
		// Token: 0x040000A3 RID: 163
		internal static HostList oHLSkipDecryption = null;

		// Token: 0x040000A4 RID: 164
		internal static bool bHLSkipDecryptionInvert = false;

		/// <summary>
		/// True if Fiddler should be maximized on restart
		/// </summary>
		// Token: 0x040000A5 RID: 165
		private static bool fNeedToMaximizeOnload;

		// Token: 0x040000A6 RID: 166
		public static RetryMode RetryOnReceiveFailure = RetryMode.Always;

		// Token: 0x040000A7 RID: 167
		internal const string RootPath = "Root";

		// Token: 0x040000A8 RID: 168
		private static bool allowRemoteConnections;
	}
}
