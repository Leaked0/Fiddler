using System;
using Telerik.NetworkConnections;

namespace Fiddler
{
	/// <summary>
	/// Holds startup settings for FiddlerCore.
	/// Use the <see cref="T:Fiddler.FiddlerCoreStartupSettingsBuilder" /> to build an instance of this class.
	/// Then pass the instance to the <see cref="M:Fiddler.FiddlerApplication.Startup(Fiddler.FiddlerCoreStartupSettings)" /> method to start FiddlerCore.
	/// </summary>
	// Token: 0x02000005 RID: 5
	public class FiddlerCoreStartupSettings
	{
		/// <summary>
		/// Initializes a new instance of <see cref="T:Fiddler.FiddlerCoreStartupSettings" />.
		/// </summary>
		// Token: 0x0600004C RID: 76 RVA: 0x00003BD3 File Offset: 0x00001DD3
		internal FiddlerCoreStartupSettings()
		{
		}

		/// <summary>
		/// The port on which the FiddlerCore app will listen on. If 0, a random port will be used.
		/// </summary>
		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600004D RID: 77 RVA: 0x00003BDB File Offset: 0x00001DDB
		// (set) Token: 0x0600004E RID: 78 RVA: 0x00003BE3 File Offset: 0x00001DE3
		public virtual ushort ListenPort { get; internal set; }

		/// <summary>
		/// If set to true, FiddlerCore registers as the system proxy.
		/// </summary>
		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600004F RID: 79 RVA: 0x00003BEC File Offset: 0x00001DEC
		// (set) Token: 0x06000050 RID: 80 RVA: 0x00003BF4 File Offset: 0x00001DF4
		public virtual bool RegisterAsSystemProxy { get; internal set; }

		/// <summary>
		/// If set to true, FiddlerCore decrypts HTTPS Traffic.
		/// </summary>
		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000051 RID: 81 RVA: 0x00003BFD File Offset: 0x00001DFD
		// (set) Token: 0x06000052 RID: 82 RVA: 0x00003C05 File Offset: 0x00001E05
		public virtual bool DecryptSSL { get; internal set; }

		/// <summary>
		/// If set to true, FiddlerCore accepts requests from remote computers or devices. WARNING: Security Impact.
		/// </summary>
		/// <remarks>
		/// Use caution when allowing Remote Clients to connect. If a hostile computer is able to proxy its traffic through your
		/// FiddlerCore instance, he could circumvent IPSec traffic rules, circumvent intranet firewalls, consume memory on your PC, etc.
		/// </remarks>
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000053 RID: 83 RVA: 0x00003C0E File Offset: 0x00001E0E
		// (set) Token: 0x06000054 RID: 84 RVA: 0x00003C16 File Offset: 0x00001E16
		public virtual bool AllowRemoteClients { get; internal set; }

		/// <summary>
		/// If set to true, FiddlerCore forwards requests to any upstream gateway.
		/// </summary>
		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000055 RID: 85 RVA: 0x00003C1F File Offset: 0x00001E1F
		// (set) Token: 0x06000056 RID: 86 RVA: 0x00003C27 File Offset: 0x00001E27
		public virtual bool ChainToUpstreamGateway { get; internal set; }

		/// <summary>
		/// If set to true, FiddlerCore sets all connections to use it, otherwise only the Local LAN is pointed to FiddlerCore.
		/// </summary>
		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000057 RID: 87 RVA: 0x00003C30 File Offset: 0x00001E30
		// (set) Token: 0x06000058 RID: 88 RVA: 0x00003C38 File Offset: 0x00001E38
		public virtual bool MonitorAllConnections { get; internal set; }

		/// <summary>
		/// If set to true, FiddlerCore sets connections to use a self-generated PAC File.
		/// </summary>
		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000059 RID: 89 RVA: 0x00003C41 File Offset: 0x00001E41
		// (set) Token: 0x0600005A RID: 90 RVA: 0x00003C49 File Offset: 0x00001E49
		public virtual bool HookUsingPACFile { get; internal set; }

		/// <summary>
		/// If set to true, FiddlerCore passes the &lt;-loopback&gt; token to the proxy exception list.
		/// </summary>
		// Token: 0x1700000E RID: 14
		// (get) Token: 0x0600005B RID: 91 RVA: 0x00003C52 File Offset: 0x00001E52
		// (set) Token: 0x0600005C RID: 92 RVA: 0x00003C5A File Offset: 0x00001E5A
		[Obsolete("Use the Telerik.NetworkConnections.NetworkConnectionsManager to register the FiddlerCore Proxy as proxy for each required connection and set the BypassHosts accordingly.")]
		public virtual bool CaptureLocalhostTraffic { get; internal set; }

		/// <summary>
		/// If set to true, FiddlerCore registers as the FTP proxy.
		/// </summary>
		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600005D RID: 93 RVA: 0x00003C63 File Offset: 0x00001E63
		// (set) Token: 0x0600005E RID: 94 RVA: 0x00003C6B File Offset: 0x00001E6B
		public virtual bool CaptureFTP { get; internal set; }

		/// <summary>
		/// The proxy settings which FiddlerCore uses to find the upstream proxy.
		/// </summary>
		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600005F RID: 95 RVA: 0x00003C74 File Offset: 0x00001E74
		// (set) Token: 0x06000060 RID: 96 RVA: 0x00003C7C File Offset: 0x00001E7C
		public virtual ProxySettings UpstreamProxySettings { get; internal set; }
	}
}
