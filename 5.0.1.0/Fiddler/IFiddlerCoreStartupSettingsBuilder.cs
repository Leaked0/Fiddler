using System;
using Telerik.NetworkConnections;

namespace Fiddler
{
	/// <summary>
	/// A generic builder interface for <see cref="T:Fiddler.FiddlerCoreStartupSettings" />.
	/// </summary>
	/// <typeparam name="T"><see cref="T:Fiddler.IFiddlerCoreStartupSettingsBuilder`2" /></typeparam>
	/// <typeparam name="P"><see cref="T:Fiddler.FiddlerCoreStartupSettings" /></typeparam>
	// Token: 0x02000004 RID: 4
	public interface IFiddlerCoreStartupSettingsBuilder<out T, out P> where T : IFiddlerCoreStartupSettingsBuilder<T, P> where P : FiddlerCoreStartupSettings
	{
		/// <summary>
		/// The port on which the FiddlerCore app will listen on. If 0, a random port will be used.
		/// </summary>
		/// <param name="port">The port on which the FiddlerCore app should listen on.</param>
		/// <returns><see cref="T:Fiddler.IFiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x06000041 RID: 65
		T ListenOnPort(ushort port);

		/// <summary>
		/// Registers as the system proxy.
		/// </summary>
		/// <returns><see cref="T:Fiddler.IFiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x06000042 RID: 66
		T RegisterAsSystemProxy();

		/// <summary>
		/// Decrypts HTTPS Traffic.
		/// </summary>
		/// <returns><see cref="T:Fiddler.IFiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x06000043 RID: 67
		T DecryptSSL();

		/// <summary>
		/// Accepts requests from remote computers or devices. WARNING: Security Impact
		/// </summary>
		/// <remarks>
		/// Use caution when allowing Remote Clients to connect. If a hostile computer is able to proxy its traffic through your
		/// FiddlerCore instance, he could circumvent IPSec traffic rules, circumvent intranet firewalls, consume memory on your PC, etc.
		/// </remarks>
		/// <returns><see cref="T:Fiddler.IFiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x06000044 RID: 68
		T AllowRemoteClients();

		/// <summary>
		/// Forwards requests to any upstream gateway.
		/// </summary>
		/// <returns><see cref="T:Fiddler.IFiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x06000045 RID: 69
		[Obsolete("Please, use the SetUpstreamProxySettingsTo method to provide the upstream proxy for FiddlerCore.")]
		T ChainToUpstreamGateway();

		/// <summary>
		/// Sets all connections to use FiddlerCore, otherwise only the Local LAN is pointed to FiddlerCore.
		/// </summary>
		/// <returns><see cref="T:Fiddler.IFiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x06000046 RID: 70
		T MonitorAllConnections();

		/// <summary>
		/// Sets connections to use a self-generated PAC File.
		/// </summary>
		/// <returns><see cref="T:Fiddler.IFiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x06000047 RID: 71
		T HookUsingPACFile();

		/// <summary>
		/// Passes the &lt;-loopback&gt; token to the proxy exception list.
		/// </summary>
		/// <returns><see cref="T:Fiddler.IFiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x06000048 RID: 72
		[Obsolete("Use the Telerik.NetworkConnections.NetworkConnectionsManager to register the FiddlerCore Proxy as proxy for each required connection and set the BypassHosts accordingly.")]
		T CaptureLocalhostTraffic();

		/// <summary>
		/// Registers FiddlerCore as the FTP proxy.
		/// </summary>
		/// <returns><see cref="T:Fiddler.IFiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x06000049 RID: 73
		T CaptureFTP();

		/// <summary>
		/// Sets the proxy settings which FiddlerCore uses to find the upstream proxy.
		/// </summary>
		/// <param name="proxySettings"><see cref="T:Telerik.NetworkConnections.ProxySettings" /></param>
		/// <returns><see cref="T:Fiddler.IFiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x0600004A RID: 74
		T SetUpstreamProxySettingsTo(ProxySettings proxySettings);

		/// <summary>
		/// Builds the FiddlerCoreStartupSettings instance.
		/// </summary>
		/// <returns>The instance of FiddlerCoreStartupSettings.</returns>
		// Token: 0x0600004B RID: 75
		P Build();
	}
}
