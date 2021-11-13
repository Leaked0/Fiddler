using System;

namespace Fiddler
{
	// Token: 0x0200002B RID: 43
	public interface ICertificateProviderInfo
	{
		/// <summary>
		/// Return a string describing the current configuration of the Certificate Provider. For instance, list
		/// the configured key size, hash algorithms, etc.
		/// </summary>
		// Token: 0x060001AD RID: 429
		string GetConfigurationString();
	}
}
