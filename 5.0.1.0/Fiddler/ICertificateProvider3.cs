using System;
using System.Security.Cryptography.X509Certificates;

namespace Fiddler
{
	// Token: 0x02000028 RID: 40
	public interface ICertificateProvider3 : ICertificateProvider2, ICertificateProvider
	{
		/// <summary>
		/// Call this function to cache a certificate in the Certificate Provider
		/// </summary>
		/// <param name="sHost">The hostname to match</param>
		/// <param name="oCert">The certificate that the Provider should later provide when GetCertificateForHost is called</param>
		/// <returns>True if the request was successful</returns>
		// Token: 0x060001A5 RID: 421
		bool CacheCertificateForHost(string sHost, X509Certificate2 oCert);
	}
}
