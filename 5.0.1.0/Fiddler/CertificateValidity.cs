using System;

namespace Fiddler
{
	/// <summary>
	/// Enumeration of possible responses specified by the ValidateServerCertificateEventArgs as modified by FiddlerApplication's <see cref="E:Fiddler.FiddlerApplication.OnValidateServerCertificate">OnValidateServerCertificate event</see>  
	/// </summary>
	// Token: 0x02000038 RID: 56
	public enum CertificateValidity
	{
		/// <summary>
		/// The certificate will be considered valid if CertificatePolicyErrors == SslPolicyErrors.None, otherwise the certificate will be invalid unless the user manually allows the certificate.
		/// </summary>
		// Token: 0x04000100 RID: 256
		Default,
		/// <summary>
		/// The certificate will be confirmed with the user even if CertificatePolicyErrors == SslPolicyErrors.None.
		/// Note: FiddlerCore does not support user-prompting and will always treat this status as ForceInvalid.
		/// </summary>
		// Token: 0x04000101 RID: 257
		ConfirmWithUser,
		/// <summary>
		/// Force the certificate to be considered Invalid, regardless of the value of CertificatePolicyErrors.
		/// </summary>
		// Token: 0x04000102 RID: 258
		ForceInvalid,
		/// <summary>
		/// Force the certificate to be considered Valid, regardless of the value of CertificatePolicyErrors.
		/// </summary>
		// Token: 0x04000103 RID: 259
		ForceValid
	}
}
