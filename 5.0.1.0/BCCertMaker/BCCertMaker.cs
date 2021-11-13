using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fiddler;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

namespace BCCertMaker
{
	// Token: 0x02000002 RID: 2
	public class BCCertMaker : ICertificateProvider5, ICertificateProvider4, ICertificateProvider3, ICertificateProvider2, ICertificateProvider, ICertificateProviderInfo, IDisposable
	{
		/// <summary>
		/// Length for the Public/Private Key used in the EE certificate
		/// </summary>
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		private static int iCertBitness
		{
			get
			{
				return FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.bc.KeyLength", 2048);
			}
		}

		/// <summary>
		/// Length for the Public/Private Key used in the Root certificate
		/// </summary>
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000002 RID: 2 RVA: 0x00002066 File Offset: 0x00000266
		private static int iRootCertBitness
		{
			get
			{
				return FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.bc.RootKeyLength", 2048);
			}
		}

		/// <summary>
		/// Should verbose logging information be emitted?
		/// </summary>
		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000003 RID: 3 RVA: 0x0000207C File Offset: 0x0000027C
		private static bool bDebugSpew
		{
			get
			{
				return FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.Debug", false);
			}
		}

		/// <summary>
		/// Controls whether we use the same Public/Private keypair for all Server Certificates  (improves perf)
		/// </summary>
		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000004 RID: 4 RVA: 0x0000208E File Offset: 0x0000028E
		private static bool bReuseServerKey
		{
			get
			{
				return FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.ReusePrivateKeys", true);
			}
		}

		/// <summary>
		/// Controls whether we use the same Public/Private keypair for the root AND all Server Certificates (improves perf)
		/// </summary>
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000005 RID: 5 RVA: 0x000020A0 File Offset: 0x000002A0
		private static bool bReuseRootKeyAsServerKey
		{
			get
			{
				return FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.ReuseRootKeysForEE", true);
			}
		}

		// Token: 0x06000006 RID: 6 RVA: 0x000020B2 File Offset: 0x000002B2
		private static string GetRootFriendly()
		{
			return FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.RootFriendly", "DO_NOT_TRUST_FiddlerRoot-BC");
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000020C8 File Offset: 0x000002C8
		private static string GetRootCN()
		{
			return FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.RootCN", "DO_NOT_TRUST_FiddlerRoot");
		}

		// Token: 0x06000008 RID: 8
		[DllImport("User32.dll")]
		private static extern int SetForegroundWindow(int hWnd);

		/// <summary>
		/// Get the base name for the KeyContainer into which the private key goes. If EE Keys are being reused, then we use only
		/// this ID.
		/// </summary>
		/// <returns></returns>
		// Token: 0x06000009 RID: 9 RVA: 0x000020DE File Offset: 0x000002DE
		private string GetKeyContainerNameBase()
		{
			return FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.KeyContainerName", "FiddlerBCKey");
		}

		/// <summary>
		/// Returns the Subject O field. Note that Fiddler's normal root uses "DO_NOT_TRUST" rather than "DO_NOT_TRUST_BC".
		/// </summary>
		/// <returns></returns>
		// Token: 0x0600000A RID: 10 RVA: 0x000020F4 File Offset: 0x000002F4
		private string GetCertO()
		{
			return "DO_NOT_TRUST_BC";
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000020FB File Offset: 0x000002FB
		private string GetCertOU()
		{
			return "Created by http://www.fiddler2.com";
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002104 File Offset: 0x00000304
		public BCCertMaker()
		{
			FiddlerApplication.Log.LogFormat("Fiddler ICertificateProvider v{0} loaded.\n\tfiddler.certmaker.bc.Debug:\t{1}\n\tObjectID:\t\t\t0x{2:x}", new object[]
			{
				Assembly.GetExecutingAssembly().GetName().Version.ToString(),
				BCCertMaker.bDebugSpew,
				this.GetHashCode()
			});
			this.iParallelTimeout = FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.bc.ParallelTimeout", this.iParallelTimeout);
			this.UseWildcards = FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.UseWildcards", true);
			if (BCCertMaker.bDebugSpew)
			{
				FiddlerApplication.Log.LogFormat("\tUsing BCMakeCert.dll v{0}", new object[] { typeof(AttributeX509).Assembly.GetName().Version.ToString() });
			}
			if (Environment.OSVersion.Version.Major >= 6)
			{
				this._sDefaultHash = "SHA256WITHRSA";
				return;
			}
			int iProviderType = FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.bc.KeyProviderType", -1);
			if (iProviderType < 0)
			{
				FiddlerApplication.Prefs.SetInt32Pref("fiddler.certmaker.bc.KeyProviderType", 1);
				return;
			}
			FiddlerApplication.Log.LogFormat("!CertMaker was reconfigured to use KeyProviderType={0}. Values != 1 are expected to fail on Windows XP.", new object[] { iProviderType });
		}

		/// <summary>
		/// Flush EE certificates to force regeneration
		/// </summary>
		// Token: 0x0600000D RID: 13 RVA: 0x000022BC File Offset: 0x000004BC
		private void _InternalFlushEECertCache()
		{
			try
			{
				this._RWLockForCache.AcquireWriterLock(-1);
				this.oEEKeyPair = null;
				if (this.certCache.Count >= 1)
				{
					this.certCache.Clear();
				}
			}
			finally
			{
				this._RWLockForCache.ReleaseWriterLock();
			}
		}

		// Token: 0x0600000E RID: 14 RVA: 0x00002318 File Offset: 0x00000518
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
		///
		/// </summary>
		/// <param name="storeName"></param>
		/// <param name="sFullIssuerSubject">FindByIssuer{Distinguished}Name requires a complete match of the SUBJECT, including CN, O, and OU</param>
		/// <returns></returns>
		// Token: 0x0600000F RID: 15 RVA: 0x00002368 File Offset: 0x00000568
		private static X509Certificate2Collection FindCertsByIssuer(StoreName storeName, StoreLocation storeLocation, string sFullIssuerSubject)
		{
			X509Certificate2Collection result;
			try
			{
				X509Store certStore = new X509Store(storeName, storeLocation);
				certStore.Open(OpenFlags.OpenExistingOnly);
				X509Certificate2Collection certs = certStore.Certificates.Find(X509FindType.FindByIssuerDistinguishedName, sFullIssuerSubject, false);
				certStore.Close();
				if (BCCertMaker.bDebugSpew)
				{
					FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> FindCertsByIssuer found {0} certificates in {1}.{2} matching '{3}'.", new object[] { certs.Count, storeLocation, storeName, sFullIssuerSubject });
				}
				result = certs;
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("FindCertsByIssuer failed: {0}", new object[] { eX.Message });
				result = new X509Certificate2Collection();
			}
			return result;
		}

		/// <summary>
		/// Converts from a BouncyCastle Certificate object into a .NET X509Certificate2 object
		/// </summary>
		/// <param name="certBC">A BouncyCastle X509Certificate</param>
		/// <returns>The .NET X509Certificate2</returns>
		// Token: 0x06000010 RID: 16 RVA: 0x00002418 File Offset: 0x00000618
		private static X509Certificate2 ConvertBCCertToDotNetCert(X509Certificate certBC)
		{
			return new X509Certificate2(DotNetUtilities.ToX509Certificate(certBC));
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002434 File Offset: 0x00000634
		private static X509Certificate2 ConvertBCCertToDotNetCert(X509Certificate certBC, AsymmetricKeyParameter privateKey)
		{
			Pkcs12StoreBuilder pkcs12StoreBuilder = new Pkcs12StoreBuilder();
			pkcs12StoreBuilder.SetUseDerEncoding(true);
			Pkcs12Store pkcs12Store = pkcs12StoreBuilder.Build();
			pkcs12Store.SetKeyEntry(string.Empty, new AsymmetricKeyEntry(privateKey), new X509CertificateEntry[]
			{
				new X509CertificateEntry(certBC)
			});
			X509Certificate2 dotNetCert;
			using (MemoryStream pfxStream = new MemoryStream())
			{
				pkcs12Store.Save(pfxStream, new char[0], new SecureRandom());
				pfxStream.Seek(0L, SeekOrigin.Begin);
				dotNetCert = new X509Certificate2(pfxStream.ToArray());
			}
			return dotNetCert;
		}

		/// <summary>
		/// Copy BC cert to Windows Certificate Storage, without key. THROWS on Errors
		/// </summary>
		/// <param name="sFriendlyName"></param>
		/// <param name="newCert"></param>
		/// <param name="oSL"></param>
		/// <param name="oSN"></param>
		/// <returns>True if operation was successfull, False if operation was cancelled.</returns>
		// Token: 0x06000012 RID: 18 RVA: 0x000024C4 File Offset: 0x000006C4
		private static bool AddBCCertToStore(string sFriendlyName, X509Certificate newCert, StoreLocation oSL, StoreName oSN)
		{
			X509Certificate2 certDotNet = BCCertMaker.ConvertBCCertToDotNetCert(newCert);
			certDotNet.FriendlyName = sFriendlyName;
			X509Store store = new X509Store(oSN, oSL);
			store.Open(OpenFlags.ReadWrite);
			bool result = true;
			try
			{
				store.Add(certDotNet);
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("Failed to add certificate to store: {0}", new object[] { eX.Message });
				result = false;
			}
			finally
			{
				store.Close();
			}
			return result;
		}

		/// <summary>
		/// Generates a new EE Certificate using the given CA Certificate to sign it. Throws on Crypto Exceptions.
		/// </summary>
		/// <param name="sCN"></param>
		/// <param name="caCert"></param>
		/// <param name="caKey"></param>
		/// <returns></returns>
		// Token: 0x06000013 RID: 19 RVA: 0x00002540 File Offset: 0x00000740
		private X509Certificate2 CreateCertificateFromCA(string sCN, X509Certificate caCert, AsymmetricKeyParameter caKey)
		{
			Stopwatch oSW = Stopwatch.StartNew();
			if (BCCertMaker.bDebugSpew)
			{
				FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> CreatingCert for: {0}", new object[] { sCN });
			}
			AsymmetricCipherKeyPair keyPair = this._GetPublicPrivateKeyPair(sCN);
			if (BCCertMaker.bDebugSpew)
			{
				FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> PrivateKey Generation took: {0}ms; {1}-bit key.", new object[]
				{
					oSW.ElapsedMilliseconds,
					BCCertMaker.iCertBitness
				});
			}
			X509V3CertificateGenerator certGen = new X509V3CertificateGenerator();
			BigInteger serialNumber = new BigInteger(1, Guid.NewGuid().ToByteArray());
			certGen.SetSerialNumber(serialNumber);
			certGen.SetIssuerDN(caCert.IssuerDN);
			certGen.SetNotBefore(DateTime.Today.AddDays((double)FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.bc.EE.CreatedDaysAgo", -7)));
			certGen.SetNotAfter(DateTime.Today.AddYears(FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.bc.EE.YearsValid", 1)).AddMonths(-1));
			X509Name dnName = new X509Name(string.Format("OU={0}, O={1}, CN={2}", this.GetCertOU(), this.GetCertO(), sCN));
			certGen.SetSubjectDN(dnName);
			certGen.SetPublicKey(keyPair.Public);
			certGen.SetSignatureAlgorithm(FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.EE.SigAlg", this._sDefaultHash));
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.AddSubjectAltName", true))
			{
				IPAddress ipTarget = Utilities.IPFromString(sCN);
				GeneralName name = new GeneralName((ipTarget == null) ? 2 : 7, sCN);
				Asn1Encodable[] SAN;
				if (ipTarget != null && ipTarget.AddressFamily == AddressFamily.InterNetworkV6)
				{
					SAN = new Asn1Encodable[]
					{
						name,
						new GeneralName(2, "[" + sCN + "]")
					};
				}
				else
				{
					SAN = new Asn1Encodable[] { name };
				}
				certGen.AddExtension(X509Extensions.SubjectAlternativeName, false, new DerSequence(SAN));
			}
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.EE.SetAKID", true))
			{
				certGen.AddExtension(X509Extensions.AuthorityKeyIdentifier, FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.EE.CriticalAKID", false), new AuthorityKeyIdentifierStructure(caCert));
			}
			certGen.AddExtension(X509Extensions.BasicConstraints, FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.EE.CriticalBasicConstraints", false), new BasicConstraints(false));
			ExtendedKeyUsage oEKU;
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.AddClientAuthEKU", false))
			{
				oEKU = new ExtendedKeyUsage(new KeyPurposeID[]
				{
					KeyPurposeID.IdKPServerAuth,
					KeyPurposeID.IdKPClientAuth
				});
			}
			else
			{
				oEKU = new ExtendedKeyUsage(new KeyPurposeID[] { KeyPurposeID.IdKPServerAuth });
			}
			certGen.AddExtension(X509Extensions.ExtendedKeyUsage, FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.EE.CriticalEKU", false), oEKU);
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.AddEVPolicyOID", false))
			{
				PolicyInformation pi = new PolicyInformation(new DerObjectIdentifier("2.16.840.1.113733.1.7.23.6"));
				DerSequence sqPolicy = new DerSequence(pi);
				certGen.AddExtension(X509Extensions.CertificatePolicies, false, sqPolicy);
			}
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.AddCRL", false))
			{
				GeneralName gn = new GeneralName(new DerIA5String(FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.CRLURL", "http://www.fiddler2.com/revocationlist.crl")), 6);
				GeneralNames gns = new GeneralNames(gn);
				DistributionPointName dpn = new DistributionPointName(gns);
				DistributionPoint distp = new DistributionPoint(dpn, null, null);
				DerSequence seq = new DerSequence(distp);
				certGen.AddExtension(X509Extensions.CrlDistributionPoints, false, seq);
			}
			X509Certificate newCert = certGen.Generate(caKey);
			if (BCCertMaker.bDebugSpew)
			{
				FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> EECert Generation took: {0}ms in total.", new object[] { oSW.ElapsedMilliseconds });
			}
			oSW.Reset();
			oSW.Start();
			X509Certificate2 certDotNet = BCCertMaker.ConvertBCCertToDotNetCert(newCert, keyPair.Private);
			if (!certDotNet.HasPrivateKey)
			{
				FiddlerApplication.Log.LogString("Fiddler.BCCertMaker> FAIL: No Private Key");
			}
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.EmitEECertFile", false))
			{
				try
				{
					string sFilename = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\" + sCN + ".cer";
					File.WriteAllBytes(sFilename, certDotNet.Export(X509ContentType.Cert));
					FiddlerApplication.Log.LogFormat("Wrote file {0}", new object[] { sFilename });
				}
				catch (Exception eX)
				{
					FiddlerApplication.Log.LogFormat("Failed to write CER file: {0}", new object[] { eX.ToString() });
				}
			}
			if (BCCertMaker.bDebugSpew)
			{
				FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> BC-to-.NET Conversion took: {0}ms.", new object[] { oSW.ElapsedMilliseconds });
			}
			return certDotNet;
		}

		/// <summary>
		/// Generates (or retrieves from cache) a Public/Private keypair to attach to an EE Certificate
		/// </summary>
		/// <param name="sCN">The CN for the certificate being generated (used for Logging only)</param>
		/// <returns>A KeyPair</returns>
		// Token: 0x06000014 RID: 20 RVA: 0x00002978 File Offset: 0x00000B78
		private AsymmetricCipherKeyPair _GetPublicPrivateKeyPair(string sCN)
		{
			AsymmetricCipherKeyPair keyPair;
			if (BCCertMaker.bReuseServerKey | BCCertMaker.bReuseRootKeyAsServerKey)
			{
				keyPair = this.oEEKeyPair;
				if (keyPair != null)
				{
					goto IL_A1;
				}
				object obj = this.oEEKeyLock;
				lock (obj)
				{
					keyPair = this.oEEKeyPair;
					if (keyPair == null)
					{
						if (BCCertMaker.bReuseRootKeyAsServerKey && this.oCAKey != null)
						{
							if (BCCertMaker.bDebugSpew)
							{
								FiddlerApplication.Log.LogFormat("Reusing the RootKey as the EEKey.", Array.Empty<object>());
							}
							keyPair = (this.oEEKeyPair = new AsymmetricCipherKeyPair(this.oCACert.GetPublicKey(), this.oCAKey));
						}
						else
						{
							keyPair = (this.oEEKeyPair = BCCertMaker._GenerateKeyPair());
						}
					}
					goto IL_A1;
				}
			}
			keyPair = BCCertMaker._GenerateKeyPair();
			IL_A1:
			BCCertMaker.LogAKey(sCN, keyPair);
			return keyPair;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002A40 File Offset: 0x00000C40
		private static void LogAKey(string sCN, AsymmetricCipherKeyPair keyPair)
		{
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.LogPrivateKeys", false))
			{
				PrivateKeyInfo privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);
				byte[] arrKey = privateKeyInfo.ToAsn1Object().GetDerEncoded();
				FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> Private Key for {0}: {1}", new object[]
				{
					sCN,
					Convert.ToBase64String(arrKey)
				});
			}
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002A9C File Offset: 0x00000C9C
		private static AsymmetricCipherKeyPair _GenerateKeyPair()
		{
			SecureRandom random = new SecureRandom();
			RsaKeyPairGenerator rsaFactory = new RsaKeyPairGenerator();
			rsaFactory.Init(new KeyGenerationParameters(random, BCCertMaker.iCertBitness));
			return rsaFactory.GenerateKeyPair();
		}

		/// <summary>
		/// Called to make a new cert.
		/// </summary>
		/// <param name="sHostname"></param>
		/// <returns></returns>
		// Token: 0x06000017 RID: 23 RVA: 0x00002AD0 File Offset: 0x00000CD0
		private X509Certificate2 MakeNewCert(string sHostname)
		{
			if (BCCertMaker.bDebugSpew)
			{
				FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> Asked to MakeNewCert({0}) from thread {1}...", new object[]
				{
					sHostname,
					Thread.CurrentThread.ManagedThreadId
				});
			}
			X509Certificate2 certNew;
			try
			{
				this._RWLockForQueue.AcquireReaderLock(-1);
				ManualResetEvent oWaitForIt;
				this.dictCreationQueue.TryGetValue(sHostname, out oWaitForIt);
				this._RWLockForQueue.ReleaseLock();
				if (oWaitForIt != null)
				{
					return this.ReturnCertWhenReady(sHostname, oWaitForIt);
				}
				this._RWLockForQueue.AcquireWriterLock(-1);
				if (this.dictCreationQueue.ContainsKey(sHostname))
				{
					this._RWLockForQueue.ReleaseWriterLock();
					return this.ReturnCertWhenReady(sHostname, this.dictCreationQueue[sHostname]);
				}
				ManualResetEvent oAnnounceToOthers = new ManualResetEvent(false);
				this.dictCreationQueue.Add(sHostname, oAnnounceToOthers);
				this._RWLockForQueue.ReleaseWriterLock();
				if (BCCertMaker.bDebugSpew)
				{
					FiddlerApplication.Log.LogFormat("Proceeding to generate ({0}) on thread {1}.", new object[]
					{
						sHostname,
						Thread.CurrentThread.ManagedThreadId
					});
				}
				this.EnsureRootCertificate();
				certNew = this.CreateCertificateFromCA(sHostname, this.oCACert, this.oCAKey);
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> Failed to create certificate for {0}: {1}\n{2}", new object[] { sHostname, eX.Message, eX.StackTrace });
				this.SignalCertificateReady(sHostname);
				return null;
			}
			try
			{
				this._RWLockForCache.AcquireWriterLock(-1);
				if (BCCertMaker.bDebugSpew)
				{
					FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> Caching EECert for {0}", new object[] { sHostname });
				}
				this.certCache[sHostname] = certNew;
			}
			finally
			{
				this._RWLockForCache.ReleaseWriterLock();
			}
			this.SignalCertificateReady(sHostname);
			return certNew;
		}

		/// <summary>
		/// Waits on the provided event until it is signaled, then returns the contents of the Cert Cache for the specified sHostname
		/// </summary>
		/// <param name="sHostname">The hostname of a Certificate which is pending creation</param>
		/// <param name="oWaitForIt">The event which will be signaled when the cert is ready (max wait is 15 seconds)</param>
		/// <returns>The Certificate (or possibly null)</returns>
		// Token: 0x06000018 RID: 24 RVA: 0x00002CA0 File Offset: 0x00000EA0
		private X509Certificate2 ReturnCertWhenReady(string sHostname, ManualResetEvent oWaitForIt)
		{
			if (BCCertMaker.bDebugSpew)
			{
				FiddlerApplication.Log.LogFormat("/Queue indicated that creation of certificate [{0}] was in-progress. Waiting up to {1}ms on thread: #{2}", new object[]
				{
					sHostname,
					this.iParallelTimeout,
					Thread.CurrentThread.ManagedThreadId
				});
			}
			if (oWaitForIt.WaitOne(this.iParallelTimeout, false))
			{
				if (BCCertMaker.bDebugSpew)
				{
					FiddlerApplication.Log.LogFormat("/Got Signal that certificate [{0}] was ready. Returning to thread #{1}.", new object[]
					{
						sHostname,
						Thread.CurrentThread.ManagedThreadId
					});
				}
			}
			else
			{
				FiddlerApplication.Log.LogFormat("!Fiddler Timed out waiting for Signal that certificate [{0}] was ready. Returning to thread #{1}.", new object[]
				{
					sHostname,
					Thread.CurrentThread.ManagedThreadId
				});
			}
			X509Certificate2 result;
			try
			{
				result = this.certCache[sHostname];
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("!Certificate cache didn't find certificate for [{0}]. Returning null to thread #{1}.", new object[]
				{
					sHostname,
					Thread.CurrentThread.ManagedThreadId
				});
				result = null;
			}
			return result;
		}

		/// <summary>
		/// Signals anyone waiting that the certificate desired is now available.
		/// </summary>
		/// <param name="sHostname">Hostname of the target certificate</param>
		// Token: 0x06000019 RID: 25 RVA: 0x00002DAC File Offset: 0x00000FAC
		private void SignalCertificateReady(string sHostname)
		{
			try
			{
				this._RWLockForQueue.AcquireWriterLock(-1);
				ManualResetEvent oToNotify;
				if (this.dictCreationQueue.TryGetValue(sHostname, out oToNotify))
				{
					if (BCCertMaker.bDebugSpew)
					{
						FiddlerApplication.Log.LogFormat("/Signaling [{0}] is ready, created by thread {1}.", new object[]
						{
							sHostname,
							Thread.CurrentThread.ManagedThreadId
						});
					}
					this.dictCreationQueue.Remove(sHostname);
					oToNotify.Set();
				}
				else
				{
					FiddlerApplication.Log.LogFormat("!Fiddler.BCCertmaker> Didn't find Event object to notify for {0}", new object[] { sHostname });
				}
			}
			finally
			{
				this._RWLockForQueue.ReleaseWriterLock();
			}
		}

		/// <summary>
		/// Ensure that the Root Certificate exists, loading or generating it if necessary. 
		/// Throws if the root could not be ensured.
		/// </summary>
		// Token: 0x0600001A RID: 26 RVA: 0x00002E54 File Offset: 0x00001054
		private void EnsureRootCertificate()
		{
			if ((this.oCACert == null || this.oCAKey == null) && !this.CreateRootCertificate())
			{
				throw new InvalidOperationException("Unable to create required BC Root certificate.");
			}
		}

		/// <summary>
		/// Finds cert, uses Reader lock.
		/// </summary>
		/// <param name="sHostname"></param>
		/// <returns></returns>
		// Token: 0x0600001B RID: 27 RVA: 0x00002E7C File Offset: 0x0000107C
		public X509Certificate2 GetCertificateForHost(string sHostname)
		{
			if (this.UseWildcards && sHostname.OICEndsWithAny(this.arrWildcardTLDs) && Utilities.IndexOfNth(sHostname, 2, '.') > 0)
			{
				sHostname = "*." + Utilities.TrimBefore(sHostname, ".");
			}
			X509Certificate2 oResult;
			try
			{
				this._RWLockForCache.AcquireReaderLock(-1);
				if (this.certCache.TryGetValue(sHostname, out oResult))
				{
					return oResult;
				}
			}
			finally
			{
				this._RWLockForCache.ReleaseReaderLock();
			}
			oResult = this.MakeNewCert(sHostname);
			return oResult;
		}

		/// <summary>
		/// Store a generated Root Certificate and PrivateKey in Preferences.
		/// </summary>
		// Token: 0x0600001C RID: 28 RVA: 0x00002F0C File Offset: 0x0000110C
		private void StoreRootInPreference()
		{
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.ReuseRoot", true))
			{
				byte[] arrCert = this.oCACert.GetEncoded();
				FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.cert", Convert.ToBase64String(arrCert));
				PrivateKeyInfo privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(this.oCAKey);
				byte[] arrKey = privateKeyInfo.ToAsn1Object().GetDerEncoded();
				FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.key", Convert.ToBase64String(arrKey));
			}
		}

		/// <summary>
		/// Load a previously-generated Root Certificate and PrivateKey from Preferences.
		/// </summary>
		/// <returns></returns>
		// Token: 0x0600001D RID: 29 RVA: 0x00002F7C File Offset: 0x0000117C
		private bool ReloadRootFromPreference()
		{
			if (!FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.ReuseRoot", true))
			{
				return false;
			}
			string sCert = FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.cert", null);
			string sKey = FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.key", null);
			if (string.IsNullOrEmpty(sCert) || string.IsNullOrEmpty(sKey))
			{
				return false;
			}
			try
			{
				X509CertificateParser oCP = new X509CertificateParser();
				this.oCACert = oCP.ReadCertificate(Convert.FromBase64String(sCert));
				this.oCAKey = PrivateKeyFactory.CreateKey(Convert.FromBase64String(sKey));
				if (BCCertMaker.bDebugSpew)
				{
					FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> Loaded root certificate and key from Preference. SubjectDN:{0}", new object[] { this.oCACert.SubjectDN.ToString() });
				}
				return true;
			}
			catch (Exception eX)
			{
				if (BCCertMaker.bDebugSpew)
				{
					FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> Warning: Unable to reload root certificate and key: {0}. Regenerating.", new object[] { eX.Message });
				}
			}
			this.oCACert = null;
			this.oCAKey = null;
			FiddlerApplication.Prefs.RemovePref("fiddler.certmaker.bc.cert");
			FiddlerApplication.Prefs.RemovePref("fiddler.certmaker.bc.key");
			return false;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00003098 File Offset: 0x00001298
		public bool CreateRootCertificate()
		{
			object obj = this.oCALock;
			lock (obj)
			{
				if (this.oCAKey != null && this.oCACert != null)
				{
					if (BCCertMaker.bDebugSpew)
					{
						FiddlerApplication.Log.LogString("Root Certificate was already created by another thread. Reusing...");
					}
					return true;
				}
				if (this.ReloadRootFromPreference())
				{
					return true;
				}
				string sSigAlg = FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.Root.SigAlg", this._sDefaultHash);
				if (BCCertMaker.bDebugSpew)
				{
					FiddlerApplication.Log.LogFormat("!Fiddler.BCCertMaker> Creating new Root certificate from thread #{0}\n\tKey: {1}-bit\n\tSigAlg: {2}\n", new object[]
					{
						Thread.CurrentThread.ManagedThreadId,
						BCCertMaker.iRootCertBitness,
						sSigAlg
					});
				}
				RsaKeyPairGenerator rsaFactory = new RsaKeyPairGenerator();
				rsaFactory.Init(new KeyGenerationParameters(new SecureRandom(new CryptoApiRandomGenerator()), BCCertMaker.iRootCertBitness));
				AsymmetricCipherKeyPair keyPair = rsaFactory.GenerateKeyPair();
				X509V3CertificateGenerator v3CertGen = new X509V3CertificateGenerator();
				BigInteger serialNumber = BigInteger.ProbablePrime(120, new Random());
				v3CertGen.SetSerialNumber(serialNumber);
				X509Name certName = new X509Name(string.Format("OU={0}, O={1}, CN={2}", this.GetCertOU(), this.GetCertO(), BCCertMaker.GetRootCN()));
				v3CertGen.SetIssuerDN(certName);
				v3CertGen.SetSubjectDN(certName);
				v3CertGen.SetNotBefore(DateTime.Today.AddDays(-7.0));
				v3CertGen.SetNotAfter(DateTime.Now.AddYears(FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.bc.Root.YearsValid", 10)));
				v3CertGen.SetPublicKey(keyPair.Public);
				v3CertGen.SetSignatureAlgorithm(sSigAlg);
				v3CertGen.AddExtension(X509Extensions.BasicConstraints, FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.Root.CriticalBasicConstraints", true), new BasicConstraints(0));
				v3CertGen.AddExtension(X509Extensions.KeyUsage, FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.Root.CriticalKeyUsage", true), new KeyUsage(4));
				v3CertGen.AddExtension(X509Extensions.SubjectKeyIdentifier, FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.Root.CriticalSKID", false), new SubjectKeyIdentifierStructure(keyPair.Public));
				if (FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.Root.SetAKID", false))
				{
					v3CertGen.AddExtension(X509Extensions.AuthorityKeyIdentifier, FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.Root.CriticalAKID", false), new AuthorityKeyIdentifierStructure(keyPair.Public));
				}
				this.oCACert = v3CertGen.Generate(keyPair.Private);
				this.oCAKey = keyPair.Private;
			}
			this.StoreRootInPreference();
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.EmitRootCertFile", false))
			{
				string pfxFile = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + Path.DirectorySeparatorChar.ToString() + "FiddlerBCRoot.pfx";
				this.WriteRootCertificateAndPrivateKeyToPkcs12File(pfxFile, null, null);
			}
			if (BCCertMaker.bDebugSpew)
			{
				FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> Root certificate created.", Array.Empty<object>());
			}
			return true;
		}

		// Token: 0x0600001F RID: 31 RVA: 0x0000336C File Offset: 0x0000156C
		public X509Certificate2 GetRootCertificate()
		{
			if (this.oCACert == null && !this.ReloadRootFromPreference())
			{
				return null;
			}
			return BCCertMaker.ConvertBCCertToDotNetCert(this.oCACert);
		}

		// Token: 0x06000020 RID: 32 RVA: 0x0000338B File Offset: 0x0000158B
		private Task Delay(int milliseconds)
		{
			return Task.Delay(milliseconds);
		}

		/// <summary>
		/// Copies the Root certificate into the Current User's Root store. This will show a prompt even if run at Admin.
		/// </summary>
		/// <returns></returns>
		// Token: 0x06000021 RID: 33 RVA: 0x00003394 File Offset: 0x00001594
		public bool TrustRootCertificate()
		{
			if (this.oCACert == null)
			{
				FiddlerApplication.Log.LogString("Fiddler.BCCertMaker> Unable to trust Root certificate; not found.");
				return false;
			}
			bool result;
			try
			{
				Task delayTask = this.Delay(200);
				delayTask.Wait();
				Task<bool> trustTask = Task.Run<bool>(() => BCCertMaker.AddBCCertToStore(BCCertMaker.GetRootFriendly(), this.oCACert, StoreLocation.CurrentUser, StoreName.Root));
				delayTask = this.Delay(200);
				delayTask.Wait();
				BCCertMaker.SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle.ToInt32());
				trustTask.Wait();
				result = trustTask.Result;
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> Failed to trust Root certificate: {0}", new object[] { eX.Message });
				result = false;
			}
			return result;
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00003450 File Offset: 0x00001650
		public bool rootCertIsTrusted(out bool bUserTrusted, out bool bMachineTrusted)
		{
			bUserTrusted = BCCertMaker.IsRootCertificateTrusted(StoreLocation.CurrentUser);
			bMachineTrusted = BCCertMaker.IsRootCertificateTrusted(StoreLocation.LocalMachine);
			return bUserTrusted | bMachineTrusted;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00003468 File Offset: 0x00001668
		public bool CacheCertificateForHost(string sHost, X509Certificate2 oCert)
		{
			try
			{
				this._RWLockForCache.AcquireWriterLock(-1);
				this.certCache[sHost] = oCert;
			}
			finally
			{
				this._RWLockForCache.ReleaseWriterLock();
			}
			return true;
		}

		/// <summary>
		/// Clears the in-memory caches including the Root Certificate.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method does not delete the private keys of the certificates.
		/// </para>
		/// <para>
		/// In order to delete them, please cast this instance to <see cref="T:Fiddler.ICertificateProvider4" />
		/// and get a copy of the cache by using the <see cref="P:Fiddler.ICertificateProvider4.CertCache" /> property.
		/// </para>
		/// </remarks>
		/// <returns>TRUE if successful</returns>
		// Token: 0x06000024 RID: 36 RVA: 0x000034B0 File Offset: 0x000016B0
		public bool ClearCertificateCache()
		{
			return this.ClearCertificateCache(true);
		}

		/// <summary>
		/// Clears the in-memory caches.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method does not delete the private keys of the certificates.
		/// </para>
		/// <para>
		/// In order to delete them, please cast this instance to <see cref="T:Fiddler.ICertificateProvider4" />
		/// and get a copy of the cache by using the <see cref="P:Fiddler.ICertificateProvider4.CertCache" /> property.
		/// </para>
		/// </remarks>
		/// <param name="bClearRoot">TRUE to clear the Root Certificate from the cache.</param>
		/// <returns>TRUE if successful</returns>
		// Token: 0x06000025 RID: 37 RVA: 0x000034BC File Offset: 0x000016BC
		public bool ClearCertificateCache(bool bClearRoot)
		{
			if (BCCertMaker.bDebugSpew)
			{
				FiddlerApplication.Log.LogString("Fiddler.BCCertMaker> Begin certificate cache cleanup.");
			}
			bool result = true;
			bool result2;
			try
			{
				this._InternalFlushEECertCache();
				if (BCCertMaker.bDebugSpew)
				{
					FiddlerApplication.Log.LogString("Fiddler.BCCertMaker> Purged in-memory cache.");
				}
				if (bClearRoot)
				{
					FiddlerApplication.Prefs.RemovePref("fiddler.certmaker.bc.cert");
					FiddlerApplication.Prefs.RemovePref("fiddler.certmaker.bc.key");
					this.oCACert = null;
					this.oCAKey = null;
					X509Certificate2Collection oToRemove = BCCertMaker.FindCertsByIssuer(StoreName.Root, StoreLocation.CurrentUser, string.Format("CN={0}, O={1}, OU={2}", BCCertMaker.GetRootCN(), this.GetCertO(), this.GetCertOU()));
					if (oToRemove.Count > 0)
					{
						if (BCCertMaker.bDebugSpew)
						{
							FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> Removing {0} certificates from Windows Current User Root Store", new object[] { oToRemove.Count });
						}
						try
						{
							X509Store certStore = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
							certStore.Open(OpenFlags.ReadWrite | OpenFlags.OpenExistingOnly);
							try
							{
								certStore.RemoveRange(oToRemove);
							}
							catch
							{
								result = false;
							}
							certStore.Close();
						}
						catch
						{
							result = false;
						}
					}
				}
				if (BCCertMaker.bDebugSpew)
				{
					FiddlerApplication.Log.LogFormat("Fiddler.BCCertMaker> Finished clearing certificate cache (EE{0}).", new object[] { bClearRoot ? "+Root" : " only" });
				}
				result2 = result;
			}
			catch (Exception eX)
			{
				string title = "BCCertMaker Cleanup Failed";
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[]
				{
					title,
					eX.ToString()
				});
				result2 = false;
			}
			return result2;
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000026 RID: 38 RVA: 0x00003660 File Offset: 0x00001860
		public IDictionary<string, X509Certificate2> CertCache
		{
			get
			{
				this._RWLockForCache.AcquireReaderLock(-1);
				IDictionary<string, X509Certificate2> copy = new Dictionary<string, X509Certificate2>(this.certCache);
				this._RWLockForCache.ReleaseReaderLock();
				return copy;
			}
		}

		/// <summary>
		/// Reads the root certificate and its private key from a PKCS#12 formated stream.
		/// </summary>
		/// <param name="pkcs12Stream">The PKCS#12 formated stream.</param>
		/// <param name="password">The password which is used to protect the private key. Could be null or empty if the private key is not protected.</param>
		/// <param name="alias">The alias for the certificate and the private key. If null, the first alias found (if any) will be used.</param>
		// Token: 0x06000027 RID: 39 RVA: 0x00003694 File Offset: 0x00001894
		public void ReadRootCertificateAndPrivateKeyFromStream(Stream pkcs12Stream, string password, string alias = null)
		{
			this.ValidateArgumentIsNotNull(pkcs12Stream, "pkcs12Stream");
			Pkcs12Store pkcs12Store = this.GetPkcs12Store();
			pkcs12Store.Load(pkcs12Stream, this.PasswordToCharArray(password));
			if (alias == null)
			{
				using (IEnumerator enumerator = pkcs12Store.Aliases.GetEnumerator())
				{
					if (enumerator.MoveNext())
					{
						string a = (string)enumerator.Current;
						alias = a;
					}
				}
			}
			X509CertificateEntry certificateEntry = pkcs12Store.GetCertificate(alias);
			AsymmetricKeyEntry keyEntry = pkcs12Store.GetKey(alias);
			if (certificateEntry == null || keyEntry == null)
			{
				throw new ArgumentException("No certificate and private key with alias: '" + alias + "' were found.");
			}
			this.oCACert = certificateEntry.Certificate;
			this.oCAKey = keyEntry.Key;
		}

		/// <summary>
		/// Writes the root certificate and its private key to a PKCS#12 stream.
		/// </summary>
		/// <param name="pkcs12Stream">The PKCS#12 stream.</param>
		/// <param name="password">The password which is used to protect the private key. If null or empty, the private key is written unprotected.</param>
		/// <param name="alias">The alias for the certificate and the private key. If null, a random alias will be created.</param>
		// Token: 0x06000028 RID: 40 RVA: 0x00003758 File Offset: 0x00001958
		public void WriteRootCertificateAndPrivateKeyToStream(Stream pkcs12Stream, string password, string alias = null)
		{
			this.ValidateRootCertificateAndPrivateKeyAreInitialized();
			this.ValidateArgumentIsNotNull(pkcs12Stream, "pkcs12Stream");
			SecureRandom random = new SecureRandom();
			if (alias == null)
			{
				alias = BitConverter.ToString(BitConverter.GetBytes(random.NextLong()));
			}
			Pkcs12Store pkcs12Store = this.GetPkcs12Store();
			pkcs12Store.SetKeyEntry(alias, new AsymmetricKeyEntry(this.oCAKey), new X509CertificateEntry[]
			{
				new X509CertificateEntry(this.oCACert)
			});
			pkcs12Store.Save(pkcs12Stream, this.PasswordToCharArray(password), random);
		}

		/// <summary>
		/// Writes the root certificate without the private key to a stream using DER encoding.
		/// </summary>
		/// <param name="stream">The stream.</param>
		// Token: 0x06000029 RID: 41 RVA: 0x000037D0 File Offset: 0x000019D0
		public void WriteRootCertificateToStream(Stream stream)
		{
			this.ValidateRootCertificateIsInitialized();
			this.ValidateArgumentIsNotNull(stream, "stream");
			X509Certificate2 rootCertificate = BCCertMaker.ConvertBCCertToDotNetCert(this.oCACert);
			byte[] rootCertificateByteArray = rootCertificate.Export(X509ContentType.Cert);
			stream.Write(rootCertificateByteArray, 0, rootCertificateByteArray.Length);
		}

		/// <summary>
		/// Reads the root certificate and its private key from the PKCS#12 file (.pfx | .p12).
		/// </summary>
		/// <param name="filename">The filename of the PKCS#12 file (.pfx | .p12)</param>
		/// <param name="password">The password which is used to protect the private key.</param>
		/// <param name="alias">The alias for the certificate and the private key. If null, the first alias in the pkcs12 will be used.</param>
		// Token: 0x0600002A RID: 42 RVA: 0x00003810 File Offset: 0x00001A10
		public void ReadRootCertificateAndPrivateKeyFromPkcs12File(string filename, string password, string alias = null)
		{
			using (FileStream fileStream = new FileStream(filename, FileMode.Open))
			{
				this.ReadRootCertificateAndPrivateKeyFromStream(fileStream, password, alias);
			}
		}

		/// <summary>
		/// Writes the root certificate and its private key to a PKCS#12 file (.pfx | .p12).
		/// </summary>
		/// <param name="filename">The filename of the PKCS#12 file (.pfx | .p12).</param>
		/// <param name="password">The password which is used to protect the private key.</param>
		/// <param name="alias">The alias for the certificate and the private key. If null, a random alias will be created.</param>
		// Token: 0x0600002B RID: 43 RVA: 0x0000384C File Offset: 0x00001A4C
		public void WriteRootCertificateAndPrivateKeyToPkcs12File(string filename, string password, string alias = null)
		{
			using (FileStream stream = new FileStream(filename, FileMode.CreateNew))
			{
				this.WriteRootCertificateAndPrivateKeyToStream(stream, password, alias);
			}
		}

		/// <summary>
		/// Writes the root certificate without the private key to a DER encoded file(.cer | .crt | .der).
		/// </summary>
		/// <param name="filename">The filename of the DER encoded file (.cer | .crt | .der)</param>
		// Token: 0x0600002C RID: 44 RVA: 0x00003888 File Offset: 0x00001A88
		public void WriteRootCertificateToDerEncodedFile(string filename)
		{
			using (FileStream stream = new FileStream(filename, FileMode.CreateNew))
			{
				this.WriteRootCertificateToStream(stream);
			}
		}

		// Token: 0x0600002D RID: 45 RVA: 0x000038C0 File Offset: 0x00001AC0
		private Pkcs12Store GetPkcs12Store()
		{
			Pkcs12StoreBuilder pkcs12StoreBuilder = new Pkcs12StoreBuilder();
			return pkcs12StoreBuilder.Build();
		}

		// Token: 0x0600002E RID: 46 RVA: 0x000038D9 File Offset: 0x00001AD9
		private char[] PasswordToCharArray(string password)
		{
			if (string.IsNullOrEmpty(password))
			{
				return new char[0];
			}
			return password.ToCharArray();
		}

		// Token: 0x0600002F RID: 47 RVA: 0x000038F0 File Offset: 0x00001AF0
		private void ValidateArgumentIsNotNull(object arg, string argName)
		{
			if (arg == null)
			{
				throw new ArgumentNullException(argName, "The argument '" + argName + "' cannot be null.");
			}
		}

		// Token: 0x06000030 RID: 48 RVA: 0x0000390C File Offset: 0x00001B0C
		private void ValidateRootCertificateIsInitialized()
		{
			if (this.oCACert == null)
			{
				throw new InvalidOperationException("There is no root certificate.");
			}
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00003921 File Offset: 0x00001B21
		private void ValidateRootCertificateAndPrivateKeyAreInitialized()
		{
			this.ValidateRootCertificateIsInitialized();
			if (this.oCAKey == null)
			{
				throw new InvalidOperationException("There is no root certificate private key.");
			}
		}

		/// <summary>
		/// Dispose by clearing all of the EE Certificates' private keys, preventing pollution of the user's \Microsoft\Crypto\RSA\ folder.
		/// </summary>
		// Token: 0x06000032 RID: 50 RVA: 0x0000393C File Offset: 0x00001B3C
		public void Dispose()
		{
			this._InternalFlushEECertCache();
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00003944 File Offset: 0x00001B44
		public string GetConfigurationString()
		{
			StringBuilder sbInfo = new StringBuilder();
			sbInfo.AppendFormat("Certificate Engine:\t{0}\n", base.GetType());
			sbInfo.AppendFormat("Engine Version:\t{0}\n\n", FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion.ToString());
			sbInfo.AppendFormat("ValidFrom:\t{0} days ago\n", -FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.bc.EE.CreatedDaysAgo", -7));
			sbInfo.AppendFormat("ValidFor:\t\t{0} years\n", FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.bc.EE.YearsValid", 2));
			sbInfo.AppendFormat("HashAlg:\t\t{0}\n", this._sDefaultHash);
			sbInfo.AppendFormat("KeyLen:\t\t{0}\n", FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.bc.KeyLength", 2048));
			sbInfo.AppendFormat("RootKeyLen:\t{0}\n", FiddlerApplication.Prefs.GetInt32Pref("fiddler.certmaker.bc.RootKeyLength", 2048));
			sbInfo.AppendFormat("ReuseServerKeys:\t{0}\n", FiddlerApplication.Prefs.GetBoolPref("fiddler.certmaker.bc.ReusePrivateKeys", true));
			return sbInfo.ToString();
		}

		/// <summary>
		/// How long should we wait for parallel creations
		/// </summary>
		// Token: 0x04000001 RID: 1
		private int iParallelTimeout = 25000;

		/// <summary>
		/// "SHA256WITHRSA", "SHA384WITHRSA", "SHA512WITHRSA", "MD5WITHRSA", etc
		/// </summary>
		// Token: 0x04000002 RID: 2
		private string _sDefaultHash = "SHA256WITHRSA";

		/// <summary>
		/// Cache of EndEntity certificates that have been generated in this session.
		/// </summary>
		// Token: 0x04000003 RID: 3
		private Dictionary<string, X509Certificate2> certCache = new Dictionary<string, X509Certificate2>();

		/// <summary>
		/// The ReaderWriter lock gates access to the certCache
		/// </summary>
		// Token: 0x04000004 RID: 4
		private ReaderWriterLock _RWLockForCache = new ReaderWriterLock();

		/// <summary>
		/// Queue of creations in progress, indexed by certificate CN.
		/// ManualResetEvent info: http://msdn.microsoft.com/en-us/library/ksb7zs2x(v=vs.95).aspx
		/// </summary>
		// Token: 0x04000005 RID: 5
		private Dictionary<string, ManualResetEvent> dictCreationQueue = new Dictionary<string, ManualResetEvent>();

		/// <summary>
		/// The ReaderWriter lock gates access to the Queue which ensures we only have one Certificate-Generating-per-Host
		/// </summary>
		// Token: 0x04000006 RID: 6
		private ReaderWriterLock _RWLockForQueue = new ReaderWriterLock();

		/// <summary>
		/// The BouncyCastle Root certificate
		/// </summary>
		// Token: 0x04000007 RID: 7
		private X509Certificate oCACert;

		/// <summary>
		/// The BouncyCastle Root Private key
		/// </summary>
		// Token: 0x04000008 RID: 8
		private AsymmetricKeyParameter oCAKey;

		/// <summary>
		/// The EE Certificate Public/Private key that we'll reuse for all EE certificates if the
		/// preference fiddler.certmaker.bc.ReusePrivateKeys is set.
		/// </summary>
		// Token: 0x04000009 RID: 9
		private AsymmetricCipherKeyPair oEEKeyPair;

		/// <summary>
		/// Object we use to lock on when updating oEEKeyPair
		/// </summary>
		// Token: 0x0400000A RID: 10
		private object oEEKeyLock = new object();

		/// <summary>
		/// Object we use to lock on when updating oCACert / OCAKey
		/// </summary>
		// Token: 0x0400000B RID: 11
		private object oCALock = new object();

		/// <summary>
		/// Should Fiddler automatically generate wildcard certificates?
		/// </summary>
		// Token: 0x0400000C RID: 12
		private bool UseWildcards;

		/// <summary>
		/// TLDs for which should Fiddler generate wildcarded 3rd-level-domain certs
		/// </summary>
		// Token: 0x0400000D RID: 13
		private readonly string[] arrWildcardTLDs = new string[] { ".com", ".org", ".edu", ".gov", ".net" };
	}
}
