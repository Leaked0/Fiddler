using System;
using System.Net;
using Telerik.NetworkConnections;

namespace Fiddler
{
	// Token: 0x0200000A RID: 10
	public class FiddlerUpstreamProxy : IWebProxy
	{
		// Token: 0x17000024 RID: 36
		// (get) Token: 0x060000C0 RID: 192 RVA: 0x0000761D File Offset: 0x0000581D
		// (set) Token: 0x060000C1 RID: 193 RVA: 0x00007625 File Offset: 0x00005825
		public ICredentials Credentials { get; set; } = CredentialCache.DefaultNetworkCredentials;

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x060000C2 RID: 194 RVA: 0x0000762E File Offset: 0x0000582E
		// (set) Token: 0x060000C3 RID: 195 RVA: 0x00007636 File Offset: 0x00005836
		public bool ForceSystemProxy { get; set; }

		// Token: 0x060000C4 RID: 196 RVA: 0x00007640 File Offset: 0x00005840
		public Uri GetProxy(Uri destination)
		{
			return this.CheckUri<Uri>(destination, () => destination, (Uri uri) => uri);
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00007694 File Offset: 0x00005894
		public bool IsBypassed(Uri host)
		{
			return this.CheckUri<bool>(host, () => true, (Uri uri) => false);
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x000076E8 File Offset: 0x000058E8
		private T CheckUri<T>(Uri uri, Func<T> bypass, Func<Uri, T> proxy)
		{
			if (!uri.IsAbsoluteUri)
			{
				return bypass();
			}
			if (FiddlerApplication.oProxy == null)
			{
				return bypass();
			}
			IPEndPoint proxyEndpoint;
			if (this.ForceSystemProxy)
			{
				proxyEndpoint = this.GetSystemProxyEndpoint(uri.Scheme);
			}
			else
			{
				proxyEndpoint = FiddlerApplication.oProxy.FindGatewayForOrigin(uri.Scheme, string.Format("{0}:{1}", uri.Host, uri.Port));
			}
			if (proxyEndpoint == null)
			{
				return bypass();
			}
			return proxy(new UriBuilder(proxyEndpoint.ToString()).Uri);
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x00007778 File Offset: 0x00005978
		private IPEndPoint GetSystemProxyEndpoint(string uriSheme)
		{
			Proxy oProxy = FiddlerApplication.oProxy;
			ProxySettings systemProxySettings = ((oProxy != null) ? oProxy.GetDefaultConnectionUpstreamProxy() : null);
			if (systemProxySettings == null)
			{
				return null;
			}
			IPEndPoint proxyEndpoint = null;
			if (uriSheme.OICEquals("http") && systemProxySettings.HttpProxyEnabled)
			{
				proxyEndpoint = Proxy.GetFirstRespondingEndpoint(string.Format("{0}:{1}", systemProxySettings.HttpProxyHost, systemProxySettings.HttpProxyPort));
			}
			if (uriSheme.OICEquals("https") && systemProxySettings.HttpsProxyEnabled)
			{
				proxyEndpoint = Proxy.GetFirstRespondingEndpoint(string.Format("{0}:{1}", systemProxySettings.HttpsProxyHost, systemProxySettings.HttpsProxyPort));
			}
			if (uriSheme.OICEquals("ftp") && systemProxySettings.FtpProxyEnabled)
			{
				proxyEndpoint = Proxy.GetFirstRespondingEndpoint(string.Format("{0}:{1}", systemProxySettings.FtpProxyHost, systemProxySettings.FtpProxyPort));
			}
			return proxyEndpoint;
		}
	}
}
