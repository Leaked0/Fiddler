using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Fiddler;
using FiddlerCore.PlatformExtensions.API;

namespace FiddlerCore.PlatformExtensions.Unix.Mac
{
	// Token: 0x020000A1 RID: 161
	internal class PlatformExtensionsForMac : PlatformExtensionsForUnix, IPlatformExtensions
	{
		// Token: 0x0600065D RID: 1629 RVA: 0x0003592C File Offset: 0x00033B2C
		private PlatformExtensionsForMac()
		{
		}

		// Token: 0x170000FC RID: 252
		// (get) Token: 0x0600065E RID: 1630 RVA: 0x00035934 File Offset: 0x00033B34
		public static PlatformExtensionsForMac Instance
		{
			get
			{
				if (PlatformExtensionsForMac.instance == null)
				{
					PlatformExtensionsForMac.instance = new PlatformExtensionsForMac();
				}
				return PlatformExtensionsForMac.instance;
			}
		}

		// Token: 0x0600065F RID: 1631 RVA: 0x0003594C File Offset: 0x00033B4C
		public override bool IsRootCertificateTrusted()
		{
			CertMaker.EnsureReady();
			ICertificateProvider5 certificateProvider = (ICertificateProvider5)CertMaker.oCertProvider;
			string rootCertificateSha = certificateProvider.GetRootCertificate().GetCertHashString();
			string trustedCertificatesFilePath = string.Empty;
			bool result;
			try
			{
				trustedCertificatesFilePath = this.GetTrustedCertificatesFilePath();
				bool isTrusted = this.IsCertificateTrusted(trustedCertificatesFilePath, rootCertificateSha);
				result = isTrusted;
			}
			catch (Exception ex)
			{
				FiddlerApplication.Log.LogString("Error when trying to check if certificate is trusted: " + ex.Message);
				result = false;
			}
			finally
			{
				try
				{
					File.Delete(trustedCertificatesFilePath);
				}
				catch (Exception ex2)
				{
					FiddlerApplication.Log.LogString("Error when trying to delete the temp certificate file: " + ex2.Message);
				}
			}
			return result;
		}

		// Token: 0x06000660 RID: 1632 RVA: 0x00035A08 File Offset: 0x00033C08
		private string GetTrustedCertificatesFilePath()
		{
			string trustSettingsPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			string shellScript = ("security trust-settings-export \"" + trustSettingsPath + "\"").Replace("\"", "\\\"").Replace("\r\n", "\n");
			Process process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "/bin/bash",
					Arguments = "-c \"" + shellScript + "\"",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};
			process.Start();
			process.WaitForExit();
			if (process.ExitCode != 0)
			{
				throw new Exception(string.Concat(new string[]
				{
					"Unable to check the trust settings for path ",
					trustSettingsPath,
					" exit code is ",
					process.ExitCode.ToString(),
					" command is: '",
					shellScript,
					"'"
				}));
			}
			return trustSettingsPath;
		}

		// Token: 0x06000661 RID: 1633 RVA: 0x00035AFC File Offset: 0x00033CFC
		private bool IsCertificateTrusted(string pathToTrustSettingsFile, string certificateSHA1)
		{
			string content = File.ReadAllText(pathToTrustSettingsFile);
			content = content.Replace(Convert.ToChar(0).ToString(), string.Empty).Replace(Convert.ToChar(65534).ToString(), string.Empty).Replace(Convert.ToChar(65535).ToString(), string.Empty);
			XDocument doc = XDocument.Parse(content);
			XElement plist = doc.Element("plist");
			XElement dict = plist.Element("dict");
			IEnumerable<XElement> elements = dict.Elements();
			XElement trustListKeyElement = elements.FirstOrDefault((XElement el) => el.Value == "trustList");
			if (trustListKeyElement == null)
			{
				throw new Exception("Unable to find element with key 'trustList' in the exported trust settings.");
			}
			int indexOfTrustList = elements.ToList<XElement>().IndexOf(trustListKeyElement);
			XElement trustListValueRootElement = elements.ElementAtOrDefault(indexOfTrustList + 1);
			if (trustListValueRootElement == null)
			{
				throw new Exception("Unable to find the value of the trustList element, i.e. next element is null");
			}
			List<XElement> trustListElements = trustListValueRootElement.Elements().ToList<XElement>();
			XElement certificateSHA1Element = trustListElements.FirstOrDefault((XElement el) => el.Name == "key" && el.Value == certificateSHA1);
			if (certificateSHA1Element == null)
			{
				throw new Exception("Certificate with SHA1 " + certificateSHA1 + " is not found in the trusted certificates");
			}
			int indexOfCertificateSHA1Element = trustListElements.IndexOf(certificateSHA1Element);
			XElement valueOfCertificateSHA1Element = trustListElements.ElementAt(indexOfCertificateSHA1Element + 1);
			if (valueOfCertificateSHA1Element == null)
			{
				throw new Exception("Unable to find the value of the " + certificateSHA1 + " element.");
			}
			XElement trustSettingsKeyElement = valueOfCertificateSHA1Element.Elements().FirstOrDefault((XElement el) => el.Name == "key" && el.Value == "trustSettings");
			if (trustSettingsKeyElement == null)
			{
				return true;
			}
			int indexOfTrustSettingsKeyElement = valueOfCertificateSHA1Element.Elements().ToList<XElement>().IndexOf(trustSettingsKeyElement);
			XElement trustSettings = valueOfCertificateSHA1Element.Elements().ElementAt(indexOfTrustSettingsKeyElement + 1);
			if (trustSettings == null)
			{
				throw new Exception("Unable to find the value of trustSettings element.");
			}
			bool isx509Allowed = PlatformExtensionsForMac.IsTrustSettingAllowed("basicX509", trustSettings);
			bool isSSlServerAllowed = PlatformExtensionsForMac.IsTrustSettingAllowed("sslServer", trustSettings);
			FiddlerApplication.Log.LogString(string.Concat(new string[]
			{
				"For certificate with SHA1 ",
				certificateSHA1,
				" the basicX509 setting is ",
				isx509Allowed.ToString(),
				" and the sslServer is ",
				isSSlServerAllowed.ToString()
			}));
			return isx509Allowed && isSSlServerAllowed;
		}

		// Token: 0x06000662 RID: 1634 RVA: 0x00035D58 File Offset: 0x00033F58
		private static bool IsTrustSettingAllowed(string key, XElement trustSettings)
		{
			Func<XElement, bool> <>9__2;
			IEnumerable<XElement> elementsMatchingKey = trustSettings.Elements().ToList<XElement>().Where(delegate(XElement el)
			{
				IEnumerable<XElement> source = el.Elements();
				Func<XElement, bool> predicate;
				if ((predicate = <>9__2) == null)
				{
					predicate = (<>9__2 = (XElement subEl) => subEl.Name == "string" && subEl.Value == key);
				}
				return source.FirstOrDefault(predicate) != null;
			});
			if (elementsMatchingKey == null || elementsMatchingKey.Count<XElement>() == 0)
			{
				return true;
			}
			List<bool> results = elementsMatchingKey.ToList<XElement>().Select(delegate(XElement elementMatchingKey)
			{
				XElement kSecTrustSettingsResultKeyElement = elementsMatchingKey.Elements<XElement>().FirstOrDefault((XElement el) => el.Name == "key" && el.Value == "kSecTrustSettingsResult");
				if (kSecTrustSettingsResultKeyElement == null)
				{
					throw new Exception("Unable to find kSecTrustSettingsResult in the element containing basicX509 key. Probably the structure of the Plist had changed and code is not updated.");
				}
				int kSecTrustSettingsResultKeyElementIndex = elementsMatchingKey.Elements<XElement>().ToList<XElement>().IndexOf(kSecTrustSettingsResultKeyElement);
				XElement kSecTrustSettingsResultValue = elementsMatchingKey.Elements<XElement>().ElementAt(kSecTrustSettingsResultKeyElementIndex + 1);
				if (kSecTrustSettingsResultValue == null)
				{
					throw new Exception("Unable to find the value of kSecTrustSettingsResult.");
				}
				return kSecTrustSettingsResultValue.Value == "1";
			}).Distinct<bool>()
				.ToList<bool>();
			return results.Count<bool>() == 1 && results.First<bool>();
		}

		// Token: 0x06000663 RID: 1635 RVA: 0x00035DE4 File Offset: 0x00033FE4
		public override void TrustRootCertificate()
		{
			CertMaker.EnsureReady();
			ICertificateProvider5 certificateProvider = (ICertificateProvider5)CertMaker.oCertProvider;
			string certificatePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			certificateProvider.WriteRootCertificateToDerEncodedFile(certificatePath);
			try
			{
				string shellScript = ("\r\n\r\nlogin_keychains_paths=$(security list-keychains | grep -e \"\\Wlogin.keychain\\W\");\r\n\r\nif [ -z \"$login_keychains_paths\" ]\r\n    then\r\n        echo \"No login keychain found.\";\r\n        exit 10;\r\nfi\r\n\r\nsecurity add-trusted-cert -k login.keychain \"" + certificatePath + "\";\r\n\r\nsecurity_exit_code=$?;\r\n\r\nif [ $security_exit_code -ne 0 ]\r\n    then\r\n        echo \"security add-trusted-cert failed with error code $security_exit_code\";\r\n        exit $security_exit_code;\r\nfi").Replace("\"", "\\\"").Replace("\r\n", "\n");
				Process process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = "/bin/bash",
						Arguments = "-c \"" + shellScript + "\"",
						RedirectStandardOutput = true,
						UseShellExecute = false,
						CreateNoWindow = true
					}
				};
				process.Start();
				process.WaitForExit();
				int exitCode = process.ExitCode;
				if (exitCode != 0)
				{
					if (exitCode != 10)
					{
						throw new Exception("Unable to trust the root certificate. Try importing and trusting it manually.");
					}
					throw new Exception("Unable to find login.keychain. Please create one or import the certificate manually in your default keychain.");
				}
			}
			finally
			{
				File.Delete(certificatePath);
			}
		}

		// Token: 0x06000664 RID: 1636 RVA: 0x00035EE0 File Offset: 0x000340E0
		public override void UntrustRootCertificate()
		{
			if (string.IsNullOrEmpty(CONFIG.sMakeCertRootCN) || CONFIG.sMakeCertRootCN.IndexOf("fiddler", StringComparison.OrdinalIgnoreCase) == -1)
			{
				throw new ArgumentException("Fiddler Certificate name did not pass sanity check!");
			}
			string shellScript = ("\r\n\r\nlogin_keychains_paths=$(security list-keychains | grep -e \"\\Wlogin.keychain\\W\");\r\n\r\nif [ -z \"$login_keychains_paths\" ]\r\n    then\r\n        echo \"No login keychain found.\";\r\n        exit 10;\r\nfi\r\n\r\nsecurity find-certificate -a -c \"" + CONFIG.sMakeCertRootCN + "\" -Z login.keychain | \\\r\n    awk '/SHA-1/{system(\"security delete-certificate -Z \"$NF\" -t login.keychain\")}'\r\n\r\nsecurity_exit_code=$?;\r\n\r\nif [ $security_exit_code -ne 0 ]\r\n    then\r\n        echo \"security delete-certificate failed with error code $security_exit_code\";\r\n        exit $security_exit_code;\r\nfi").Replace("\"", "\\\"").Replace("\r\n", "\n");
			Process process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "/bin/bash",
					Arguments = "-c \"" + shellScript + "\"",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};
			process.Start();
			string errorResult = process.StandardError.ReadToEnd();
			process.WaitForExit();
			int exitCode = process.ExitCode;
			if (exitCode == 0 && !string.IsNullOrEmpty(errorResult) && errorResult.IndexOf("cancel", StringComparison.OrdinalIgnoreCase) != -1)
			{
				exitCode = 20;
			}
			if (exitCode == 0)
			{
				return;
			}
			if (exitCode == 10)
			{
				throw new Exception("Unable to find login.keychain. If the certificate was imported in another keychain, you must remove it manually.");
			}
			if (exitCode != 20)
			{
				throw new Exception("Unable to remove the root certificate. Try opening the login keychain and removing it manually.");
			}
			throw new Exception("Unable to remove the root certificate. The operation was cancelled.");
		}

		// Token: 0x040002DA RID: 730
		private static PlatformExtensionsForMac instance;
	}
}
