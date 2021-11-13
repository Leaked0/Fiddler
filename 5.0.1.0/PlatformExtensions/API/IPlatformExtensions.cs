using System;

namespace FiddlerCore.PlatformExtensions.API
{
	/// <summary>
	/// Implement this interface in order to provide FiddlerCore with platform specific functionality.
	/// </summary>
	// Token: 0x020000A4 RID: 164
	internal interface IPlatformExtensions
	{
		/// <summary>
		/// Map a local port number to the originating process ID.
		/// </summary>
		/// <param name="port">The port number.</param>
		/// <param name="includeIPv6">true to include processes using IPv6 addresses in the mapping.</param>
		/// <param name="processId">Contains the originating process ID if the operation is successful.</param>
		/// <param name="errorMessage">Contains an error message if the operation fails.</param>
		/// <returns>true if the operation is successful, false otherwise.</returns>
		// Token: 0x06000669 RID: 1641
		bool TryMapPortToProcessId(int port, bool includeIPv6, out int processId, out string errorMessage);

		/// <summary>
		/// Gets any process' name and ID which listens on a port.
		/// </summary>
		/// <param name="port">The port number.</param>
		/// <param name="processName">Contains the process name of a process if there is one listening on the port, otherwise contains an empty string.</param>
		/// <param name="processId">Contains the process ID of a process if there is one listening on the port, otherwise contains 0.</param>
		/// <param name="errorMessage">Contains an error message if the operation fails.</param>
		/// <returns>true if the operation is successful, false otherwise.</returns>
		// Token: 0x0600066A RID: 1642
		bool TryGetListeningProcessOnPort(int port, out string processName, out int processId, out string errorMessage);

		/// <summary>
		/// Changes system-wide timer's resolution.
		/// </summary>
		/// <param name="increase">true to increase the resolution for better accuracy of timestamps, false to decrease it to the default value for the system.</param>
		/// <returns>true if the operation is successful, false otherwise.</returns>
		// Token: 0x0600066B RID: 1643
		bool TryChangeTimersResolution(bool increase);

		/// <summary>
		/// Returns true if the system-wide timer's resolution is increased, false otherwise.
		/// </summary>
		// Token: 0x170000FE RID: 254
		// (get) Token: 0x0600066C RID: 1644
		bool HighResolutionTimersEnabled { get; }

		/// <summary>
		/// Gets a proxy helper, which can be used to manipulate proxy settings.
		/// </summary>
		// Token: 0x170000FF RID: 255
		// (get) Token: 0x0600066D RID: 1645
		IProxyHelper ProxyHelper { get; }

		/// <summary>
		/// This event is raised when a debug message is being spewed.
		/// </summary>
		// Token: 0x1400001B RID: 27
		// (add) Token: 0x0600066E RID: 1646
		// (remove) Token: 0x0600066F RID: 1647
		event EventHandler<MessageEventArgs> DebugSpew;

		/// <summary>
		/// This event is raised when an error has occured.
		/// </summary>
		// Token: 0x1400001C RID: 28
		// (add) Token: 0x06000670 RID: 1648
		// (remove) Token: 0x06000671 RID: 1649
		event EventHandler<MessageEventArgs> Error;

		/// <summary>
		/// This event is raised when a message is being logged.
		/// </summary>
		// Token: 0x1400001D RID: 29
		// (add) Token: 0x06000672 RID: 1650
		// (remove) Token: 0x06000673 RID: 1651
		event EventHandler<MessageEventArgs> Log;

		/// <summary>
		/// Decompresses a byte[] that is compressed with XPRESS.
		/// </summary>
		/// <param name="data">The compressed byte[].</param>
		/// <returns>The decompressed byte[].</returns>
		// Token: 0x06000674 RID: 1652
		byte[] DecompressXpress(byte[] data);

		/// <summary>
		/// This method is used to post-process the name of a process, in order to resolve it more accurately.
		/// </summary>
		/// <param name="pid">The ID of the process, whose name should be post-processed.</param>
		/// <param name="processName">The process name that should be post-processed.</param>
		/// <returns>The post-processed process name.</returns>
		// Token: 0x06000675 RID: 1653
		string PostProcessProcessName(int pid, string processName);

		/// <summary>
		/// This method is used to set the user-agent string for the current process.
		/// </summary>
		/// <param name="userAgent">The user-agent string.</param>
		// Token: 0x06000676 RID: 1654
		void SetUserAgentStringForCurrentProcess(string userAgent);

		/// <summary>
		/// This method is used to get the number of milliseconds since the system start.
		/// </summary>
		/// <param name="milliseconds">Contains the system uptime in milliseconds if the operation is successful.</param>
		/// <returns>true if the operation is successful, false otherwise.</returns>
		// Token: 0x06000677 RID: 1655
		bool TryGetUptimeInMilliseconds(out ulong milliseconds);

		/// <summary>
		/// Creates <see cref="T:FiddlerCore.PlatformExtensions.API.IAutoProxy" />.
		/// </summary>
		/// <param name="autoDiscover">True if the <see cref="T:FiddlerCore.PlatformExtensions.API.IAutoProxy" /> must use the WPAD protocol, false otherwise.</param>
		/// <param name="pacUrl">URL of the Proxy Auto-Config file. Can be null.</param>
		/// <param name="autoProxyRunInProcess">True if the WPAD processing should be done in the current process, false otherwise.</param>
		/// <param name="autoLoginIfChallenged">Specifies whether the client's domain credentials should be automatically sent
		/// in response to an NTLM or Negotiate Authentication challenge when the <see cref="T:FiddlerCore.PlatformExtensions.API.IAutoProxy" /> requests the PAC file.</param>
		/// <returns><see cref="T:FiddlerCore.PlatformExtensions.API.IAutoProxy" /></returns>
		// Token: 0x06000678 RID: 1656
		IAutoProxy CreateAutoProxy(bool autoDiscover, string pacUrl, bool autoProxyRunInProcess, bool autoLoginIfChallenged);

		/// <summary>
		/// Automatically imports and trusts the Fiddler Root Certificate in the OS Certificate Store.
		/// </summary>
		// Token: 0x06000679 RID: 1657
		void TrustRootCertificate();

		/// <summary>
		/// Removes the Fiddler Root Certificate from the OS Certificate Store.
		/// </summary>
		// Token: 0x0600067A RID: 1658
		void UntrustRootCertificate();

		/// <summary>
		/// Checks if Fiddler Root Certificate is imported and trusted in the OS Certificate Store.
		/// </summary>
		/// <returns>true in case the Certificate is trusted, false otherwise</returns>
		// Token: 0x0600067B RID: 1659
		bool IsRootCertificateTrusted();

		/// <summary>
		/// This method is used to get the parent process Id.
		/// </summary>
		/// <param name="childProcessId">Contains the child process Id.</param>
		/// <param name="errorMessage">Contains an error message if the operation fails.</param>
		/// <returns>the parent process Id if is successful, zero otherwise.</returns>
		// Token: 0x0600067C RID: 1660
		int GetParentProcessId(int childProcessId, out string errorMessage);
	}
}
