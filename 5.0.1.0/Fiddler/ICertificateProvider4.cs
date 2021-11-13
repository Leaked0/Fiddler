using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using FiddlerCore.Utilities.SmartAssembly.Attributes;

namespace Fiddler
{
	// Token: 0x02000029 RID: 41
	[DoNotObfuscateType]
	public interface ICertificateProvider4 : ICertificateProvider3, ICertificateProvider2, ICertificateProvider
	{
		/// <summary>
		/// Copy of the cache of the EndEntity certificates that have been generated in this session.
		/// </summary>
		// Token: 0x17000057 RID: 87
		// (get) Token: 0x060001A6 RID: 422
		IDictionary<string, X509Certificate2> CertCache { get; }
	}
}
