using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using FiddlerCore.Utilities;

namespace Fiddler
{
	/// <summary>
	/// [DEPRECATED] Use the BCCertMaker instead.
	/// This is the default Fiddler certificate provider.
	/// </summary>
	// Token: 0x0200001D RID: 29
	public class DefaultCertificateProvider : ICertificateProvider3, ICertificateProvider2, ICertificateProvider, ICertificateProviderInfo
	{
		// Token: 0x06000169 RID: 361 RVA: 0x00012C60 File Offset: 0x00010E60
		private void GetReaderLock()
		{
			this._oRWLock.EnterReadLock();
		}

		// Token: 0x0600016A RID: 362 RVA: 0x00012C6D File Offset: 0x00010E6D
		private void FreeReaderLock()
		{
			this._oRWLock.ExitReadLock();
		}

		// Token: 0x0600016B RID: 363 RVA: 0x00012C7A File Offset: 0x00010E7A
		private void GetWriterLock()
		{
			this._oRWLock.EnterWriteLock();
		}

		// Token: 0x0600016C RID: 364 RVA: 0x00012C87 File Offset: 0x00010E87
		private void FreeWriterLock()
		{
			this._oRWLock.ExitWriteLock();
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00012C94 File Offset: 0x00010E94
		public DefaultCertificateProvider()
		{
			bool bTriedCertEnroll = false;
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.PreferCertEnroll", true) && DefaultCertificateProvider.OSSupportsCertEnroll())
			{
				bTriedCertEnroll = true;
				this.CertCreator = DefaultCertificateProvider.CertEnrollEngine.GetEngine(this);
			}
			if (this.CertCreator == null)
			{
				this.CertCreator = DefaultCertificateProvider.MakeCertEngine.GetEngine();
			}
			if (this.CertCreator == null && !bTriedCertEnroll)
			{
				this.CertCreator = DefaultCertificateProvider.CertEnrollEngine.GetEngine(this);
			}
			if (this.CertCreator == null)
			{
				FiddlerApplication.Log.LogFormat("!Fiddler.CertMaker> Critical failure: No Certificate Creation engine could be created. Disabling HTTPS Decryption.", Array.Empty<object>());
				CONFIG.DecryptHTTPS = false;
				return;
			}
			this.UseWildcards = FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.UseWildcards", true);
			if (this.CertCreator.GetType() == typeof(DefaultCertificateProvider.MakeCertEngine))
			{
				this.UseWildcards = false;
			}
			if (CONFIG.bDebugCertificateGeneration)
			{
				FiddlerApplication.Log.LogFormat("/Fiddler.CertMaker> Using {0} for certificate generation; UseWildcards={1}.", new object[]
				{
					this.CertCreator.GetType().ToString(),
					this.UseWildcards
				});
			}
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00012DDE File Offset: 0x00010FDE
		private static bool OSSupportsCertEnroll()
		{
			return Environment.OSVersion.Version.Major > 6 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor > 0);
		}

		// Token: 0x0600016F RID: 367 RVA: 0x00012E1C File Offset: 0x0001101C
		internal string GetEngineString()
		{
			if (this.CertCreator.GetType() == typeof(DefaultCertificateProvider.CertEnrollEngine))
			{
				return "CertEnroll engine";
			}
			if (this.CertCreator.GetType() == typeof(DefaultCertificateProvider.MakeCertEngine))
			{
				return "MakeCert engine";
			}
			return "Unknown engine";
		}

		/// <summary>
		/// Find certificates that have the specified full subject.
		/// </summary>
		/// <param name="storeName">The store to search</param>
		/// <param name="sFullSubject">FindBySubject{Distinguished}Name requires a complete match of the SUBJECT, including CN, O, and OU</param>
		/// <returns>Matching certificates</returns>
		// Token: 0x06000170 RID: 368 RVA: 0x00012E74 File Offset: 0x00011074
		private static X509Certificate2Collection FindCertsBySubject(StoreName storeName, StoreLocation storeLocation, string sFullSubject)
		{
			X509Store certStore = new X509Store(storeName, storeLocation);
			X509Certificate2Collection result;
			try
			{
				certStore.Open(OpenFlags.OpenExistingOnly);
				X509Certificate2Collection certs = certStore.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, sFullSubject, false);
				result = certs;
			}
			finally
			{
				certStore.Close();
			}
			return result;
		}

		/// <summary>
		/// Find all certificates (in the CurrentUser Personal store) that have the specified issuer.
		/// </summary>
		/// <param name="storeName">The store to search</param>
		/// <param name="sFullIssuerSubject">FindByIssuer{Distinguished}Name requires a complete match of the SUBJECT, including CN, O, and OU</param>
		/// <returns>Matching certificates</returns>
		// Token: 0x06000171 RID: 369 RVA: 0x00012EBC File Offset: 0x000110BC
		private static X509Certificate2Collection FindCertsByIssuer(StoreName storeName, string sFullIssuerSubject)
		{
			X509Store certStore = new X509Store(storeName, StoreLocation.CurrentUser);
			X509Certificate2Collection result;
			try
			{
				certStore.Open(OpenFlags.OpenExistingOnly);
				X509Certificate2Collection certs = certStore.Certificates.Find(X509FindType.FindByIssuerDistinguishedName, sFullIssuerSubject, false);
				result = certs;
			}
			finally
			{
				certStore.Close();
			}
			return result;
		}

		/// <summary>
		/// Interface method: Clear the in-memory caches and Windows certificate stores
		/// </summary>
		/// <param name="bRemoveRoot">TRUE to clear the Root Certificate from the cache and Windows stores</param>
		/// <returns>TRUE if successful</returns>
		// Token: 0x06000172 RID: 370 RVA: 0x00012F04 File Offset: 0x00011104
		public bool ClearCertificateCache(bool bRemoveRoot)
		{
			bool bResult = true;
			try
			{
				this.GetWriterLock();
				this.certServerCache.Clear();
				this.certRoot = null;
				string sFullRootSubject = string.Format("CN={0}{1}", CONFIG.sMakeCertRootCN, CONFIG.sMakeCertSubjectO);
				X509Certificate2Collection oToRemove;
				if (bRemoveRoot)
				{
					oToRemove = DefaultCertificateProvider.FindCertsBySubject(StoreName.Root, StoreLocation.CurrentUser, sFullRootSubject);
					if (oToRemove.Count > 0)
					{
						X509Store certStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
						certStore.Open(OpenFlags.ReadWrite | OpenFlags.OpenExistingOnly);
						try
						{
							certStore.RemoveRange(oToRemove);
						}
						catch
						{
							bResult = false;
						}
						certStore.Close();
					}
				}
				oToRemove = DefaultCertificateProvider.FindCertsByIssuer(StoreName.My, sFullRootSubject);
				if (oToRemove.Count > 0)
				{
					if (!bRemoveRoot)
					{
						X509Certificate2 oRoot = this.GetRootCertificate();
						if (oRoot != null)
						{
							oToRemove.Remove(oRoot);
							if (oToRemove.Count < 1)
							{
								return true;
							}
						}
					}
					X509Store certStore2 = new X509Store(StoreName.My, StoreLocation.CurrentUser);
					certStore2.Open(OpenFlags.ReadWrite | OpenFlags.OpenExistingOnly);
					try
					{
						certStore2.RemoveRange(oToRemove);
					}
					catch
					{
						bResult = false;
					}
					certStore2.Close();
				}
			}
			finally
			{
				this.FreeWriterLock();
			}
			return bResult;
		}

		/// <summary>
		/// Interface method: Clear the in-memory caches and Windows certificate stores
		/// </summary>
		/// <returns></returns>
		// Token: 0x06000173 RID: 371 RVA: 0x0001300C File Offset: 0x0001120C
		public bool ClearCertificateCache()
		{
			return this.ClearCertificateCache(true);
		}

		// Token: 0x06000174 RID: 372 RVA: 0x00013015 File Offset: 0x00011215
		public bool rootCertIsTrusted(out bool bUserTrusted, out bool bMachineTrusted)
		{
			bUserTrusted = DefaultCertificateProvider.IsRootCertificateTrusted(StoreLocation.CurrentUser);
			bMachineTrusted = DefaultCertificateProvider.IsRootCertificateTrusted(StoreLocation.LocalMachine);
			return bUserTrusted | bMachineTrusted;
		}

		// Token: 0x06000175 RID: 373 RVA: 0x0001302C File Offset: 0x0001122C
		public bool TrustRootCertificate()
		{
			X509Certificate2 oRoot = this.GetRootCertificate();
			if (oRoot == null)
			{
				FiddlerApplication.Log.LogString("!Fiddler.CertMaker> The Root certificate could not be found.");
				return false;
			}
			bool result;
			try
			{
				X509Store certStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
				certStore.Open(OpenFlags.ReadWrite);
				try
				{
					certStore.Add(oRoot);
				}
				finally
				{
					certStore.Close();
				}
				result = true;
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("!Fiddler.CertMaker> Unable to auto-trust root: {0}", new object[] { eX });
				result = false;
			}
			return result;
		}

		// Token: 0x06000176 RID: 374 RVA: 0x000130B4 File Offset: 0x000112B4
		private static bool IsRootCertificateTrusted(StoreLocation storeLocation)
		{
			X509Certificate2 rootCertificate = CertMaker.GetRootCertificate();
			if (rootCertificate == null)
			{
				return false;
			}
			X509Store store = new X509Store(StoreName.Root, storeLocation);
			bool result;
			try
			{
				store.Open(OpenFlags.MaxAllowed);
				result = store.Certificates.Contains(rootCertificate);
			}
			finally
			{
				store.Close();
			}
			return result;
		}

		/// <summary>
		/// Use MakeCert to generate a unique self-signed certificate
		/// </summary>
		/// <returns>TRUE if the Root certificate was generated successfully</returns>
		// Token: 0x06000177 RID: 375 RVA: 0x00013104 File Offset: 0x00011304
		public bool CreateRootCertificate()
		{
			return this.CreateCert(CONFIG.sMakeCertRootCN, true) != null;
		}

		/// <summary>
		/// Get the root certificate from cache or storage, only IF IT ALREADY EXISTS.
		/// </summary>
		/// <returns></returns>
		// Token: 0x06000178 RID: 376 RVA: 0x00013118 File Offset: 0x00011318
		public X509Certificate2 GetRootCertificate()
		{
			if (this.certRoot != null)
			{
				return this.certRoot;
			}
			X509Certificate2 oRoot = DefaultCertificateProvider.LoadCertificateFromWindowsStore(CONFIG.sMakeCertRootCN);
			if (CONFIG.bDebugCertificateGeneration)
			{
				if (oRoot != null)
				{
					DefaultCertificateProvider._LogPrivateKeyContainer(oRoot);
				}
				else
				{
					FiddlerApplication.Log.LogString("DefaultCertMaker: GetRootCertificate() did not find the root in the Windows TrustStore.");
				}
			}
			this.certRoot = oRoot;
			return oRoot;
		}

		// Token: 0x06000179 RID: 377 RVA: 0x00013168 File Offset: 0x00011368
		private static void _LogPrivateKeyContainer(X509Certificate2 oRoot)
		{
			try
			{
				if (oRoot != null)
				{
					if (!oRoot.HasPrivateKey)
					{
						FiddlerApplication.Log.LogString("/Fiddler.CertMaker> Root Certificate located but HasPrivateKey==false!");
					}
					FiddlerApplication.Log.LogFormat("/Fiddler.CertMaker> Root Certificate located; private key in container '{0}'", new object[] { (oRoot.PrivateKey as RSACryptoServiceProvider).CspKeyContainerInfo.UniqueKeyContainerName });
				}
				else
				{
					FiddlerApplication.Log.LogString("/Fiddler.CertMaker> Unable to log Root Certificate private key storage as the certificate was unexpectedly null");
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("/Fiddler.CertMaker> Failed to identify private key location for Root Certificate. Exception: {0}", new object[] { Utilities.DescribeException(eX) });
			}
		}

		/// <summary>
		/// Returns an Interception certificate for the specified hostname
		/// </summary>
		/// <param name="sHostname">Hostname for the target certificate</param>
		/// <remarks>This method uses a Reader lock when checking the cache and a Writer lock when updating the cache.</remarks>
		/// <returns>An Interception Certificate, or NULL</returns>
		// Token: 0x0600017A RID: 378 RVA: 0x00013200 File Offset: 0x00011400
		public X509Certificate2 GetCertificateForHost(string sHostname)
		{
			if (this.UseWildcards && sHostname.OICEndsWithAny(this.arrWildcardTLDs) && Utilities.IndexOfNth(sHostname, 2, '.') > 0)
			{
				sHostname = "*." + Utilities.TrimBefore(sHostname, ".");
			}
			X509Certificate2 certResult;
			try
			{
				this.GetReaderLock();
				if (this.certServerCache.TryGetValue(sHostname, out certResult))
				{
					return certResult;
				}
			}
			finally
			{
				this.FreeReaderLock();
			}
			bool bCreated;
			certResult = this.LoadOrCreateCertificate(sHostname, out bCreated);
			if (certResult != null && !bCreated)
			{
				this.CacheCertificateForHost(sHostname, certResult);
			}
			return certResult;
		}

		/// <summary>
		/// Find a certificate from the certificate store, creating a new certificate if it was not found.
		/// </summary>
		/// <param name="sHostname">A SubjectCN hostname, of the form www.example.com</param>
		/// <param name="bAttemptedCreation">TRUE if the cert wasn't found in the Windows Certificate store and this function attempted to create it.</param>
		/// <remarks>No locks are acquired by this method itself.</remarks>
		/// <returns>A certificate or /null/</returns>
		// Token: 0x0600017B RID: 379 RVA: 0x00013298 File Offset: 0x00011498
		internal X509Certificate2 LoadOrCreateCertificate(string sHostname, out bool bAttemptedCreation)
		{
			bAttemptedCreation = false;
			X509Certificate2 oCert = DefaultCertificateProvider.LoadCertificateFromWindowsStore(sHostname);
			if (oCert != null)
			{
				return oCert;
			}
			bAttemptedCreation = true;
			oCert = this.CreateCert(sHostname, false);
			if (oCert == null)
			{
				FiddlerApplication.Log.LogFormat("!Fiddler.CertMaker> Tried to create cert for '{0}', but can't find it from thread {1}!", new object[]
				{
					sHostname,
					Thread.CurrentThread.ManagedThreadId
				});
			}
			return oCert;
		}

		/// <summary>
		/// Find (but do not create!) a certificate from the CurrentUser certificate store, if present.
		/// </summary>
		/// <remarks>No locks are acquired by this method itself.</remarks>
		/// <returns>A certificate or /null/</returns>
		// Token: 0x0600017C RID: 380 RVA: 0x000132F0 File Offset: 0x000114F0
		internal static X509Certificate2 LoadCertificateFromWindowsStore(string sHostname)
		{
			X509Store oStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
			try
			{
				oStore.Open(OpenFlags.ReadOnly);
				string sFullSubject = string.Format("CN={0}{1}", sHostname, CONFIG.sMakeCertSubjectO);
				foreach (X509Certificate2 certCandidate in oStore.Certificates)
				{
					if (sFullSubject.OICEquals(certCandidate.Subject))
					{
						return certCandidate;
					}
				}
			}
			finally
			{
				oStore.Close();
			}
			return null;
		}

		/// <summary>
		/// Updates the Server Certificate cache under the Writer lock
		/// </summary>
		/// <param name="sHost">The target hostname</param>
		/// <param name="oCert">The certificate to cache</param>
		/// <returns></returns>
		// Token: 0x0600017D RID: 381 RVA: 0x0001336C File Offset: 0x0001156C
		public bool CacheCertificateForHost(string sHost, X509Certificate2 oCert)
		{
			try
			{
				this.GetWriterLock();
				this.certServerCache[sHost] = oCert;
			}
			finally
			{
				this.FreeWriterLock();
			}
			return true;
		}

		/// <summary>
		/// Creates a certificate for ServerAuth. If isRoot is set, designates that this is a self-signed root.
		/// </summary>
		/// <remarks>Uses a reader lock when checking for the Root certificate. Uses a Writer lock when creating a certificate.</remarks>
		/// <param name="sHostname">A string of the form: "www.hostname.com"</param>
		/// <param name="isRoot">A boolean indicating if this is a request to create the root certificate</param>
		/// <returns>Newly-created certificate, or Null</returns>
		// Token: 0x0600017E RID: 382 RVA: 0x000133A8 File Offset: 0x000115A8
		private X509Certificate2 CreateCert(string sHostname, bool isRoot)
		{
			if (sHostname.IndexOfAny(new char[] { '"', '\r', '\n', '\0' }) != -1)
			{
				return null;
			}
			if (!isRoot && this.GetRootCertificate() == null)
			{
				try
				{
					this.GetWriterLock();
					if (this.GetRootCertificate() == null && !this.CreateRootCertificate())
					{
						string title = "Certificate Error";
						string message = "Creation of the root certificate was not successful.";
						FiddlerApplication.Log.LogFormat("{0}: {1}", new object[] { title, message });
						return null;
					}
				}
				finally
				{
					this.FreeWriterLock();
				}
			}
			X509Certificate2 oNewCert = null;
			try
			{
				this.GetWriterLock();
				X509Certificate2 oCheckAgain;
				if (!this.certServerCache.TryGetValue(sHostname, out oCheckAgain))
				{
					oCheckAgain = DefaultCertificateProvider.LoadCertificateFromWindowsStore(sHostname);
				}
				if (oCheckAgain != null)
				{
					if (CONFIG.bDebugCertificateGeneration)
					{
						FiddlerApplication.Log.LogFormat("/Fiddler.CertMaker>{1} A racing thread already successfully CreatedCert({0})", new object[]
						{
							sHostname,
							Thread.CurrentThread.ManagedThreadId
						});
					}
					return oCheckAgain;
				}
				oNewCert = this.CertCreator.CreateCert(sHostname, isRoot);
				if (oNewCert != null)
				{
					if (isRoot)
					{
						this.certRoot = oNewCert;
					}
					else
					{
						this.certServerCache[sHostname] = oNewCert;
					}
				}
			}
			finally
			{
				this.FreeWriterLock();
			}
			if (oNewCert == null && !isRoot)
			{
				FiddlerApplication.Log.LogFormat("!Fiddler.CertMaker> Failed to create certificate for '{0}'.", new object[] { sHostname });
				DefaultCertificateProvider._LogPrivateKeyContainer(this.GetRootCertificate());
			}
			return oNewCert;
		}

		// Token: 0x0600017F RID: 383 RVA: 0x00013504 File Offset: 0x00011704
		public string GetConfigurationString()
		{
			if (this.CertCreator == null)
			{
				return "No Engine Loaded.";
			}
			StringBuilder sbInfo = new StringBuilder();
			string sEngine = Utilities.TrimBefore(this.CertCreator.GetType().ToString(), "+");
			sbInfo.AppendFormat("Certificate Engine:\t{0}\n", sEngine);
			if (sEngine == "CertEnrollEngine")
			{
				sbInfo.AppendFormat("HashAlg-Root:\t{0}\n", FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.ce.Root.SigAlg", "SHA256"));
				sbInfo.AppendFormat("HashAlg-EE:\t{0}\n", FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.ce.EE.SigAlg", "SHA256"));
				sbInfo.AppendFormat("KeyLen-Root:\t{0}bits\n", FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.ce.Root.KeyLength", 2048));
				sbInfo.AppendFormat("KeyLen-EE:\t{0}bits\n", FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.ce.EE.KeyLength", 2048));
				sbInfo.AppendFormat("ValidFrom:\t{0} days ago\n", FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.GraceDays", 366));
				sbInfo.AppendFormat("ValidFor:\t\t{0} days\n", FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.ValidDays", 820));
			}
			else
			{
				sbInfo.AppendFormat("ValidFrom:\t{0} days ago\n", FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.GraceDays", 366));
				sbInfo.AppendFormat("HashAlg-Root:\t{0}\n", FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.Root.SigAlg", (Environment.OSVersion.Version.Major > 5) ? "SHA256" : "SHA1"));
				sbInfo.AppendFormat("HashAlg-EE:\t{0}\n", FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.EE.SigAlg", (Environment.OSVersion.Version.Major > 5) ? "SHA256" : "SHA1"));
				sbInfo.AppendFormat("ExtraParams-Root:\t{0}\n", FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.EE.extraparams", string.Empty));
				sbInfo.AppendFormat("ExtraParams-EE:\t{0}\n", FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.root.extraparams", string.Empty));
			}
			return sbInfo.ToString();
		}

		// Token: 0x040000B5 RID: 181
		private const int fiddlerCertmakerValidDays = 820;

		/// <summary>
		/// The underlying Certificate Generator (MakeCert or CertEnroll)
		/// </summary>
		// Token: 0x040000B6 RID: 182
		private DefaultCertificateProvider.ICertificateCreator CertCreator;

		/// <summary>
		/// Cache of previously-generated EE certificates. Thread safety managed by _oRWLock
		/// </summary>
		// Token: 0x040000B7 RID: 183
		private Dictionary<string, X509Certificate2> certServerCache = new Dictionary<string, X509Certificate2>();

		/// <summary>
		/// Cache of previously-generated Root certificate
		/// </summary>
		// Token: 0x040000B8 RID: 184
		private X509Certificate2 certRoot;

		/// <summary>
		/// Should Fiddler automatically generate wildcard certificates?
		/// </summary>
		// Token: 0x040000B9 RID: 185
		private bool UseWildcards;

		// Token: 0x040000BA RID: 186
		private readonly string[] arrWildcardTLDs = new string[] { ".com", ".org", ".edu", ".gov", ".net" };

		/// <summary>
		/// Reader/Writer lock gates access to the certificate cache and generation functions.
		/// </summary>
		/// <remarks>We must set the SupportsRecursion flag because there are cases where the thread holds the lock in Write mode and then enters Read mode in a nested call.</remarks>
		// Token: 0x040000BB RID: 187
		private ReaderWriterLockSlim _oRWLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

		// Token: 0x020000C3 RID: 195
		private interface ICertificateCreator
		{
			// Token: 0x060006FC RID: 1788
			X509Certificate2 CreateCert(string sSubject, bool isRoot);
		}

		/// <summary>
		/// CertEnroll is an ActiveX Control available on Windows Vista and later that allows programmatic generation of X509 certificates.
		/// We can use it as an alternative to MakeCert.exe; it offers better behavior (e.g. setting AKID) and doesn't require redistributing makecert.exe
		/// </summary>
		// Token: 0x020000C4 RID: 196
		private class CertEnrollEngine : DefaultCertificateProvider.ICertificateCreator
		{
			/// <summary>
			/// Factory method. Returns null if this engine cannot be created
			/// </summary>
			// Token: 0x060006FD RID: 1789 RVA: 0x00037B08 File Offset: 0x00035D08
			internal static DefaultCertificateProvider.ICertificateCreator GetEngine(ICertificateProvider3 ParentProvider)
			{
				try
				{
					return new DefaultCertificateProvider.CertEnrollEngine(ParentProvider);
				}
				catch (Exception eX)
				{
					FiddlerApplication.Log.LogFormat("Failed to initialize CertEnrollEngine: {0}", new object[] { Utilities.DescribeException(eX) });
				}
				return null;
			}

			// Token: 0x060006FE RID: 1790 RVA: 0x00037B54 File Offset: 0x00035D54
			private CertEnrollEngine(ICertificateProvider3 ParentProvider)
			{
				this._ParentProvider = ParentProvider;
				this.sProviderName = FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.KeyProviderName", this.sProviderName);
				this.typeX500DN = Type.GetTypeFromProgID("X509Enrollment.CX500DistinguishedName", true);
				this.typeX509PrivateKey = Type.GetTypeFromProgID("X509Enrollment.CX509PrivateKey", true);
				this.typeOID = Type.GetTypeFromProgID("X509Enrollment.CObjectId", true);
				this.typeOIDS = Type.GetTypeFromProgID("X509Enrollment.CObjectIds.1", true);
				this.typeEKUExt = Type.GetTypeFromProgID("X509Enrollment.CX509ExtensionEnhancedKeyUsage");
				this.typeKUExt = Type.GetTypeFromProgID("X509Enrollment.CX509ExtensionKeyUsage");
				this.typeRequestCert = Type.GetTypeFromProgID("X509Enrollment.CX509CertificateRequestCertificate");
				this.typeX509Extensions = Type.GetTypeFromProgID("X509Enrollment.CX509Extensions");
				this.typeBasicConstraints = Type.GetTypeFromProgID("X509Enrollment.CX509ExtensionBasicConstraints");
				this.typeSignerCertificate = Type.GetTypeFromProgID("X509Enrollment.CSignerCertificate");
				this.typeX509Enrollment = Type.GetTypeFromProgID("X509Enrollment.CX509Enrollment");
				this.typeAlternativeName = Type.GetTypeFromProgID("X509Enrollment.CAlternativeName");
				this.typeAlternativeNames = Type.GetTypeFromProgID("X509Enrollment.CAlternativeNames");
				this.typeAlternativeNamesExt = Type.GetTypeFromProgID("X509Enrollment.CX509ExtensionAlternativeNames");
			}

			// Token: 0x060006FF RID: 1791 RVA: 0x00037C78 File Offset: 0x00035E78
			public X509Certificate2 CreateCert(string sSubjectCN, bool isRoot)
			{
				return this.InternalCreateCert(sSubjectCN, isRoot, true);
			}

			/// <summary>
			/// Invoke CertEnroll
			/// </summary>
			/// <param name="sSubjectCN">Target CN</param>
			/// <param name="isRoot">TRUE if the certificate is a root cert</param>
			/// <param name="switchToMTAIfNeeded">TRUE if we should validate that we're running in a MTA thread and switch if not</param>
			/// <returns>A Cert</returns>
			// Token: 0x06000700 RID: 1792 RVA: 0x00037C84 File Offset: 0x00035E84
			private X509Certificate2 InternalCreateCert(string sSubjectCN, bool isRoot, bool switchToMTAIfNeeded)
			{
				if (switchToMTAIfNeeded && Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
				{
					if (CONFIG.bDebugCertificateGeneration)
					{
						FiddlerApplication.Log.LogFormat("/Fiddler.CertMaker> Caller was in ApartmentState: {0}; hopping to Threadpool", new object[] { Thread.CurrentThread.GetApartmentState().ToString() });
					}
					X509Certificate2 newCert = null;
					ManualResetEvent oMRE = new ManualResetEvent(false);
					ThreadPool.QueueUserWorkItem(delegate(object o)
					{
						newCert = this.InternalCreateCert(sSubjectCN, isRoot, false);
						oMRE.Set();
					});
					oMRE.WaitOne();
					oMRE.Close();
					return newCert;
				}
				string sFullSubject = string.Format("CN={0}{1}", sSubjectCN, CONFIG.sMakeCertSubjectO);
				if (CONFIG.bDebugCertificateGeneration)
				{
					FiddlerApplication.Log.LogFormat("/Fiddler.CertMaker> Invoking CertEnroll for Subject: {0}; Thread's ApartmentState: {1}", new object[]
					{
						sFullSubject,
						Thread.CurrentThread.GetApartmentState().ToString()
					});
				}
				string sHash = FiddlerApplication.Prefs.GetStringPref(isRoot ? "fiddler.certmaker.ce.Root.SigAlg" : "fiddler.certmaker.ce.EE.SigAlg", "SHA256");
				int iGraceDays = 0 - FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.GraceDays", 366);
				int iValidDays = FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.ValidDays", 820);
				try
				{
					X509Certificate2 oNewCert;
					if (isRoot)
					{
						int iPrivateKeyLen = FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.ce.Root.KeyLength", 2048);
						oNewCert = this.GenerateCertificate(true, sSubjectCN, sFullSubject, iPrivateKeyLen, sHash, DateTime.Now.AddDays((double)iGraceDays), DateTime.Now.AddDays((double)iValidDays), null);
					}
					else
					{
						int iPrivateKeyLen2 = FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.ce.EE.KeyLength", 2048);
						oNewCert = this.GenerateCertificate(false, sSubjectCN, sFullSubject, iPrivateKeyLen2, sHash, DateTime.Now.AddDays((double)iGraceDays), DateTime.Now.AddDays((double)iValidDays), this._ParentProvider.GetRootCertificate());
					}
					if (CONFIG.bDebugCertificateGeneration)
					{
						FiddlerApplication.Log.LogFormat("/Fiddler.CertMaker> Finished CertEnroll for '{0}'. Returning {1}", new object[]
						{
							sFullSubject,
							(oNewCert != null) ? "cert" : "null"
						});
					}
					return oNewCert;
				}
				catch (Exception eX)
				{
					FiddlerApplication.Log.LogFormat("!ERROR: Failed to generate Certificate using CertEnroll. {0}", new object[] { Utilities.DescribeException(eX) });
				}
				return null;
			}

			// Token: 0x06000701 RID: 1793 RVA: 0x00037EFC File Offset: 0x000360FC
			private X509Certificate2 GenerateCertificate(bool bIsRoot, string sSubjectCN, string sFullSubject, int iPrivateKeyLength, string sHashAlg, DateTime dtValidFrom, DateTime dtValidTo, X509Certificate2 oSigningCertificate)
			{
				if (bIsRoot != (oSigningCertificate == null))
				{
					throw new ArgumentException("You must specify a Signing Certificate if and only if you are not creating a root.", "oSigningCertificate");
				}
				object oSubjectDN = Activator.CreateInstance(this.typeX500DN);
				object[] arrArgs = new object[] { sFullSubject, 0 };
				this.typeX500DN.InvokeMember("Encode", BindingFlags.InvokeMethod, null, oSubjectDN, arrArgs);
				object oIssuerDN = Activator.CreateInstance(this.typeX500DN);
				if (!bIsRoot)
				{
					arrArgs[0] = oSigningCertificate.Subject;
				}
				this.typeX500DN.InvokeMember("Encode", BindingFlags.InvokeMethod, null, oIssuerDN, arrArgs);
				object oPrivateKey = null;
				if (!bIsRoot)
				{
					oPrivateKey = this._oSharedPrivateKey;
				}
				if (oPrivateKey == null)
				{
					oPrivateKey = Activator.CreateInstance(this.typeX509PrivateKey);
					arrArgs = new object[] { this.sProviderName };
					this.typeX509PrivateKey.InvokeMember("ProviderName", BindingFlags.PutDispProperty, null, oPrivateKey, arrArgs);
					arrArgs[0] = 2;
					this.typeX509PrivateKey.InvokeMember("ExportPolicy", BindingFlags.PutDispProperty, null, oPrivateKey, arrArgs);
					arrArgs = new object[] { bIsRoot ? 2 : 1 };
					this.typeX509PrivateKey.InvokeMember("KeySpec", BindingFlags.PutDispProperty, null, oPrivateKey, arrArgs);
					if (!bIsRoot)
					{
						arrArgs = new object[] { 176 };
						this.typeX509PrivateKey.InvokeMember("KeyUsage", BindingFlags.PutDispProperty, null, oPrivateKey, arrArgs);
					}
					arrArgs[0] = iPrivateKeyLength;
					this.typeX509PrivateKey.InvokeMember("Length", BindingFlags.PutDispProperty, null, oPrivateKey, arrArgs);
					this.typeX509PrivateKey.InvokeMember("Create", BindingFlags.InvokeMethod, null, oPrivateKey, null);
					if (!bIsRoot)
					{
						this._oSharedPrivateKey = oPrivateKey;
					}
				}
				else if (CONFIG.bDebugCertificateGeneration)
				{
					FiddlerApplication.Log.LogFormat("/Fiddler.CertMaker> Reusing PrivateKey for '{0}'", new object[] { sSubjectCN });
				}
				arrArgs = new object[1];
				object oServerAuthOID = Activator.CreateInstance(this.typeOID);
				arrArgs[0] = "1.3.6.1.5.5.7.3.1";
				this.typeOID.InvokeMember("InitializeFromValue", BindingFlags.InvokeMethod, null, oServerAuthOID, arrArgs);
				object oOIDS = Activator.CreateInstance(this.typeOIDS);
				arrArgs[0] = oServerAuthOID;
				this.typeOIDS.InvokeMember("Add", BindingFlags.InvokeMethod, null, oOIDS, arrArgs);
				object oEKUExt = Activator.CreateInstance(this.typeEKUExt);
				arrArgs[0] = oOIDS;
				this.typeEKUExt.InvokeMember("InitializeEncode", BindingFlags.InvokeMethod, null, oEKUExt, arrArgs);
				object oRequest = Activator.CreateInstance(this.typeRequestCert);
				arrArgs = new object[]
				{
					1,
					oPrivateKey,
					string.Empty
				};
				this.typeRequestCert.InvokeMember("InitializeFromPrivateKey", BindingFlags.InvokeMethod, null, oRequest, arrArgs);
				arrArgs = new object[] { oSubjectDN };
				this.typeRequestCert.InvokeMember("Subject", BindingFlags.PutDispProperty, null, oRequest, arrArgs);
				arrArgs[0] = oIssuerDN;
				this.typeRequestCert.InvokeMember("Issuer", BindingFlags.PutDispProperty, null, oRequest, arrArgs);
				arrArgs[0] = dtValidFrom;
				this.typeRequestCert.InvokeMember("NotBefore", BindingFlags.PutDispProperty, null, oRequest, arrArgs);
				arrArgs[0] = dtValidTo;
				this.typeRequestCert.InvokeMember("NotAfter", BindingFlags.PutDispProperty, null, oRequest, arrArgs);
				object oKUExt = Activator.CreateInstance(this.typeKUExt);
				arrArgs[0] = 176;
				this.typeKUExt.InvokeMember("InitializeEncode", BindingFlags.InvokeMethod, null, oKUExt, arrArgs);
				object oExtensions = this.typeRequestCert.InvokeMember("X509Extensions", BindingFlags.GetProperty, null, oRequest, null);
				arrArgs = new object[1];
				if (!bIsRoot)
				{
					arrArgs[0] = oKUExt;
					this.typeX509Extensions.InvokeMember("Add", BindingFlags.InvokeMethod, null, oExtensions, arrArgs);
				}
				arrArgs[0] = oEKUExt;
				this.typeX509Extensions.InvokeMember("Add", BindingFlags.InvokeMethod, null, oExtensions, arrArgs);
				if (!bIsRoot && FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.AddSubjectAltName", true))
				{
					object oSubjectAltName = Activator.CreateInstance(this.typeAlternativeName);
					IPAddress ipDest = Utilities.IPFromString(sSubjectCN);
					if (ipDest == null)
					{
						arrArgs = new object[] { 3, sSubjectCN };
						this.typeAlternativeName.InvokeMember("InitializeFromString", BindingFlags.InvokeMethod, null, oSubjectAltName, arrArgs);
					}
					else
					{
						arrArgs = new object[]
						{
							8,
							1,
							Convert.ToBase64String(ipDest.GetAddressBytes())
						};
						this.typeAlternativeName.InvokeMember("InitializeFromRawData", BindingFlags.InvokeMethod, null, oSubjectAltName, arrArgs);
					}
					object objAlternativeNames = Activator.CreateInstance(this.typeAlternativeNames);
					arrArgs = new object[] { oSubjectAltName };
					this.typeAlternativeNames.InvokeMember("Add", BindingFlags.InvokeMethod, null, objAlternativeNames, arrArgs);
					Marshal.ReleaseComObject(oSubjectAltName);
					if (ipDest != null && AddressFamily.InterNetworkV6 == ipDest.AddressFamily)
					{
						oSubjectAltName = Activator.CreateInstance(this.typeAlternativeName);
						arrArgs = new object[]
						{
							3,
							"[" + sSubjectCN + "]"
						};
						this.typeAlternativeName.InvokeMember("InitializeFromString", BindingFlags.InvokeMethod, null, oSubjectAltName, arrArgs);
						arrArgs = new object[] { oSubjectAltName };
						this.typeAlternativeNames.InvokeMember("Add", BindingFlags.InvokeMethod, null, objAlternativeNames, arrArgs);
						Marshal.ReleaseComObject(oSubjectAltName);
					}
					object oExtAlternativeNames = Activator.CreateInstance(this.typeAlternativeNamesExt);
					arrArgs = new object[] { objAlternativeNames };
					this.typeAlternativeNamesExt.InvokeMember("InitializeEncode", BindingFlags.InvokeMethod, null, oExtAlternativeNames, arrArgs);
					arrArgs = new object[] { oExtAlternativeNames };
					this.typeX509Extensions.InvokeMember("Add", BindingFlags.InvokeMethod, null, oExtensions, arrArgs);
				}
				if (bIsRoot)
				{
					object oBasicConstraints = Activator.CreateInstance(this.typeBasicConstraints);
					arrArgs = new object[] { "true", "0" };
					this.typeBasicConstraints.InvokeMember("InitializeEncode", BindingFlags.InvokeMethod, null, oBasicConstraints, arrArgs);
					arrArgs = new object[] { oBasicConstraints };
					this.typeX509Extensions.InvokeMember("Add", BindingFlags.InvokeMethod, null, oExtensions, arrArgs);
				}
				else
				{
					object oCA = Activator.CreateInstance(this.typeSignerCertificate);
					arrArgs = new object[] { 0, 0, 12, oSigningCertificate.Thumbprint };
					this.typeSignerCertificate.InvokeMember("Initialize", BindingFlags.InvokeMethod, null, oCA, arrArgs);
					arrArgs = new object[] { oCA };
					this.typeRequestCert.InvokeMember("SignerCertificate", BindingFlags.PutDispProperty, null, oRequest, arrArgs);
				}
				object oHash = Activator.CreateInstance(this.typeOID);
				arrArgs = new object[] { 1, 0, 0, sHashAlg };
				this.typeOID.InvokeMember("InitializeFromAlgorithmName", BindingFlags.InvokeMethod, null, oHash, arrArgs);
				arrArgs = new object[] { oHash };
				this.typeRequestCert.InvokeMember("HashAlgorithm", BindingFlags.PutDispProperty, null, oRequest, arrArgs);
				this.typeRequestCert.InvokeMember("Encode", BindingFlags.InvokeMethod, null, oRequest, null);
				object oEnrollment = Activator.CreateInstance(this.typeX509Enrollment);
				arrArgs[0] = oRequest;
				this.typeX509Enrollment.InvokeMember("InitializeFromRequest", BindingFlags.InvokeMethod, null, oEnrollment, arrArgs);
				if (bIsRoot)
				{
					arrArgs[0] = "DO_NOT_TRUST_FiddlerRoot-CE";
					this.typeX509Enrollment.InvokeMember("CertificateFriendlyName", BindingFlags.PutDispProperty, null, oEnrollment, arrArgs);
				}
				arrArgs[0] = 0;
				object oCert = this.typeX509Enrollment.InvokeMember("CreateRequest", BindingFlags.InvokeMethod, null, oEnrollment, arrArgs);
				arrArgs = new object[]
				{
					2,
					oCert,
					0,
					string.Empty
				};
				this.typeX509Enrollment.InvokeMember("InstallResponse", BindingFlags.InvokeMethod, null, oEnrollment, arrArgs);
				arrArgs = new object[] { null, 0, 1 };
				string oCertAsString = string.Empty;
				try
				{
					oCertAsString = (string)this.typeX509Enrollment.InvokeMember("CreatePFX", BindingFlags.InvokeMethod, null, oEnrollment, arrArgs);
				}
				catch (Exception eX)
				{
					FiddlerApplication.Log.LogFormat("!Failed to CreatePFX: {0}", new object[] { Utilities.DescribeException(eX) });
					return null;
				}
				return new X509Certificate2(Convert.FromBase64String(oCertAsString), string.Empty, X509KeyStorageFlags.Exportable);
			}

			// Token: 0x0400033C RID: 828
			private ICertificateProvider3 _ParentProvider;

			// Token: 0x0400033D RID: 829
			private Type typeX500DN;

			// Token: 0x0400033E RID: 830
			private Type typeX509PrivateKey;

			// Token: 0x0400033F RID: 831
			private Type typeOID;

			// Token: 0x04000340 RID: 832
			private Type typeOIDS;

			// Token: 0x04000341 RID: 833
			private Type typeKUExt;

			// Token: 0x04000342 RID: 834
			private Type typeEKUExt;

			// Token: 0x04000343 RID: 835
			private Type typeRequestCert;

			// Token: 0x04000344 RID: 836
			private Type typeX509Extensions;

			// Token: 0x04000345 RID: 837
			private Type typeBasicConstraints;

			// Token: 0x04000346 RID: 838
			private Type typeSignerCertificate;

			// Token: 0x04000347 RID: 839
			private Type typeX509Enrollment;

			// Token: 0x04000348 RID: 840
			private Type typeAlternativeName;

			// Token: 0x04000349 RID: 841
			private Type typeAlternativeNames;

			// Token: 0x0400034A RID: 842
			private Type typeAlternativeNamesExt;

			// Token: 0x0400034B RID: 843
			private string sProviderName = "Microsoft Enhanced Cryptographic Provider v1.0";

			// Token: 0x0400034C RID: 844
			private object _oSharedPrivateKey;
		}

		// Token: 0x020000C5 RID: 197
		private class MakeCertEngine : DefaultCertificateProvider.ICertificateCreator
		{
			/// <summary>
			/// Factory method. Returns null if this engine cannot be created
			/// </summary>
			// Token: 0x06000702 RID: 1794 RVA: 0x00038730 File Offset: 0x00036930
			internal static DefaultCertificateProvider.ICertificateCreator GetEngine()
			{
				try
				{
					return new DefaultCertificateProvider.MakeCertEngine();
				}
				catch (Exception eX)
				{
					FiddlerApplication.Log.LogFormat("!Failed to initialize MakeCertEngine: {0}", new object[] { Utilities.DescribeException(eX) });
				}
				return null;
			}

			/// <summary>
			/// Constructor: Simply cache the path to MakeCert
			/// </summary>
			// Token: 0x06000703 RID: 1795 RVA: 0x0003877C File Offset: 0x0003697C
			private MakeCertEngine()
			{
				if (Environment.OSVersion.Version.Major > 5)
				{
					this._sDefaultHash = "sha256";
				}
				this._sMakeCertLocation = CONFIG.GetPath("MakeCert");
				if (!File.Exists(this._sMakeCertLocation))
				{
					FiddlerApplication.Log.LogFormat("Cannot locate:\n\t\"{0}\"\n\nPlease move makecert.exe to the Fiddler installation directory.", new object[] { this._sMakeCertLocation });
					throw new FileNotFoundException("Cannot locate: \"" + this._sMakeCertLocation + "\". Please move makecert.exe to the Fiddler installation directory.");
				}
			}

			// Token: 0x06000704 RID: 1796 RVA: 0x00038810 File Offset: 0x00036A10
			public X509Certificate2 CreateCert(string sHostname, bool isRoot)
			{
				X509Certificate2 oNewCert = null;
				string sDateFormatString = FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.DateFormatString", "MM/dd/yyyy");
				int iGraceDays = 0 - FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.GraceDays", 366);
				DateTime dtValidityStarts = DateTime.Now.AddDays((double)iGraceDays);
				string sCmdLine;
				if (isRoot)
				{
					sCmdLine = string.Format(CONFIG.sMakeCertParamsRoot, new object[]
					{
						sHostname,
						CONFIG.sMakeCertSubjectO,
						CONFIG.sMakeCertRootCN,
						FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.Root.SigAlg", this._sDefaultHash),
						dtValidityStarts.ToString(sDateFormatString, CultureInfo.InvariantCulture),
						FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.Root.extraparams", string.Empty)
					});
				}
				else
				{
					sCmdLine = string.Format(CONFIG.sMakeCertParamsEE, new object[]
					{
						sHostname,
						CONFIG.sMakeCertSubjectO,
						CONFIG.sMakeCertRootCN,
						FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.EE.SigAlg", this._sDefaultHash),
						dtValidityStarts.ToString(sDateFormatString, CultureInfo.InvariantCulture),
						FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.EE.extraparams", string.Empty)
					});
				}
				if (CONFIG.bDebugCertificateGeneration)
				{
					FiddlerApplication.Log.LogFormat("/Fiddler.CertMaker> Invoking makecert.exe with arguments: {0}", new object[] { sCmdLine });
				}
				int iExitCode;
				string sErrorText = Utilities.GetExecutableOutput(this._sMakeCertLocation, sCmdLine, out iExitCode);
				if (CONFIG.bDebugCertificateGeneration)
				{
					FiddlerApplication.Log.LogFormat("/Fiddler.CertMaker>{3}-CreateCert({0}) => ({1}){2}", new object[]
					{
						sHostname,
						iExitCode,
						(iExitCode == 0) ? "." : ("\r\n" + sErrorText),
						Thread.CurrentThread.ManagedThreadId
					});
				}
				if (iExitCode == 0)
				{
					int iRetryCount = 6;
					do
					{
						oNewCert = DefaultCertificateProvider.LoadCertificateFromWindowsStore(sHostname);
						Thread.Sleep(50 * (6 - iRetryCount));
						if (CONFIG.bDebugCertificateGeneration && oNewCert == null)
						{
							FiddlerApplication.Log.LogFormat("!WARNING: Couldn't find certificate for {0} on try #{1}", new object[]
							{
								sHostname,
								6 - iRetryCount
							});
						}
						iRetryCount--;
					}
					while (oNewCert == null && iRetryCount >= 0);
				}
				if (oNewCert == null)
				{
					string sError = string.Format("Creation of the interception certificate failed.\n\nmakecert.exe returned {0}.\n\n{1}", iExitCode, sErrorText);
					FiddlerApplication.Log.LogFormat("Fiddler.CertMaker> [{0} {1}] Returned Error: {2} ", new object[] { this._sMakeCertLocation, sCmdLine, sError });
				}
				return oNewCert;
			}

			/// <summary>
			/// File path pointing to the location of MakeCert.exe
			/// </summary>
			// Token: 0x0400034D RID: 845
			private string _sMakeCertLocation;

			/// <summary>
			/// Hash to use when signing certificates.
			/// Note: sha1 is required on XP (even w/SP3, using sha256 throws 0x80090008).
			/// </summary>
			// Token: 0x0400034E RID: 846
			private string _sDefaultHash = "sha1";
		}
	}
}
