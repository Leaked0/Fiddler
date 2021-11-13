using System;

namespace FiddlerCore.PlatformExtensions.API
{
	/// <summary>
	/// Implement this interface to handle upstream gateways when the client is configured to use WPAD or a Proxy AutoConfig (PAC) script.
	/// </summary>
	// Token: 0x020000A3 RID: 163
	internal interface IAutoProxy : IDisposable
	{
		/// <summary>
		/// Outs the <paramref name="proxy" /> for the requested <paramref name="url" />.
		/// </summary>
		/// <param name="url">The URL for which the <paramref name="proxy" /> should be determined.</param>
		/// <param name="proxy">One or more of the following strings separated by semicolons.
		/// ([&lt;scheme&gt;=][&lt;scheme&gt;"://"]&lt;server&gt;[":"&lt;port&gt;])</param>
		/// <param name="errorMessage">If the method fails this parameter should contain the error message, null otherwise.</param>
		/// <returns>True if the method succeeds, false otherwise.</returns>
		// Token: 0x06000667 RID: 1639
		bool TryGetProxyForUrl(string url, out string proxy, out string errorMessage);

		/// <summary>
		/// Outs WPAD-discovered URL of the Proxy Auto-Config file.
		/// </summary>
		/// <param name="pacUrl">The Proxy Auto-Config URL.</param>
		/// <returns>True if the method succeeds, false otherwise.</returns>
		// Token: 0x06000668 RID: 1640
		bool TryGetPacUrl(out string pacUrl);
	}
}
