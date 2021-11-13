using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using BCCertMaker;

namespace Fiddler
{
	/// <summary>
	/// This class is used to find and create certificates for use in HTTPS interception. 
	/// The default implementation (DefaultCertProvider object) uses the Windows Certificate store, 
	/// but if a plugin ICertificateProvider is provided, it is used instead.
	/// </summary>
	// Token: 0x02000018 RID: 24
	public class CertMaker
	{
		// Token: 0x060000F3 RID: 243 RVA: 0x0000EFB4 File Offset: 0x0000D1B4
		public static string GetCertProviderInfo()
		{
			CertMaker.EnsureReady();
			if (CertMaker.oCertProvider is DefaultCertificateProvider)
			{
				return ((DefaultCertificateProvider)CertMaker.oCertProvider).GetEngineString();
			}
			string sPath = CertMaker.oCertProvider.GetType().Assembly.Location;
			string sAppPath = CONFIG.GetPath("App");
			if (sPath.StartsWith(sAppPath))
			{
				sPath = sPath.Substring(sAppPath.Length);
			}
			return string.Format("{0} from {1}", CertMaker.oCertProvider, sPath);
		}

		/// <summary>
		/// Ensures that the Certificate Generator is ready; thread-safe
		/// </summary>
		// Token: 0x060000F4 RID: 244 RVA: 0x0000F028 File Offset: 0x0000D228
		public static void EnsureReady()
		{
			if (CertMaker.oCertProvider != null)
			{
				return;
			}
			object lockProvider = CertMaker._lockProvider;
			lock (lockProvider)
			{
				if (CertMaker.oCertProvider == null)
				{
					CertMaker.oCertProvider = CertMaker.LoadOverrideCertProvider() ?? new BCCertMaker();
				}
			}
		}

		/// <summary>
		/// Load a delegate Certificate Provider
		/// </summary>
		/// <returns>The provider, or null</returns>
		// Token: 0x060000F5 RID: 245 RVA: 0x0000F088 File Offset: 0x0000D288
		private static ICertificateProvider LoadOverrideCertProvider()
		{
			string sFile = FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.assembly", CONFIG.GetPath("App") + "CertMaker.dll");
			Assembly a;
			try
			{
				if (!File.Exists(sFile))
				{
					FiddlerApplication.Log.LogFormat("Assembly '{0}' was not found. Using default Certificate Generator.", new object[] { sFile });
					return null;
				}
				a = Assembly.UnsafeLoadFrom(sFile);
				if (!Utilities.FiddlerMeetsVersionRequirement(a, "Certificate Makers"))
				{
					FiddlerApplication.Log.LogFormat("Assembly '{0}' did not specify a RequiredVersionAttribute. Aborting load of Certificate Generation module.", new object[] { sFile });
					return null;
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("Failed to load CertMaker from '{0}' due to '{1}'.", new object[] { sFile, eX.Message });
				return null;
			}
			foreach (Type t in a.GetExportedTypes())
			{
				if (t.IsClass && !t.IsAbstract && t.IsPublic && typeof(ICertificateProvider).IsAssignableFrom(t))
				{
					try
					{
						return (ICertificateProvider)Activator.CreateInstance(t);
					}
					catch (Exception eX2)
					{
						string title = "Load Error";
						string message = string.Format("[Fiddler] Failure loading '{0}' CertMaker from {1}: {2}\n\n{3}\n\n{4}", new object[] { t.Name, sFile, eX2.Message, eX2.StackTrace, eX2.InnerException });
						FiddlerApplication.Log.LogFormat("{0}: {1}", new object[] { title, message });
					}
				}
			}
			FiddlerApplication.Log.LogFormat("Assembly '{0}' did not contain a recognized ICertificateProvider.", new object[] { sFile });
			return null;
		}

		/// <summary>
		/// Removes Fiddler-generated certificates from the Windows certificate store
		/// </summary>
		// Token: 0x060000F6 RID: 246 RVA: 0x0000F254 File Offset: 0x0000D454
		public static bool removeFiddlerGeneratedCerts()
		{
			return CertMaker.removeFiddlerGeneratedCerts(true);
		}

		/// <summary>
		/// Removes Fiddler-generated certificates from the Windows certificate store
		/// </summary>
		/// <param name="bRemoveRoot">Indicates whether Root certificates should also be cleaned up</param>
		// Token: 0x060000F7 RID: 247 RVA: 0x0000F25C File Offset: 0x0000D45C
		public static bool removeFiddlerGeneratedCerts(bool bRemoveRoot)
		{
			CertMaker.EnsureReady();
			if (CertMaker.oCertProvider is ICertificateProvider2)
			{
				return (CertMaker.oCertProvider as ICertificateProvider2).ClearCertificateCache(bRemoveRoot);
			}
			return CertMaker.oCertProvider.ClearCertificateCache();
		}

		/// <summary>
		/// Returns the Root certificate that Fiddler uses to generate per-site certificates used for HTTPS interception.
		/// </summary>
		/// <returns>Returns the root certificate, if present, or null if the root certificate does not exist.</returns>
		// Token: 0x060000F8 RID: 248 RVA: 0x0000F28A File Offset: 0x0000D48A
		public static X509Certificate2 GetRootCertificate()
		{
			CertMaker.EnsureReady();
			return CertMaker.oCertProvider.GetRootCertificate();
		}

		/// <summary>
		/// Return the raw byte[]s of the root certificate, or null
		/// </summary>
		/// <returns></returns>
		// Token: 0x060000F9 RID: 249 RVA: 0x0000F29C File Offset: 0x0000D49C
		internal static byte[] getRootCertBytes()
		{
			X509Certificate2 oRoot = CertMaker.GetRootCertificate();
			if (oRoot == null)
			{
				return null;
			}
			return oRoot.Export(X509ContentType.Cert);
		}

		// Token: 0x060000FA RID: 250 RVA: 0x0000F2BC File Offset: 0x0000D4BC
		internal static bool exportRootToDesktop()
		{
			try
			{
				byte[] arrRoot = CertMaker.getRootCertBytes();
				if (arrRoot != null)
				{
					File.WriteAllBytes(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + Path.DirectorySeparatorChar.ToString() + "FiddlerRoot.cer", arrRoot);
					return true;
				}
				string title = "Export Failed";
				string message = "The root certificate could not be located.";
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[] { title, message });
			}
			catch (Exception eX)
			{
				string title2 = "Certificate Export Failed";
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[]
				{
					title2,
					eX.ToString()
				});
			}
			return false;
		}

		/// <summary>
		/// Request a certificate with the specified SubjectCN
		/// </summary>
		/// <param name="sHostname">A string of the form: "www.hostname.com"</param>
		/// <returns>A certificate or /null/ if the certificate could not be found or created</returns>
		// Token: 0x060000FB RID: 251 RVA: 0x0000F364 File Offset: 0x0000D564
		public static X509Certificate2 FindCert(string sHostname)
		{
			CertMaker.EnsureReady();
			return CertMaker.oCertProvider.GetCertificateForHost(sHostname);
		}

		/// <summary>
		/// Pre-cache a Certificate in the Certificate Maker that should be returned in subsequent calls to FindCert
		/// </summary>
		/// <param name="sHost">The hostname for which this certificate should be returned.</param>
		/// <param name="oCert">The X509Certificate2 with attached Private Key</param>
		/// <returns>TRUE if the Certificate Provider succeeded in pre-caching the certificate. FALSE if Provider doesn't support pre-caching. THROWS if supplied Certificate lacks Private Key.</returns>
		// Token: 0x060000FC RID: 252 RVA: 0x0000F384 File Offset: 0x0000D584
		public static bool StoreCert(string sHost, X509Certificate2 oCert)
		{
			if (!oCert.HasPrivateKey)
			{
				throw new ArgumentException("The provided certificate MUST have a private key.", "oCert");
			}
			CertMaker.EnsureReady();
			ICertificateProvider3 oCP = CertMaker.oCertProvider as ICertificateProvider3;
			return oCP != null && oCP.CacheCertificateForHost(sHost, oCert);
		}

		/// <summary>
		/// Pre-cache a Certificate in the Certificate Maker that should be returned in subsequent calls to FindCert
		/// </summary>
		/// <param name="sHost">The hostname for which this certificate should be returned.</param>
		/// <param name="sPFXFilename">The filename of the PFX file containing the certificate and private key</param>
		/// <param name="sPFXPassword">The password for the PFX file</param>
		/// <notes>Throws if the Certificate Provider failed to pre-cache the certificate</notes>
		// Token: 0x060000FD RID: 253 RVA: 0x0000F3C8 File Offset: 0x0000D5C8
		public static void StoreCert(string sHost, string sPFXFilename, string sPFXPassword)
		{
			X509Certificate2 oCert = new X509Certificate2(sPFXFilename, sPFXPassword);
			if (!CertMaker.StoreCert(sHost, oCert))
			{
				throw new InvalidOperationException("The current ICertificateProvider does not support storing custom certificates.");
			}
		}

		/// <summary>
		/// Determine if the self-signed root certificate exists
		/// </summary>
		/// <returns>True if the Root certificate returned from <see cref="M:Fiddler.CertMaker.GetRootCertificate">GetRootCertificate</see> is non-null, False otherwise.</returns>
		// Token: 0x060000FE RID: 254 RVA: 0x0000F3F4 File Offset: 0x0000D5F4
		public static bool rootCertExists()
		{
			bool result;
			try
			{
				X509Certificate2 oRoot = CertMaker.GetRootCertificate();
				result = oRoot != null;
			}
			catch (Exception eX)
			{
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Is Fiddler's root certificate in the Root store?
		/// </summary>
		/// <returns>TRUE if so</returns>
		// Token: 0x060000FF RID: 255 RVA: 0x0000F424 File Offset: 0x0000D624
		public static bool rootCertIsTrusted()
		{
			CertMaker.EnsureReady();
			bool bUserTrusted;
			bool bMachineTrusted;
			return CertMaker.oCertProvider.rootCertIsTrusted(out bUserTrusted, out bMachineTrusted);
		}

		/// <summary>
		/// Is Fiddler's root certificate in the Machine Root store?
		/// </summary>
		/// <returns>TRUE if so</returns>
		// Token: 0x06000100 RID: 256 RVA: 0x0000F444 File Offset: 0x0000D644
		public static bool rootCertIsMachineTrusted()
		{
			CertMaker.EnsureReady();
			bool bUserTrusted;
			bool bMachineTrusted;
			CertMaker.oCertProvider.rootCertIsTrusted(out bUserTrusted, out bMachineTrusted);
			return bMachineTrusted;
		}

		/// <summary>
		/// Create a self-signed root certificate to use as the trust anchor for HTTPS interception certificate chains
		/// </summary>
		/// <returns>TRUE if successful</returns>
		// Token: 0x06000101 RID: 257 RVA: 0x0000F466 File Offset: 0x0000D666
		public static bool createRootCert()
		{
			CertMaker.EnsureReady();
			return CertMaker.oCertProvider.CreateRootCertificate();
		}

		/// <summary>
		/// Finds the Fiddler root certificate and prompts the user to add it to the TRUSTED store.
		/// Note: The system certificate store is used by most applications (IE, Chrome, etc) but not
		/// all; for instance, Firefox uses its own certificate store.
		/// </summary>
		/// <returns>True if successful</returns>
		// Token: 0x06000102 RID: 258 RVA: 0x0000F477 File Offset: 0x0000D677
		public static bool trustRootCert()
		{
			CertMaker.EnsureReady();
			return CertMaker.oCertProvider.TrustRootCertificate();
		}

		// Token: 0x06000103 RID: 259 RVA: 0x0000F488 File Offset: 0x0000D688
		internal static bool flushCertCache()
		{
			CertMaker.EnsureReady();
			ICertificateProvider2 oCP = CertMaker.oCertProvider as ICertificateProvider2;
			return oCP != null && oCP.ClearCertificateCache(false);
		}

		/// <summary>
		/// Dispose of the Certificate Provider, if any.
		/// </summary>
		// Token: 0x06000104 RID: 260 RVA: 0x0000F4B4 File Offset: 0x0000D6B4
		public static void DoDispose()
		{
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.CertMaker.CleanupServerCertsOnExit", false))
			{
				CertMaker.removeFiddlerGeneratedCerts(false);
			}
			IDisposable oDP = CertMaker.oCertProvider as IDisposable;
			if (oDP != null)
			{
				oDP.Dispose();
			}
			CertMaker.oCertProvider = null;
		}

		/// <summary>
		/// Enables specification of a delegate certificate provider that generates certificates for HTTPS interception.
		/// </summary>
		// Token: 0x04000058 RID: 88
		public static ICertificateProvider oCertProvider = null;

		/// <summary>
		/// Lock on this object when TestExistenceOf/Create oCertProvider
		/// </summary>
		// Token: 0x04000059 RID: 89
		private static object _lockProvider = new object();
	}
}
