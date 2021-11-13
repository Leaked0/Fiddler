using System;
using System.IO;
using FiddlerCore.Utilities.SmartAssembly.Attributes;

namespace Fiddler
{
	// Token: 0x0200002A RID: 42
	[DoNotObfuscateType]
	public interface ICertificateProvider5 : ICertificateProvider4, ICertificateProvider3, ICertificateProvider2, ICertificateProvider
	{
		/// <summary>
		/// When this method is called, your extension should read the root certificate and its private key from a stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="password">The password which is used to protect the private key. Could be null or empty if the private key is not protected.</param>
		/// <param name="alias">The alias for the certificate and the private key. Could be null.</param>
		// Token: 0x060001A7 RID: 423
		void ReadRootCertificateAndPrivateKeyFromStream(Stream stream, string password, string alias = null);

		/// <summary>
		/// When this method is called, your extension should write the root certificate and its private key to a stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="password">The password protecting the private key. If null or empty, the private key is written unprotected.</param>
		/// <param name="alias">The alias for the certificate and the private key. If null, a random alias could be created.</param>
		// Token: 0x060001A8 RID: 424
		void WriteRootCertificateAndPrivateKeyToStream(Stream stream, string password, string alias = null);

		/// <summary>
		/// When this method is called, your extension should write the root certificate without the private key to a stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		// Token: 0x060001A9 RID: 425
		void WriteRootCertificateToStream(Stream stream);

		/// <summary>
		/// When this method is called, your extension should read the root certificate and its private key from the PKCS#12 file(.pfx | .p12).
		/// </summary>
		/// <param name="filename">The filename of the PKCS#12 file (.pfx | .p12).</param>
		/// <param name="password">The password which is used to protect the private key. Could be null or empty if the private key is not protected.</param>
		/// <param name="alias">The alias for the certificate and the private key. Could be null.</param>
		// Token: 0x060001AA RID: 426
		void ReadRootCertificateAndPrivateKeyFromPkcs12File(string filename, string password, string alias = null);

		/// <summary>
		/// When this method is called, your extension should write the root certificate and its private key to a PKCS#12 file(.pfx | .p12).
		/// </summary>
		/// <param name="filename">The filename of the PKCS#12 file (.pfx | .p12).</param>
		/// <param name="password">The password which is used to protect the private key. If null or empty, the private key is written unprotected.</param>
		/// <param name="alias">The alias for the certificate and the private key. If null, a random alias could be created.</param>
		// Token: 0x060001AB RID: 427
		void WriteRootCertificateAndPrivateKeyToPkcs12File(string filename, string password, string alias = null);

		/// <summary>
		/// When this method is called, your extension should write the root certificate without the private key to a DER encoded file(.cer | .crt | .der).
		/// </summary>
		/// <param name="filename">The filename of the DER encoded file (.cer | .crt | .der)</param>
		// Token: 0x060001AC RID: 428
		void WriteRootCertificateToDerEncodedFile(string filename);
	}
}
