using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Fiddler
{
	/// <summary>
	/// These EventArgs are passed to the FiddlerApplication.OnValidateServerCertificate event handler when a server-provided HTTPS certificate is evaluated
	/// </summary>
	// Token: 0x02000039 RID: 57
	public class ValidateServerCertificateEventArgs : EventArgs
	{
		/// <summary>
		/// EventArgs for the ValidateServerCertificateEvent that allows host to override default certificate handling policy
		/// </summary>
		/// <param name="inSession">The session</param>
		/// <param name="inExpectedCN">The CN expected for this session</param>
		/// <param name="inServerCertificate">The certificate provided by the server</param>
		/// <param name="inServerCertificateChain">The certificate chain of that certificate</param>
		/// <param name="inSslPolicyErrors">Errors from default validation</param>
		// Token: 0x0600024B RID: 587 RVA: 0x000163EA File Offset: 0x000145EA
		internal ValidateServerCertificateEventArgs(Session inSession, string inExpectedCN, X509Certificate inServerCertificate, X509Chain inServerCertificateChain, SslPolicyErrors inSslPolicyErrors)
		{
			this._oSession = inSession;
			this._sExpectedCN = inExpectedCN;
			this._oServerCertificate = inServerCertificate;
			this._ServerCertificateChain = inServerCertificateChain;
			this._sslPolicyErrors = inSslPolicyErrors;
		}

		/// <summary>
		/// The port to which this request was targeted
		/// </summary>
		// Token: 0x17000068 RID: 104
		// (get) Token: 0x0600024C RID: 588 RVA: 0x00016417 File Offset: 0x00014617
		public int TargetPort
		{
			get
			{
				return this._oSession.port;
			}
		}

		/// <summary>
		/// The SubjectCN (e.g. Hostname) that should be expected on this HTTPS connection, based on the request's Host property.
		/// </summary>
		// Token: 0x17000069 RID: 105
		// (get) Token: 0x0600024D RID: 589 RVA: 0x00016424 File Offset: 0x00014624
		public string ExpectedCN
		{
			get
			{
				return this._sExpectedCN;
			}
		}

		/// <summary>
		/// The Session for which a HTTPS certificate was received.
		/// </summary>
		// Token: 0x1700006A RID: 106
		// (get) Token: 0x0600024E RID: 590 RVA: 0x0001642C File Offset: 0x0001462C
		public Session Session
		{
			get
			{
				return this._oSession;
			}
		}

		/// <summary>
		/// The server's certificate chain.
		/// </summary>
		// Token: 0x1700006B RID: 107
		// (get) Token: 0x0600024F RID: 591 RVA: 0x00016434 File Offset: 0x00014634
		public X509Chain ServerCertificateChain
		{
			get
			{
				return this._ServerCertificateChain;
			}
		}

		/// <summary>
		/// The SslPolicyErrors found during default certificate evaluation.
		/// </summary>
		// Token: 0x1700006C RID: 108
		// (get) Token: 0x06000250 RID: 592 RVA: 0x0001643C File Offset: 0x0001463C
		public SslPolicyErrors CertificatePolicyErrors
		{
			get
			{
				return this._sslPolicyErrors;
			}
		}

		/// <summary>
		/// Set this property to override the certificate validity
		/// </summary>
		// Token: 0x1700006D RID: 109
		// (get) Token: 0x06000251 RID: 593 RVA: 0x00016444 File Offset: 0x00014644
		// (set) Token: 0x06000252 RID: 594 RVA: 0x0001644C File Offset: 0x0001464C
		public CertificateValidity ValidityState { get; set; }

		/// <summary>
		/// The X509Certificate provided by the server to vouch for its authenticity
		/// </summary>
		// Token: 0x1700006E RID: 110
		// (get) Token: 0x06000253 RID: 595 RVA: 0x00016455 File Offset: 0x00014655
		public X509Certificate ServerCertificate
		{
			get
			{
				return this._oServerCertificate;
			}
		}

		// Token: 0x04000104 RID: 260
		private readonly X509Certificate _oServerCertificate;

		// Token: 0x04000105 RID: 261
		private readonly string _sExpectedCN;

		// Token: 0x04000106 RID: 262
		private readonly Session _oSession;

		// Token: 0x04000107 RID: 263
		private readonly X509Chain _ServerCertificateChain;

		// Token: 0x04000108 RID: 264
		private readonly SslPolicyErrors _sslPolicyErrors;
	}
}
