using System;
using Telerik.NetworkConnections;

namespace Fiddler
{
	/// <summary>
	/// A generic builder class for <see cref="T:Fiddler.FiddlerCoreStartupSettings" />.
	/// </summary>
	/// <typeparam name="T"><see cref="T:Fiddler.FiddlerCoreStartupSettingsBuilder`2" /></typeparam>
	/// <typeparam name="P"><see cref="T:Fiddler.FiddlerCoreStartupSettings" /></typeparam>
	// Token: 0x02000003 RID: 3
	public abstract class FiddlerCoreStartupSettingsBuilder<T, P> : IFiddlerCoreStartupSettingsBuilder<T, P> where T : FiddlerCoreStartupSettingsBuilder<T, P> where P : FiddlerCoreStartupSettings
	{
		/// <summary>
		/// Initializes a new instance of <see cref="T:Fiddler.FiddlerCoreStartupSettingsBuilder`2" />
		/// </summary>
		/// <param name="fiddlerCoreStartupSettings">The instance of FiddlerCoreStartupSettings which is going to be built.</param>
		// Token: 0x06000035 RID: 53 RVA: 0x00003A69 File Offset: 0x00001C69
		internal FiddlerCoreStartupSettingsBuilder(P fiddlerCoreStartupSettings)
		{
			if (fiddlerCoreStartupSettings == null)
			{
				throw new ArgumentNullException("fiddlerCoreStartupSettings", "fiddlerCoreStartupSettings cannot be null.");
			}
			this.fiddlerCoreStartupSettings = fiddlerCoreStartupSettings;
			this.t = (T)((object)this);
		}

		/// <summary>
		/// The port on which the FiddlerCore app will listen on. If 0, a random port will be used.
		/// </summary>
		/// <param name="port">The port on which the FiddlerCore app should listen on.</param>
		/// <returns><see cref="T:Fiddler.FiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x06000036 RID: 54 RVA: 0x00003A9C File Offset: 0x00001C9C
		public virtual T ListenOnPort(ushort port)
		{
			this.fiddlerCoreStartupSettings.ListenPort = port;
			return this.t;
		}

		/// <summary>
		/// Registers as the system proxy.
		/// </summary>
		/// <returns><see cref="T:Fiddler.FiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x06000037 RID: 55 RVA: 0x00003AB5 File Offset: 0x00001CB5
		public virtual T RegisterAsSystemProxy()
		{
			this.fiddlerCoreStartupSettings.RegisterAsSystemProxy = true;
			return this.t;
		}

		/// <summary>
		/// Decrypts HTTPS Traffic.
		/// </summary>
		/// <returns><see cref="T:Fiddler.FiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x06000038 RID: 56 RVA: 0x00003ACE File Offset: 0x00001CCE
		public virtual T DecryptSSL()
		{
			this.fiddlerCoreStartupSettings.DecryptSSL = true;
			return this.t;
		}

		/// <summary>
		/// Accepts requests from remote computers or devices. WARNING: Security Impact
		/// </summary>
		/// <remarks>
		/// Use caution when allowing Remote Clients to connect. If a hostile computer is able to proxy its traffic through your
		/// FiddlerCore instance, he could circumvent IPSec traffic rules, circumvent intranet firewalls, consume memory on your PC, etc.
		/// </remarks>
		/// <returns><see cref="T:Fiddler.FiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x06000039 RID: 57 RVA: 0x00003AE7 File Offset: 0x00001CE7
		public virtual T AllowRemoteClients()
		{
			this.fiddlerCoreStartupSettings.AllowRemoteClients = true;
			return this.t;
		}

		/// <summary>
		/// Forwards requests to any upstream gateway.
		/// </summary>
		/// <returns><see cref="T:Fiddler.FiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x0600003A RID: 58 RVA: 0x00003B00 File Offset: 0x00001D00
		public virtual T ChainToUpstreamGateway()
		{
			this.fiddlerCoreStartupSettings.ChainToUpstreamGateway = true;
			return this.t;
		}

		/// <summary>
		/// Sets all connections to use FiddlerCore, otherwise only the Local LAN is pointed to FiddlerCore.
		/// </summary>
		/// <returns><see cref="T:Fiddler.FiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x0600003B RID: 59 RVA: 0x00003B19 File Offset: 0x00001D19
		public virtual T MonitorAllConnections()
		{
			this.fiddlerCoreStartupSettings.MonitorAllConnections = true;
			return this.t;
		}

		/// <summary>
		/// Sets connections to use a self-generated PAC File.
		/// </summary>
		/// <returns><see cref="T:Fiddler.FiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x0600003C RID: 60 RVA: 0x00003B32 File Offset: 0x00001D32
		public virtual T HookUsingPACFile()
		{
			this.fiddlerCoreStartupSettings.HookUsingPACFile = true;
			return this.t;
		}

		/// <summary>
		/// Passes the &lt;-loopback&gt; token to the proxy exception list.
		/// </summary>
		/// <returns><see cref="T:Fiddler.FiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x0600003D RID: 61 RVA: 0x00003B4B File Offset: 0x00001D4B
		[Obsolete("Use the Telerik.NetworkConnections.NetworkConnectionsManager to register the FiddlerCore Proxy as proxy for each required connection and set the BypassHosts accordingly.")]
		public virtual T CaptureLocalhostTraffic()
		{
			this.fiddlerCoreStartupSettings.CaptureLocalhostTraffic = true;
			return this.t;
		}

		/// <summary>
		/// Registers FiddlerCore as the FTP proxy.
		/// </summary>
		/// <returns><see cref="T:Fiddler.FiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x0600003E RID: 62 RVA: 0x00003B64 File Offset: 0x00001D64
		public virtual T CaptureFTP()
		{
			this.fiddlerCoreStartupSettings.CaptureFTP = true;
			return this.t;
		}

		/// <summary>
		/// Sets the proxy settings which FiddlerCore uses to find the upstream proxy.
		/// </summary>
		/// <param name="proxySettings"><see cref="T:Telerik.NetworkConnections.ProxySettings" /></param>
		/// <returns><see cref="T:Fiddler.IFiddlerCoreStartupSettingsBuilder`2" /></returns>
		// Token: 0x0600003F RID: 63 RVA: 0x00003B7D File Offset: 0x00001D7D
		public virtual T SetUpstreamProxySettingsTo(ProxySettings proxySettings)
		{
			this.fiddlerCoreStartupSettings.UpstreamProxySettings = proxySettings;
			return this.t;
		}

		/// <summary>
		/// Builds the FiddlerCoreStartupSettings instance.
		/// </summary>
		/// <returns>The instance of FiddlerCoreStartupSettings.</returns>
		// Token: 0x06000040 RID: 64 RVA: 0x00003B98 File Offset: 0x00001D98
		public P Build()
		{
			if (this.fiddlerCoreStartupSettingsIsBuilt)
			{
				throw new InvalidOperationException("An instance of FiddlerCoreStartupSettingsBuilder is able to build FiddlerCoreStartupSettings only once.");
			}
			this.fiddlerCoreStartupSettingsIsBuilt = true;
			P result = this.fiddlerCoreStartupSettings;
			this.fiddlerCoreStartupSettings = default(P);
			return result;
		}

		/// <summary>
		/// The FiddlerCoreStartupSettings instance being built.
		/// </summary>
		// Token: 0x0400000E RID: 14
		protected P fiddlerCoreStartupSettings;

		/// <summary>
		/// Reference to this. Return this field instead of (T)this in your methods in order to avoid multiple casting.
		/// </summary>
		// Token: 0x0400000F RID: 15
		protected readonly T t;

		// Token: 0x04000010 RID: 16
		private bool fiddlerCoreStartupSettingsIsBuilt;
	}
}
