using System;
using System.Security.Cryptography.X509Certificates;

namespace Fiddler
{
	// Token: 0x02000014 RID: 20
	internal static class CertInfo
	{
		// Token: 0x060000E6 RID: 230 RVA: 0x0000EA77 File Offset: 0x0000CC77
		internal static string GetSubjectAltNames(X509Certificate2 cert)
		{
			if (cert.Extensions["2.5.29.17"] != null)
			{
				return cert.Extensions["2.5.29.17"].Format(true);
			}
			return null;
		}

		// Token: 0x04000053 RID: 83
		private const string szOID_SUBJECT_ALT_NAME2 = "2.5.29.17";
	}
}
