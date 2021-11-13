using System;

namespace Fiddler
{
	/// <summary>
	/// Type of Upstream Gateway
	/// </summary>
	// Token: 0x02000011 RID: 17
	[Obsolete("Use Telerik.NetworkConnections.NetworkConnectionsManager.GetCurrentProxySettingsForConnection(*) to get information for the current proxy settings.")]
	public enum GatewayType : byte
	{
		/// <summary>
		/// Traffic should be sent directly to the server
		/// </summary>
		// Token: 0x04000046 RID: 70
		None,
		/// <summary>
		/// Traffic should be sent to a manually-specified proxy
		/// </summary>
		// Token: 0x04000047 RID: 71
		Manual,
		/// <summary>
		/// Traffic should be sent to the System-configured proxy
		/// </summary>
		// Token: 0x04000048 RID: 72
		System,
		/// <summary>
		/// Proxy should be automatically detected
		/// </summary>
		// Token: 0x04000049 RID: 73
		WPAD
	}
}
