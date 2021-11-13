using System;

namespace FiddlerCore.PlatformExtensions.API
{
	/// <summary>
	/// Implement this interface in order to provide FiddlerCore with Windows-specific functionality.
	/// </summary>
	// Token: 0x020000A7 RID: 167
	internal interface IWindowsSpecificPlatformExtensions : IPlatformExtensions
	{
		/// <summary>
		/// Gets a WinINet helper, which can be used to access WinINet native API.
		/// </summary>
		// Token: 0x17000100 RID: 256
		// (get) Token: 0x06000682 RID: 1666
		IWinINetHelper WinINetHelper { get; }
	}
}
