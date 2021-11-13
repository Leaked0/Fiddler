using System;
using System.IO;
using System.Net;
using System.Threading;
using FiddlerCore.PlatformExtensions;
using FiddlerCore.PlatformExtensions.API;

namespace Fiddler
{
	/// <summary>
	/// The AutoProxy class is used to handle upstream gateways when the client was configured to use WPAD or a Proxy AutoConfig (PAC) script.
	/// </summary>
	// Token: 0x02000074 RID: 116
	internal class AutoProxy : IDisposable
	{
		/// <summary>
		/// Get the text of the file located at a specified file URI, or null if the URI is non-file or the file is not found.
		/// </summary>
		// Token: 0x060005A5 RID: 1445 RVA: 0x000337A4 File Offset: 0x000319A4
		private static string GetPACFileText(string sURI)
		{
			string result;
			try
			{
				Uri oURI = new Uri(sURI);
				if (!oURI.IsFile)
				{
					result = null;
				}
				else
				{
					string sFilename = oURI.LocalPath;
					if (!File.Exists(sFilename))
					{
						FiddlerApplication.Log.LogFormat("! Failed to find the configured PAC script '{0}'", new object[] { sFilename });
						result = null;
					}
					else
					{
						result = File.ReadAllText(sFilename);
					}
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("! Failed to host the configured PAC script {0}", new object[] { eX });
				result = null;
			}
			return result;
		}

		// Token: 0x060005A6 RID: 1446 RVA: 0x00033828 File Offset: 0x00031A28
		internal AutoProxy(bool bAutoDiscover, string sAutoConfigUrl)
		{
			this._bUseAutoDiscovery = bAutoDiscover;
			if (!string.IsNullOrEmpty(sAutoConfigUrl))
			{
				if (sAutoConfigUrl.OICStartsWith("file:") || sAutoConfigUrl.StartsWith("\\\\") || (sAutoConfigUrl.Length > 2 && sAutoConfigUrl[1] == ':'))
				{
					Proxy.sUpstreamPACScript = AutoProxy.GetPACFileText(sAutoConfigUrl);
					if (!string.IsNullOrEmpty(Proxy.sUpstreamPACScript))
					{
						FiddlerApplication.Log.LogFormat("!WARNING: System proxy was configured to use a file-protocol sourced script ({0}). Proxy scripts delivered by the file protocol are not supported by many clients. Please see http://blogs.msdn.com/b/ieinternals/archive/2013/10/11/web-proxy-configuration-and-ie11-changes.aspx for more information.", new object[] { sAutoConfigUrl });
						sAutoConfigUrl = "http://" + CONFIG.sFiddlerListenHostPort + "/UpstreamProxy.pac";
					}
				}
				this._sPACScriptLocation = sAutoConfigUrl;
			}
			bool autoProxyRunInProcess = FiddlerApplication.Prefs.GetBoolPref("fiddler.network.gateway.DetermineInProcess", false);
			this.autoProxy = this.platformExtensions.CreateAutoProxy(bAutoDiscover, sAutoConfigUrl, autoProxyRunInProcess, CONFIG.bAutoProxyLogon);
		}

		// Token: 0x060005A7 RID: 1447 RVA: 0x00033914 File Offset: 0x00031B14
		~AutoProxy()
		{
			this.Dispose(false);
		}

		/// <summary>
		/// Returns a string containing the currently selected autoproxy options
		/// </summary>
		/// <returns></returns>
		// Token: 0x060005A8 RID: 1448 RVA: 0x00033944 File Offset: 0x00031B44
		public override string ToString()
		{
			string sResult = null;
			if (this.iAutoProxySuccessCount < 0)
			{
				sResult = "\tOffline/disabled\n";
			}
			else
			{
				if (this._bUseAutoDiscovery)
				{
					string sURI = this.GetWPADUrl();
					if (string.IsNullOrEmpty(sURI))
					{
						sURI = "Not detected";
					}
					sResult = string.Format("\tWPAD: {0}\n", sURI);
				}
				if (this._sPACScriptLocation != null)
				{
					sResult = sResult + "\tConfig script: " + this._sPACScriptLocation + "\n";
				}
			}
			return sResult ?? "\tDisabled";
		}

		/// <summary>
		/// Get WPAD-discovered URL for display purposes (e.g. Help&gt; About); note that we don't actually use this when determining the gateway,
		/// instead relying on the this.autoProxy.TryGetProxyForUrl method to do this work for us.
		/// </summary>
		/// <returns>A WPAD url, if found, or String.Empty</returns>
		// Token: 0x060005A9 RID: 1449 RVA: 0x000339B8 File Offset: 0x00031BB8
		internal string GetWPADUrl()
		{
			if (this.disposed)
			{
				return null;
			}
			string result;
			try
			{
				this.disposedLock.EnterReadLock();
				string wpadUrl;
				if (this.disposed)
				{
					result = null;
				}
				else if (this.autoProxy.TryGetPacUrl(out wpadUrl))
				{
					result = wpadUrl;
				}
				else
				{
					result = null;
				}
			}
			finally
			{
				this.disposedLock.ExitReadLock();
			}
			return result;
		}

		/// <summary>
		/// Return gateway endpoint for requested Url. TODO: Add caching layer on our side? TODO: Support multiple results?
		/// </summary>
		/// <param name="sUrl">The URL for which the gateway should be determined</param>
		/// <param name="ipepResult">The Endpoint of the Gateway, or null</param>
		/// <returns>TRUE if WinHttpGetProxyForUrl succeeded</returns>
		// Token: 0x060005AA RID: 1450 RVA: 0x00033A20 File Offset: 0x00031C20
		public bool GetAutoProxyForUrl(string sUrl, out IPEndPoint ipepResult)
		{
			if (this.disposed)
			{
				ipepResult = null;
				return false;
			}
			string proxy;
			string errorMessage;
			bool success;
			try
			{
				this.disposedLock.EnterReadLock();
				if (this.disposed)
				{
					ipepResult = null;
					return false;
				}
				success = this.autoProxy.TryGetProxyForUrl(sUrl, out proxy, out errorMessage);
			}
			finally
			{
				this.disposedLock.ExitReadLock();
			}
			if (success)
			{
				ipepResult = Utilities.IPEndPointFromHostPortString(proxy);
				if (ipepResult == null)
				{
					FiddlerApplication.Log.LogFormat("Proxy Configuration Script specified an unreachable proxy: {0} for URL: {1}", new object[] { proxy, sUrl });
				}
				return true;
			}
			if (!string.IsNullOrEmpty(errorMessage))
			{
				FiddlerApplication._Log.LogString("Fiddler.Network.AutoProxy> " + errorMessage);
			}
			ipepResult = null;
			return false;
		}

		/// <summary>
		/// Dispose AutoProxy.
		/// </summary>
		// Token: 0x060005AB RID: 1451 RVA: 0x00033ADC File Offset: 0x00031CDC
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060005AC RID: 1452 RVA: 0x00033AEC File Offset: 0x00031CEC
		protected virtual void Dispose(bool disposing)
		{
			if (this.disposed)
			{
				return;
			}
			try
			{
				this.disposedLock.EnterUpgradeableReadLock();
				if (this.disposed)
				{
					return;
				}
				try
				{
					this.disposedLock.EnterWriteLock();
					this.disposed = true;
				}
				finally
				{
					this.disposedLock.ExitWriteLock();
				}
			}
			finally
			{
				this.disposedLock.ExitUpgradeableReadLock();
			}
			if (disposing)
			{
				this.autoProxy.Dispose();
				while (this.disposedLock.WaitingReadCount > 0 || this.disposedLock.WaitingUpgradeCount > 0 || this.disposedLock.WaitingWriteCount > 0 || this.disposedLock.IsReadLockHeld || this.disposedLock.IsUpgradeableReadLockHeld || this.disposedLock.IsWriteLockHeld)
				{
				}
				this.disposedLock.Dispose();
			}
		}

		// Token: 0x0400029D RID: 669
		private readonly IPlatformExtensions platformExtensions = PlatformExtensionsFactory.Instance.CreatePlatformExtensions();

		// Token: 0x0400029E RID: 670
		private readonly IAutoProxy autoProxy;

		/// <summary>
		/// Indication as to whether AutoProxy information is valid. 0=Unknown/Enabled; 1=Valid/Enabled; -1=Invalid/Disabled
		/// </summary>
		// Token: 0x0400029F RID: 671
		internal int iAutoProxySuccessCount;

		// Token: 0x040002A0 RID: 672
		private readonly string _sPACScriptLocation;

		// Token: 0x040002A1 RID: 673
		private readonly bool _bUseAutoDiscovery = true;

		// Token: 0x040002A2 RID: 674
		private volatile bool disposed;

		// Token: 0x040002A3 RID: 675
		private readonly ReaderWriterLockSlim disposedLock = new ReaderWriterLockSlim();
	}
}
