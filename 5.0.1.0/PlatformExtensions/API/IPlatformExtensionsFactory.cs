using System;

namespace FiddlerCore.PlatformExtensions.API
{
	/// <summary>
	/// Implement this interface in order to implement a factory, which is used to create <see cref="T:FiddlerCore.PlatformExtensions.API.IPlatformExtensions" /> objects.
	/// </summary>
	// Token: 0x020000A5 RID: 165
	internal interface IPlatformExtensionsFactory
	{
		/// <summary>
		/// Creates new <see cref="T:FiddlerCore.PlatformExtensions.API.IPlatformExtensions" /> object.
		/// </summary>
		/// <returns>The platform extensions object.</returns>
		// Token: 0x0600067D RID: 1661
		IPlatformExtensions CreatePlatformExtensions();
	}
}
