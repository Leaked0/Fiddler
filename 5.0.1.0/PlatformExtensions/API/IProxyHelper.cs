using System;

namespace FiddlerCore.PlatformExtensions.API
{
	/// <summary>
	/// Implement this interface, in order to provide FiddlerCore with platform-specific proxy helper.
	/// This interface contains members used to manipulate proxy settings.
	/// </summary>
	// Token: 0x020000A6 RID: 166
	internal interface IProxyHelper
	{
		/// <summary>
		/// Configures the current process to use no proxy.
		/// </summary>
		// Token: 0x0600067E RID: 1662
		void DisableProxyForCurrentProcess();

		/// <summary>
		/// Returns the current process' proxy settings.
		/// </summary>
		/// <returns>String containing a HEX view of the current process' proxy settings.</returns>
		// Token: 0x0600067F RID: 1663
		string GetProxyForCurrentProcessAsHexView();

		/// <summary>
		/// Configures current process' proxy settings to default.
		/// </summary>
		// Token: 0x06000680 RID: 1664
		void ResetProxyForCurrentProcess();

		/// <summary>
		/// Configures current process' proxy settings.
		/// </summary>
		/// <param name="proxy">The proxy information (IP and port). It can be per connection type
		/// (e.g. http=127.0.0.1:8080;https=127.0.0.1:444) or global (e.g. 127.0.0.1:8888).</param>
		/// <param name="bypassList">Semi-colon delimted list of hosts to bypass proxy
		/// (e.g. www.google.com;www.microsoft.com)</param>
		// Token: 0x06000681 RID: 1665
		void SetProxyForCurrentProcess(string proxy, string bypassList);
	}
}
