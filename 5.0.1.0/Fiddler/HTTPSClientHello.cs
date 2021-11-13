using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fiddler
{
	/// <summary>
	/// The HTTPSClientHello class is used to parse the bytes of a HTTPS ClientHello message.
	/// </summary>
	// Token: 0x02000045 RID: 69
	internal class HTTPSClientHello
	{
		// Token: 0x17000080 RID: 128
		// (get) Token: 0x060002C9 RID: 713 RVA: 0x00019543 File Offset: 0x00017743
		public string ServerNameIndicator
		{
			get
			{
				if (!string.IsNullOrEmpty(this._ServerNameIndicator))
				{
					return this._ServerNameIndicator;
				}
				return string.Empty;
			}
		}

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x060002CA RID: 714 RVA: 0x0001955E File Offset: 0x0001775E
		public string SessionID
		{
			get
			{
				if (this._SessionID == null)
				{
					return string.Empty;
				}
				return Utilities.ByteArrayToString(this._SessionID);
			}
		}

		// Token: 0x060002CB RID: 715 RVA: 0x0001957C File Offset: 0x0001777C
		private static string CipherSuitesToString(uint[] inArr)
		{
			if (inArr == null)
			{
				return "null";
			}
			if (inArr.Length == 0)
			{
				return "empty";
			}
			StringBuilder sbOutput = new StringBuilder(inArr.Length * 20);
			for (int i = 0; i < inArr.Length; i++)
			{
				sbOutput.Append("\t[" + inArr[i].ToString("X4") + "]\t");
				string sSuite;
				if ((ulong)inArr[i] < (ulong)((long)HTTPSClientHello.SSL3CipherSuites.Length))
				{
					sbOutput.AppendLine(HTTPSClientHello.SSL3CipherSuites[(int)inArr[i]]);
				}
				else if (HTTPSClientHello.dictTLSCipherSuites.TryGetValue(inArr[i], out sSuite))
				{
					sbOutput.AppendLine(sSuite);
				}
				else
				{
					sbOutput.AppendLine("Unrecognized cipher - See https://www.iana.org/assignments/tls-parameters/");
				}
			}
			return sbOutput.ToString();
		}

		// Token: 0x060002CC RID: 716 RVA: 0x0001962C File Offset: 0x0001782C
		private static string CompressionSuitesToString(byte[] inArr)
		{
			if (inArr == null)
			{
				return "(not specified)";
			}
			if (inArr.Length == 0)
			{
				return "(none)";
			}
			StringBuilder sbOutput = new StringBuilder();
			for (int i = 0; i < inArr.Length; i++)
			{
				sbOutput.Append("\t[" + inArr[i].ToString("X2") + "]\t");
				if ((int)inArr[i] < HTTPSClientHello.HTTPSCompressionSuites.Length)
				{
					sbOutput.AppendLine(HTTPSClientHello.HTTPSCompressionSuites[(int)inArr[i]]);
				}
				else
				{
					sbOutput.AppendLine("Unrecognized compression format");
				}
			}
			return sbOutput.ToString();
		}

		// Token: 0x060002CD RID: 717 RVA: 0x000196B7 File Offset: 0x000178B7
		private static string ExtensionListToString(List<string> slExts)
		{
			if (slExts == null || slExts.Count < 1)
			{
				return "\tnone";
			}
			return string.Join("\n", slExts.ToArray());
		}

		// Token: 0x060002CE RID: 718 RVA: 0x000196DC File Offset: 0x000178DC
		public override string ToString()
		{
			StringBuilder sbOutput = new StringBuilder(512);
			if (this._HandshakeVersion == 2)
			{
				sbOutput.Append("A SSLv2-compatible ClientHello handshake was found. Fiddler extracted the parameters below.\n\n");
			}
			else
			{
				sbOutput.Append("A SSLv3-compatible ClientHello handshake was found. Fiddler extracted the parameters below.\n\n");
			}
			sbOutput.AppendFormat("Version: {0}\n", HTTPSUtilities.HTTPSVersionToString(this._MajorVersion, this._MinorVersion));
			sbOutput.AppendFormat("Random: {0}\n", Utilities.ByteArrayToString(this._Random));
			uint uiSecSinceEpoch = (uint)(((int)this._Random[3] << 24) + ((int)this._Random[2] << 16) + ((int)this._Random[1] << 8) + (int)this._Random[0]);
			DateTime dtWhen = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(uiSecSinceEpoch).ToLocalTime();
			sbOutput.AppendFormat("\"Time\": {0}\n", dtWhen);
			sbOutput.AppendFormat("SessionID: {0}\n", Utilities.ByteArrayToString(this._SessionID));
			sbOutput.AppendFormat("Extensions: \n{0}\n", HTTPSClientHello.ExtensionListToString(this._Extensions));
			sbOutput.AppendFormat("Ciphers: \n{0}\n", HTTPSClientHello.CipherSuitesToString(this._CipherSuites));
			sbOutput.AppendFormat("Compression: \n{0}\n", HTTPSClientHello.CompressionSuitesToString(this._CompressionSuites));
			return sbOutput.ToString();
		}

		/// <summary>
		/// Parse ClientHello from stream. See Page 77 of SSL &amp; TLS Essentials
		/// </summary>
		// Token: 0x060002CF RID: 719 RVA: 0x00019810 File Offset: 0x00017A10
		internal bool LoadFromStream(Stream oNS)
		{
			int iProt = oNS.ReadByte();
			if (iProt == 128)
			{
				this._HandshakeVersion = 2;
				int iRecordLen = oNS.ReadByte();
				int iMsgType = oNS.ReadByte();
				this._MajorVersion = oNS.ReadByte();
				this._MinorVersion = oNS.ReadByte();
				if (this._MajorVersion == 0 && this._MinorVersion == 2)
				{
					this._MajorVersion = 2;
					this._MinorVersion = 0;
				}
				int iCiphersLen = oNS.ReadByte() << 8;
				iCiphersLen += oNS.ReadByte();
				int iSessionIDLen = oNS.ReadByte() << 8;
				iSessionIDLen += oNS.ReadByte();
				int iRandomLen = oNS.ReadByte() << 8;
				iRandomLen += oNS.ReadByte();
				this._CipherSuites = new uint[iCiphersLen / 3];
				for (int iCipher = 0; iCipher < this._CipherSuites.Length; iCipher++)
				{
					this._CipherSuites[iCipher] = (uint)((oNS.ReadByte() << 16) + (oNS.ReadByte() << 8) + oNS.ReadByte());
				}
				this._SessionID = new byte[iSessionIDLen];
				int cBytes = oNS.Read(this._SessionID, 0, this._SessionID.Length);
				this._Random = new byte[iRandomLen];
				cBytes = oNS.Read(this._Random, 0, this._Random.Length);
			}
			else if (iProt == 22)
			{
				this._HandshakeVersion = 3;
				this._MajorVersion = oNS.ReadByte();
				this._MinorVersion = oNS.ReadByte();
				int iRecordLen2 = oNS.ReadByte() << 8;
				iRecordLen2 += oNS.ReadByte();
				int iMsgType2 = oNS.ReadByte();
				if (iMsgType2 != 1)
				{
					return false;
				}
				byte[] data = new byte[3];
				int cBytes = oNS.Read(data, 0, data.Length);
				if (cBytes < 3)
				{
					return false;
				}
				this._MessageLen = ((int)data[0] << 16) + ((int)data[1] << 8) + (int)data[2];
				this._MajorVersion = oNS.ReadByte();
				this._MinorVersion = oNS.ReadByte();
				this._Random = new byte[32];
				cBytes = oNS.Read(this._Random, 0, 32);
				if (cBytes < 32)
				{
					return false;
				}
				int iSessionIDLen2 = oNS.ReadByte();
				this._SessionID = new byte[iSessionIDLen2];
				cBytes = oNS.Read(this._SessionID, 0, this._SessionID.Length);
				data = new byte[2];
				cBytes = oNS.Read(data, 0, data.Length);
				if (cBytes < 2)
				{
					return false;
				}
				int cbCiphers = ((int)data[0] << 8) + (int)data[1];
				this._CipherSuites = new uint[cbCiphers / 2];
				data = new byte[cbCiphers];
				cBytes = oNS.Read(data, 0, data.Length);
				if (cBytes != data.Length)
				{
					return false;
				}
				for (int x = 0; x < this._CipherSuites.Length; x++)
				{
					this._CipherSuites[x] = (uint)(((int)data[2 * x] << 8) + (int)data[2 * x + 1]);
				}
				int cCompressionSuites = oNS.ReadByte();
				if (cCompressionSuites < 1)
				{
					return false;
				}
				this._CompressionSuites = new byte[cCompressionSuites];
				for (int x2 = 0; x2 < this._CompressionSuites.Length; x2++)
				{
					int iSuite = oNS.ReadByte();
					if (iSuite < 0)
					{
						return false;
					}
					this._CompressionSuites[x2] = (byte)iSuite;
				}
				if (this._MajorVersion < 3 || (this._MajorVersion == 3 && this._MinorVersion < 1))
				{
					return true;
				}
				data = new byte[2];
				cBytes = oNS.Read(data, 0, data.Length);
				if (cBytes < 2)
				{
					return true;
				}
				int cExtensionsLen = ((int)data[0] << 8) + (int)data[1];
				if (cExtensionsLen < 1)
				{
					return true;
				}
				data = new byte[cExtensionsLen];
				cBytes = oNS.Read(data, 0, data.Length);
				if (cBytes == data.Length)
				{
					this.ParseClientHelloExtensions(data);
				}
			}
			return true;
		}

		/// <summary>
		/// Parse a single extension using the list from http://tools.ietf.org/html/rfc6066
		/// https://www.iana.org/assignments/tls-extensiontype-values/tls-extensiontype-values.xml
		/// https://src.chromium.org/viewvc/chrome/trunk/src/net/third_party/nss/ssl/sslt.h
		/// </summary>
		/// <param name="iExtType"></param>
		/// <param name="arrData"></param>
		// Token: 0x060002D0 RID: 720 RVA: 0x00019B84 File Offset: 0x00017D84
		private void ParseClientHelloExtension(int iExtType, byte[] arrData)
		{
			if (this._Extensions == null)
			{
				this._Extensions = new List<string>();
			}
			if (iExtType <= 30032)
			{
				if (iExtType <= 13172)
				{
					if (iExtType <= 2570)
					{
						switch (iExtType)
						{
						case 0:
						{
							StringBuilder sbHostList = new StringBuilder();
							int cbHostLen;
							for (int iPtr = 2; iPtr < arrData.Length; iPtr += 3 + cbHostLen)
							{
								int iHostType = (int)arrData[iPtr];
								cbHostLen = ((int)arrData[iPtr + 1] << 8) + (int)arrData[iPtr + 2];
								string sHost = Encoding.ASCII.GetString(arrData, iPtr + 3, cbHostLen);
								if (iHostType == 0)
								{
									this._ServerNameIndicator = sHost;
									sbHostList.AppendFormat("{0}{1}", (sbHostList.Length > 1) ? "; " : string.Empty, sHost);
								}
							}
							this._Extensions.Add(string.Format("\tserver_name\t{0}", sbHostList.ToString()));
							return;
						}
						case 1:
							this._Extensions.Add(string.Format("\tmax_fragment_length\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 2:
							this._Extensions.Add(string.Format("\tclient_certificate_url\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 3:
							this._Extensions.Add(string.Format("\ttrusted_ca_keys\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 4:
							this._Extensions.Add(string.Format("\ttruncated_hmac\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 5:
						{
							string sStatusRequest = Utilities.ByteArrayToString(arrData);
							if (sStatusRequest == "01 00 00 00 00")
							{
								sStatusRequest = "OCSP - Implicit Responder";
							}
							this._Extensions.Add(string.Format("\tstatus_request\t{0}", sStatusRequest));
							return;
						}
						case 6:
							this._Extensions.Add(string.Format("\tuser_mapping\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 7:
						case 8:
						case 26:
						case 27:
						case 28:
						case 29:
						case 30:
						case 31:
						case 32:
						case 33:
						case 34:
						case 36:
						case 37:
						case 38:
						case 39:
						case 48:
							goto IL_757;
						case 9:
							this._Extensions.Add(string.Format("\tcert_type\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 10:
							this._Extensions.Add(string.Format("\tsupported_groups\t{0}", HTTPSUtilities.GetSupportedGroupsAsString(arrData)));
							return;
						case 11:
							this._Extensions.Add(string.Format("\tec_point_formats\t{0}", HTTPSUtilities.GetECCPointFormatsAsString(arrData)));
							return;
						case 12:
							this._Extensions.Add(string.Format("\tsrp_rfc_5054\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 13:
							this._Extensions.Add(string.Format("\tsignature_algs\t{0}", HTTPSUtilities.GetSignatureAndHashAlgsAsString(arrData)));
							return;
						case 14:
							this._Extensions.Add(string.Format("\tuse_srtp\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 15:
							this._Extensions.Add(string.Format("\theartbeat_rfc_6520\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 16:
						{
							string sALPN = HTTPSUtilities.GetProtocolListAsString(arrData);
							this._Extensions.Add(string.Format("\tALPN\t\t{0}", sALPN));
							return;
						}
						case 17:
							this._Extensions.Add(string.Format("\tstatus_request_v2 (RFC6961)\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 18:
							this._Extensions.Add(string.Format("\tSignedCertTimestamp (RFC6962)\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 19:
							this._Extensions.Add(string.Format("\tClientCertificateType (RFC7250)\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 20:
							this._Extensions.Add(string.Format("\tServerCertificateType (RFC7250)\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 21:
							this._Extensions.Add(string.Format("\tpadding\t\t{0}", HTTPSUtilities.DescribePadding(arrData)));
							return;
						case 22:
							this._Extensions.Add(string.Format("\tencrypt_then_mac (RFC7366)\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 23:
							this._Extensions.Add(string.Format("\textended_master_secret\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 24:
							this._Extensions.Add(string.Format("\ttoken_binding\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 25:
							this._Extensions.Add(string.Format("\tcached_info\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 35:
							this._Extensions.Add(string.Format("\tSessionTicket\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 40:
							this._Extensions.Add(string.Format("\tkey_share\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 41:
							this._Extensions.Add(string.Format("\tpre_shared_key\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 42:
							this._Extensions.Add(string.Format("\tearly_data\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 43:
							this._Extensions.Add(string.Format("\tsupported_versions\t{0}", HTTPSUtilities.GetSupportedVersions(arrData)));
							return;
						case 44:
							this._Extensions.Add(string.Format("\tcookie\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 45:
							this._Extensions.Add(string.Format("\tpsk_key_exchange_modes\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 46:
							this._Extensions.Add(string.Format("\tticket_early_data_info\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 47:
							this._Extensions.Add(string.Format("\tcertificate_authorities\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						case 49:
							this._Extensions.Add("\tpost_handshake_auth");
							return;
						case 50:
							this._Extensions.Add(string.Format("\tsignature_algorithms_cert\t{0}", HTTPSUtilities.GetSignatureAndHashAlgsAsString(arrData)));
							return;
						case 51:
							this._Extensions.Add(string.Format("\tkey_share\t{0}", Utilities.ByteArrayToString(arrData)));
							return;
						default:
							if (iExtType != 2570)
							{
								goto IL_757;
							}
							break;
						}
					}
					else if (iExtType != 6682 && iExtType != 10794)
					{
						if (iExtType != 13172)
						{
							goto IL_757;
						}
						this._Extensions.Add(string.Format("\tNextProtocolNego\t{0}", Utilities.ByteArrayToString(arrData)));
						return;
					}
				}
				else if (iExtType <= 21760)
				{
					if (iExtType != 14906 && iExtType != 19018)
					{
						if (iExtType != 21760)
						{
							goto IL_757;
						}
						this._Extensions.Add(string.Format("\ttoken_binding(MSDraft)\t{0}", Utilities.ByteArrayToString(arrData)));
						return;
					}
				}
				else if (iExtType != 23130 && iExtType != 27242)
				{
					if (iExtType - 30031 > 1)
					{
						goto IL_757;
					}
					this._Extensions.Add(string.Format("\tchannel_id(GoogleDraft)\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				}
			}
			else if (iExtType <= 43690)
			{
				if (iExtType <= 35466)
				{
					if (iExtType != 31354 && iExtType != 35466)
					{
						goto IL_757;
					}
				}
				else
				{
					if (iExtType == 35655)
					{
						this._Extensions.Add(string.Format("\tCompatPadding\t{0} bytes", arrData.Length.ToString()));
						return;
					}
					if (iExtType != 39578 && iExtType != 43690)
					{
						goto IL_757;
					}
				}
			}
			else if (iExtType <= 56026)
			{
				if (iExtType != 47802 && iExtType != 51914 && iExtType != 56026)
				{
					goto IL_757;
				}
			}
			else if (iExtType != 60138 && iExtType != 64250)
			{
				if (iExtType != 65281)
				{
					goto IL_757;
				}
				this._Extensions.Add(string.Format("\trenegotiation_info\t{0}", Utilities.ByteArrayToString(arrData)));
				return;
			}
			this._Extensions.Add(string.Format("\tgrease (0x{0:x})\t{1}", iExtType, Utilities.ByteArrayToString(arrData)));
			return;
			IL_757:
			this._Extensions.Add(string.Format("\t0x{0:x4}\t\t{1}", iExtType, Utilities.ByteArrayToString(arrData)));
		}

		// Token: 0x060002D1 RID: 721 RVA: 0x0001A30C File Offset: 0x0001850C
		private void ParseClientHelloExtensions(byte[] arrExtensionsData)
		{
			int iExtDataLen;
			for (int iPtr = 0; iPtr < arrExtensionsData.Length; iPtr += 4 + iExtDataLen)
			{
				int iExtensionType = ((int)arrExtensionsData[iPtr] << 8) + (int)arrExtensionsData[iPtr + 1];
				iExtDataLen = ((int)arrExtensionsData[iPtr + 2] << 8) + (int)arrExtensionsData[iPtr + 3];
				byte[] arrExtData = new byte[iExtDataLen];
				Buffer.BlockCopy(arrExtensionsData, iPtr + 4, arrExtData, 0, arrExtData.Length);
				this.ParseClientHelloExtension(iExtensionType, arrExtData);
			}
		}

		// Token: 0x0400012D RID: 301
		private int _HandshakeVersion;

		// Token: 0x0400012E RID: 302
		private int _MessageLen;

		// Token: 0x0400012F RID: 303
		private int _MajorVersion;

		// Token: 0x04000130 RID: 304
		private int _MinorVersion;

		// Token: 0x04000131 RID: 305
		private byte[] _Random;

		// Token: 0x04000132 RID: 306
		private byte[] _SessionID;

		// Token: 0x04000133 RID: 307
		private uint[] _CipherSuites;

		// Token: 0x04000134 RID: 308
		private byte[] _CompressionSuites;

		// Token: 0x04000135 RID: 309
		private string _ServerNameIndicator;

		// Token: 0x04000136 RID: 310
		private List<string> _Extensions;

		// Token: 0x04000137 RID: 311
		internal static readonly string[] HTTPSCompressionSuites = new string[] { "NO_COMPRESSION", "DEFLATE" };

		// Token: 0x04000138 RID: 312
		internal static readonly string[] SSL3CipherSuites = new string[]
		{
			"SSL_NULL_WITH_NULL_NULL", "SSL_RSA_WITH_NULL_MD5", "SSL_RSA_WITH_NULL_SHA", "SSL_RSA_EXPORT_WITH_RC4_40_MD5", "SSL_RSA_WITH_RC4_128_MD5", "SSL_RSA_WITH_RC4_128_SHA", "SSL_RSA_EXPORT_WITH_RC2_40_MD5", "SSL_RSA_WITH_IDEA_SHA", "SSL_RSA_EXPORT_WITH_DES40_SHA", "SSL_RSA_WITH_DES_SHA",
			"SSL_RSA_WITH_3DES_EDE_SHA", "SSL_DH_DSS_EXPORT_WITH_DES40_SHA", "SSL_DH_DSS_WITH_DES_SHA", "SSL_DH_DSS_WITH_3DES_EDE_SHA", "SSL_DH_RSA_EXPORT_WITH_DES40_SHA", "SSL_DH_RSA_WITH_DES_SHA", "SSL_DH_RSA_WITH_3DES_EDE_SHA", "SSL_DHE_DSS_EXPORT_WITH_DES40_SHA", "SSL_DHE_DSS_WITH_DES_SHA", "SSL_DHE_DSS_WITH_3DES_EDE_SHA",
			"SSL_DHE_RSA_EXPORT_WITH_DES40_SHA", "SSL_DHE_RSA_WITH_DES_SHA", "SSL_DHE_RSA_WITH_3DES_EDE_SHA", "SSL_DH_anon_EXPORT_WITH_RC4_40_MD5", "SSL_DH_anon_WITH_RC4_128_MD5", "SSL_DH_anon_EXPORT_WITH_DES40_SHA", "SSL_DH_anon_WITH_DES_SHA", "SSL_DH_anon_WITH_3DES_EDE_SHA", "SSL_FORTEZZA_KEA_WITH_NULL_SHA", "SSL_FORTEZZA_KEA_WITH_FORTEZZA_SHA",
			"SSL_FORTEZZA_KEA_WITH_RC4_128_SHA"
		};

		/// <summary>
		/// Map cipher id numbers to names. See http://www.iana.org/assignments/tls-parameters/
		/// Format is PROTOCOL_KEYAGREEMENT_AUTHENTICATIONMECHANISM_CIPHER_MACPRIMITIVE
		/// </summary>
		// Token: 0x04000139 RID: 313
		internal static readonly Dictionary<uint, string> dictTLSCipherSuites = new Dictionary<uint, string>
		{
			{ 0U, "TLS_NULL_WITH_NULL_NULL" },
			{ 1U, "TLS_RSA_WITH_NULL_MD5" },
			{ 2U, "TLS_RSA_WITH_NULL_SHA" },
			{ 3U, "TLS_RSA_EXPORT_WITH_RC4_40_MD5" },
			{ 4U, "TLS_RSA_WITH_RC4_128_MD5" },
			{ 5U, "TLS_RSA_WITH_RC4_128_SHA" },
			{ 6U, "TLS_RSA_EXPORT_WITH_RC2_CBC_40_MD5" },
			{ 7U, "TLS_RSA_WITH_IDEA_CBC_SHA" },
			{ 8U, "TLS_RSA_EXPORT_WITH_DES40_CBC_SHA" },
			{ 9U, "TLS_RSA_WITH_DES_CBC_SHA" },
			{ 10U, "TLS_RSA_WITH_3DES_EDE_CBC_SHA" },
			{ 11U, "TLS_DH_DSS_EXPORT_WITH_DES40_CBC_SHA" },
			{ 12U, "TLS_DH_DSS_WITH_DES_CBC_SHA" },
			{ 13U, "TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA" },
			{ 14U, "TLS_DH_RSA_EXPORT_WITH_DES40_CBC_SHA" },
			{ 15U, "TLS_DH_RSA_WITH_DES_CBC_SHA" },
			{ 16U, "TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA" },
			{ 17U, "TLS_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA" },
			{ 18U, "TLS_DHE_DSS_WITH_DES_CBC_SHA" },
			{ 19U, "TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA" },
			{ 20U, "TLS_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA" },
			{ 21U, "TLS_DHE_RSA_WITH_DES_CBC_SHA" },
			{ 22U, "TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA" },
			{ 23U, "TLS_DH_anon_EXPORT_WITH_RC4_40_MD5" },
			{ 24U, "TLS_DH_anon_WITH_RC4_128_MD5" },
			{ 25U, "TLS_DH_anon_EXPORT_WITH_DES40_CBC_SHA" },
			{ 26U, "TLS_DH_anon_WITH_DES_CBC_SHA" },
			{ 27U, "TLS_DH_anon_WITH_3DES_EDE_CBC_SHA" },
			{ 30U, "TLS_KRB5_WITH_DES_CBC_SHA" },
			{ 31U, "TLS_KRB5_WITH_3DES_EDE_CBC_SHA" },
			{ 32U, "TLS_KRB5_WITH_RC4_128_SHA" },
			{ 33U, "TLS_KRB5_WITH_IDEA_CBC_SHA" },
			{ 34U, "TLS_KRB5_WITH_DES_CBC_MD5" },
			{ 35U, "TLS_KRB5_WITH_3DES_EDE_CBC_MD5" },
			{ 36U, "TLS_KRB5_WITH_RC4_128_MD5" },
			{ 37U, "TLS_KRB5_WITH_IDEA_CBC_MD5" },
			{ 38U, "TLS_KRB5_EXPORT_WITH_DES_CBC_40_SHA" },
			{ 39U, "TLS_KRB5_EXPORT_WITH_RC2_CBC_40_SHA" },
			{ 40U, "TLS_KRB5_EXPORT_WITH_RC4_40_SHA" },
			{ 41U, "TLS_KRB5_EXPORT_WITH_DES_CBC_40_MD5" },
			{ 42U, "TLS_KRB5_EXPORT_WITH_RC2_CBC_40_MD5" },
			{ 43U, "TLS_KRB5_EXPORT_WITH_RC4_40_MD5" },
			{ 44U, "TLS_PSK_WITH_NULL_SHA" },
			{ 45U, "TLS_DHE_PSK_WITH_NULL_SHA" },
			{ 46U, "TLS_RSA_PSK_WITH_NULL_SHA" },
			{ 47U, "TLS_RSA_WITH_AES_128_CBC_SHA" },
			{ 48U, "TLS_DH_DSS_WITH_AES_128_CBC_SHA" },
			{ 49U, "TLS_DH_RSA_WITH_AES_128_CBC_SHA" },
			{ 50U, "TLS_DHE_DSS_WITH_AES_128_CBC_SHA" },
			{ 51U, "TLS_DHE_RSA_WITH_AES_128_CBC_SHA" },
			{ 52U, "TLS_DH_anon_WITH_AES_128_CBC_SHA" },
			{ 53U, "TLS_RSA_WITH_AES_256_CBC_SHA" },
			{ 54U, "TLS_DH_DSS_WITH_AES_256_CBC_SHA" },
			{ 55U, "TLS_DH_RSA_WITH_AES_256_CBC_SHA" },
			{ 56U, "TLS_DHE_DSS_WITH_AES_256_CBC_SHA" },
			{ 57U, "TLS_DHE_RSA_WITH_AES_256_CBC_SHA" },
			{ 58U, "TLS_DH_anon_WITH_AES_256_CBC_SHA" },
			{ 59U, "TLS_RSA_WITH_NULL_SHA256" },
			{ 60U, "TLS_RSA_WITH_AES_128_CBC_SHA256" },
			{ 61U, "TLS_RSA_WITH_AES_256_CBC_SHA256" },
			{ 62U, "TLS_DH_DSS_WITH_AES_128_CBC_SHA256" },
			{ 63U, "TLS_DH_RSA_WITH_AES_128_CBC_SHA256" },
			{ 64U, "TLS_DHE_DSS_WITH_AES_128_CBC_SHA256" },
			{ 65U, "TLS_RSA_WITH_CAMELLIA_128_CBC_SHA" },
			{ 66U, "TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA" },
			{ 67U, "TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA" },
			{ 68U, "TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA" },
			{ 69U, "TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA" },
			{ 70U, "TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA" },
			{ 103U, "TLS_DHE_RSA_WITH_AES_128_CBC_SHA256" },
			{ 104U, "TLS_DH_DSS_WITH_AES_256_CBC_SHA256" },
			{ 105U, "TLS_DH_RSA_WITH_AES_256_CBC_SHA256" },
			{ 106U, "TLS_DHE_DSS_WITH_AES_256_CBC_SHA256" },
			{ 107U, "TLS_DHE_RSA_WITH_AES_256_CBC_SHA256" },
			{ 108U, "TLS_DH_anon_WITH_AES_128_CBC_SHA256" },
			{ 109U, "TLS_DH_anon_WITH_AES_256_CBC_SHA256" },
			{ 132U, "TLS_RSA_WITH_CAMELLIA_256_CBC_SHA" },
			{ 133U, "TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA" },
			{ 134U, "TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA" },
			{ 135U, "TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA" },
			{ 136U, "TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA" },
			{ 137U, "TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA" },
			{ 138U, "TLS_PSK_WITH_RC4_128_SHA" },
			{ 139U, "TLS_PSK_WITH_3DES_EDE_CBC_SHA" },
			{ 140U, "TLS_PSK_WITH_AES_128_CBC_SHA" },
			{ 141U, "TLS_PSK_WITH_AES_256_CBC_SHA" },
			{ 142U, "TLS_DHE_PSK_WITH_RC4_128_SHA" },
			{ 143U, "TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA" },
			{ 144U, "TLS_DHE_PSK_WITH_AES_128_CBC_SHA" },
			{ 145U, "TLS_DHE_PSK_WITH_AES_256_CBC_SHA" },
			{ 146U, "TLS_RSA_PSK_WITH_RC4_128_SHA" },
			{ 147U, "TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA" },
			{ 148U, "TLS_RSA_PSK_WITH_AES_128_CBC_SHA" },
			{ 149U, "TLS_RSA_PSK_WITH_AES_256_CBC_SHA" },
			{ 150U, "TLS_RSA_WITH_SEED_CBC_SHA" },
			{ 151U, "TLS_DH_DSS_WITH_SEED_CBC_SHA" },
			{ 152U, "TLS_DH_RSA_WITH_SEED_CBC_SHA" },
			{ 153U, "TLS_DHE_DSS_WITH_SEED_CBC_SHA" },
			{ 154U, "TLS_DHE_RSA_WITH_SEED_CBC_SHA" },
			{ 155U, "TLS_DH_anon_WITH_SEED_CBC_SHA" },
			{ 156U, "TLS_RSA_WITH_AES_128_GCM_SHA256" },
			{ 157U, "TLS_RSA_WITH_AES_256_GCM_SHA384" },
			{ 158U, "TLS_DHE_RSA_WITH_AES_128_GCM_SHA256" },
			{ 159U, "TLS_DHE_RSA_WITH_AES_256_GCM_SHA384" },
			{ 160U, "TLS_DH_RSA_WITH_AES_128_GCM_SHA256" },
			{ 161U, "TLS_DH_RSA_WITH_AES_256_GCM_SHA384" },
			{ 162U, "TLS_DHE_DSS_WITH_AES_128_GCM_SHA256" },
			{ 163U, "TLS_DHE_DSS_WITH_AES_256_GCM_SHA384" },
			{ 164U, "TLS_DH_DSS_WITH_AES_128_GCM_SHA256" },
			{ 165U, "TLS_DH_DSS_WITH_AES_256_GCM_SHA384" },
			{ 166U, "TLS_DH_anon_WITH_AES_128_GCM_SHA256" },
			{ 167U, "TLS_DH_anon_WITH_AES_256_GCM_SHA384" },
			{ 168U, "TLS_PSK_WITH_AES_128_GCM_SHA256" },
			{ 169U, "TLS_PSK_WITH_AES_256_GCM_SHA384" },
			{ 170U, "TLS_DHE_PSK_WITH_AES_128_GCM_SHA256" },
			{ 171U, "TLS_DHE_PSK_WITH_AES_256_GCM_SHA384" },
			{ 172U, "TLS_RSA_PSK_WITH_AES_128_GCM_SHA256" },
			{ 173U, "TLS_RSA_PSK_WITH_AES_256_GCM_SHA384" },
			{ 174U, "TLS_PSK_WITH_AES_128_CBC_SHA256" },
			{ 175U, "TLS_PSK_WITH_AES_256_CBC_SHA384" },
			{ 176U, "TLS_PSK_WITH_NULL_SHA256" },
			{ 177U, "TLS_PSK_WITH_NULL_SHA384" },
			{ 178U, "TLS_DHE_PSK_WITH_AES_128_CBC_SHA256" },
			{ 179U, "TLS_DHE_PSK_WITH_AES_256_CBC_SHA384" },
			{ 180U, "TLS_DHE_PSK_WITH_NULL_SHA256" },
			{ 181U, "TLS_DHE_PSK_WITH_NULL_SHA384" },
			{ 182U, "TLS_RSA_PSK_WITH_AES_128_CBC_SHA256" },
			{ 183U, "TLS_RSA_PSK_WITH_AES_256_CBC_SHA384" },
			{ 184U, "TLS_RSA_PSK_WITH_NULL_SHA256" },
			{ 185U, "TLS_RSA_PSK_WITH_NULL_SHA384" },
			{ 186U, "TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 187U, "TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 188U, "TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 189U, "TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 190U, "TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 191U, "TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 192U, "TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256" },
			{ 193U, "TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256" },
			{ 194U, "TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256" },
			{ 195U, "TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256" },
			{ 196U, "TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256" },
			{ 197U, "TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA256" },
			{ 198U, "TLS_SM4_GCM_SM3" },
			{ 199U, "TLS_SM4_CCM_SM3" },
			{ 255U, "TLS_EMPTY_RENEGOTIATION_INFO_SCSV" },
			{ 4865U, "TLS_AES_128_GCM_SHA256" },
			{ 4866U, "TLS_AES_256_GCM_SHA384" },
			{ 4867U, "TLS_CHACHA20_POLY1305_SHA256" },
			{ 4868U, "TLS_AES_128_CCM_SHA256" },
			{ 4869U, "TLS_AES_128_CCM_8_SHA256" },
			{ 22016U, "TLS_FALLBACK_SCSV" },
			{ 49153U, "TLS_ECDH_ECDSA_WITH_NULL_SHA" },
			{ 49154U, "TLS_ECDH_ECDSA_WITH_RC4_128_SHA" },
			{ 49155U, "TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA" },
			{ 49156U, "TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA" },
			{ 49157U, "TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA" },
			{ 49158U, "TLS_ECDHE_ECDSA_WITH_NULL_SHA" },
			{ 49159U, "TLS_ECDHE_ECDSA_WITH_RC4_128_SHA" },
			{ 49160U, "TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA" },
			{ 49161U, "TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA" },
			{ 49162U, "TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA" },
			{ 49163U, "TLS_ECDH_RSA_WITH_NULL_SHA" },
			{ 49164U, "TLS_ECDH_RSA_WITH_RC4_128_SHA" },
			{ 49165U, "TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA" },
			{ 49166U, "TLS_ECDH_RSA_WITH_AES_128_CBC_SHA" },
			{ 49167U, "TLS_ECDH_RSA_WITH_AES_256_CBC_SHA" },
			{ 49168U, "TLS_ECDHE_RSA_WITH_NULL_SHA" },
			{ 49169U, "TLS_ECDHE_RSA_WITH_RC4_128_SHA" },
			{ 49170U, "TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA" },
			{ 49171U, "TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA" },
			{ 49172U, "TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA" },
			{ 49173U, "TLS_ECDH_anon_WITH_NULL_SHA" },
			{ 49174U, "TLS_ECDH_anon_WITH_RC4_128_SHA" },
			{ 49175U, "TLS_ECDH_anon_WITH_3DES_EDE_CBC_SHA" },
			{ 49176U, "TLS_ECDH_anon_WITH_AES_128_CBC_SHA" },
			{ 49177U, "TLS_ECDH_anon_WITH_AES_256_CBC_SHA" },
			{ 49178U, "TLS_SRP_SHA_WITH_3DES_EDE_CBC_SHA" },
			{ 49179U, "TLS_SRP_SHA_RSA_WITH_3DES_EDE_CBC_SHA" },
			{ 49180U, "TLS_SRP_SHA_DSS_WITH_3DES_EDE_CBC_SHA" },
			{ 49181U, "TLS_SRP_SHA_WITH_AES_128_CBC_SHA" },
			{ 49182U, "TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA" },
			{ 49183U, "TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA" },
			{ 49184U, "TLS_SRP_SHA_WITH_AES_256_CBC_SHA" },
			{ 49185U, "TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA" },
			{ 49186U, "TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA" },
			{ 49187U, "TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256" },
			{ 49188U, "TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384" },
			{ 49189U, "TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256" },
			{ 49190U, "TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384" },
			{ 49191U, "TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256" },
			{ 49192U, "TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384" },
			{ 49193U, "TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256" },
			{ 49194U, "TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384" },
			{ 49195U, "TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256" },
			{ 49196U, "TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384" },
			{ 49197U, "TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256" },
			{ 49198U, "TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384" },
			{ 49199U, "TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256" },
			{ 49200U, "TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384" },
			{ 49201U, "TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256" },
			{ 49202U, "TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384" },
			{ 49203U, "TLS_ECDHE_PSK_WITH_RC4_128_SHA" },
			{ 49204U, "TLS_ECDHE_PSK_WITH_3DES_EDE_CBC_SHA" },
			{ 49205U, "TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA" },
			{ 49206U, "TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA" },
			{ 49207U, "TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256" },
			{ 49208U, "TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384" },
			{ 49209U, "TLS_ECDHE_PSK_WITH_NULL_SHA" },
			{ 49210U, "TLS_ECDHE_PSK_WITH_NULL_SHA256" },
			{ 49211U, "TLS_ECDHE_PSK_WITH_NULL_SHA384" },
			{ 49212U, "TLS_RSA_WITH_ARIA_128_CBC_SHA256" },
			{ 49213U, "TLS_RSA_WITH_ARIA_256_CBC_SHA384" },
			{ 49214U, "TLS_DH_DSS_WITH_ARIA_128_CBC_SHA256" },
			{ 49215U, "TLS_DH_DSS_WITH_ARIA_256_CBC_SHA384" },
			{ 49216U, "TLS_DH_RSA_WITH_ARIA_128_CBC_SHA256" },
			{ 49217U, "TLS_DH_RSA_WITH_ARIA_256_CBC_SHA384" },
			{ 49218U, "TLS_DHE_DSS_WITH_ARIA_128_CBC_SHA256" },
			{ 49219U, "TLS_DHE_DSS_WITH_ARIA_256_CBC_SHA384" },
			{ 49220U, "TLS_DHE_RSA_WITH_ARIA_128_CBC_SHA256" },
			{ 49221U, "TLS_DHE_RSA_WITH_ARIA_256_CBC_SHA384" },
			{ 49222U, "TLS_DH_anon_WITH_ARIA_128_CBC_SHA256" },
			{ 49223U, "TLS_DH_anon_WITH_ARIA_256_CBC_SHA384" },
			{ 49224U, "TLS_ECDHE_ECDSA_WITH_ARIA_128_CBC_SHA256" },
			{ 49225U, "TLS_ECDHE_ECDSA_WITH_ARIA_256_CBC_SHA384" },
			{ 49226U, "TLS_ECDH_ECDSA_WITH_ARIA_128_CBC_SHA256" },
			{ 49227U, "TLS_ECDH_ECDSA_WITH_ARIA_256_CBC_SHA384" },
			{ 49228U, "TLS_ECDHE_RSA_WITH_ARIA_128_CBC_SHA256" },
			{ 49229U, "TLS_ECDHE_RSA_WITH_ARIA_256_CBC_SHA384" },
			{ 49230U, "TLS_ECDH_RSA_WITH_ARIA_128_CBC_SHA256" },
			{ 49231U, "TLS_ECDH_RSA_WITH_ARIA_256_CBC_SHA384" },
			{ 49232U, "TLS_RSA_WITH_ARIA_128_GCM_SHA256" },
			{ 49233U, "TLS_RSA_WITH_ARIA_256_GCM_SHA384" },
			{ 49234U, "TLS_DHE_RSA_WITH_ARIA_128_GCM_SHA256" },
			{ 49235U, "TLS_DHE_RSA_WITH_ARIA_256_GCM_SHA384" },
			{ 49236U, "TLS_DH_RSA_WITH_ARIA_128_GCM_SHA256" },
			{ 49237U, "TLS_DH_RSA_WITH_ARIA_256_GCM_SHA384" },
			{ 49238U, "TLS_DHE_DSS_WITH_ARIA_128_GCM_SHA256" },
			{ 49239U, "TLS_DHE_DSS_WITH_ARIA_256_GCM_SHA384" },
			{ 49240U, "TLS_DH_DSS_WITH_ARIA_128_GCM_SHA256" },
			{ 49241U, "TLS_DH_DSS_WITH_ARIA_256_GCM_SHA384" },
			{ 49242U, "TLS_DH_anon_WITH_ARIA_128_GCM_SHA256" },
			{ 49243U, "TLS_DH_anon_WITH_ARIA_256_GCM_SHA384" },
			{ 49244U, "TLS_ECDHE_ECDSA_WITH_ARIA_128_GCM_SHA256" },
			{ 49245U, "TLS_ECDHE_ECDSA_WITH_ARIA_256_GCM_SHA384" },
			{ 49246U, "TLS_ECDH_ECDSA_WITH_ARIA_128_GCM_SHA256" },
			{ 49247U, "TLS_ECDH_ECDSA_WITH_ARIA_256_GCM_SHA384" },
			{ 49248U, "TLS_ECDHE_RSA_WITH_ARIA_128_GCM_SHA256" },
			{ 49249U, "TLS_ECDHE_RSA_WITH_ARIA_256_GCM_SHA384" },
			{ 49250U, "TLS_ECDH_RSA_WITH_ARIA_128_GCM_SHA256" },
			{ 49251U, "TLS_ECDH_RSA_WITH_ARIA_256_GCM_SHA384" },
			{ 49252U, "TLS_PSK_WITH_ARIA_128_CBC_SHA256" },
			{ 49253U, "TLS_PSK_WITH_ARIA_256_CBC_SHA384" },
			{ 49254U, "TLS_DHE_PSK_WITH_ARIA_128_CBC_SHA256" },
			{ 49255U, "TLS_DHE_PSK_WITH_ARIA_256_CBC_SHA384" },
			{ 49256U, "TLS_RSA_PSK_WITH_ARIA_128_CBC_SHA256" },
			{ 49257U, "TLS_RSA_PSK_WITH_ARIA_256_CBC_SHA384" },
			{ 49258U, "TLS_PSK_WITH_ARIA_128_GCM_SHA256" },
			{ 49259U, "TLS_PSK_WITH_ARIA_256_GCM_SHA384" },
			{ 49260U, "TLS_DHE_PSK_WITH_ARIA_128_GCM_SHA256" },
			{ 49261U, "TLS_DHE_PSK_WITH_ARIA_256_GCM_SHA384" },
			{ 49262U, "TLS_RSA_PSK_WITH_ARIA_128_GCM_SHA256" },
			{ 49263U, "TLS_RSA_PSK_WITH_ARIA_256_GCM_SHA384" },
			{ 49264U, "TLS_ECDHE_PSK_WITH_ARIA_128_CBC_SHA256" },
			{ 49265U, "TLS_ECDHE_PSK_WITH_ARIA_256_CBC_SHA384" },
			{ 49266U, "TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 49267U, "TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384" },
			{ 49268U, "TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 49269U, "TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384" },
			{ 49270U, "TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 49271U, "TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384" },
			{ 49272U, "TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 49273U, "TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384" },
			{ 49274U, "TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256" },
			{ 49275U, "TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384" },
			{ 49276U, "TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256" },
			{ 49277U, "TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384" },
			{ 49278U, "TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256" },
			{ 49279U, "TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384" },
			{ 49280U, "TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256" },
			{ 49281U, "TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384" },
			{ 49282U, "TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256" },
			{ 49283U, "TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384" },
			{ 49284U, "TLS_DH_anon_WITH_CAMELLIA_128_GCM_SHA256" },
			{ 49285U, "TLS_DH_anon_WITH_CAMELLIA_256_GCM_SHA384" },
			{ 49286U, "TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256" },
			{ 49287U, "TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384" },
			{ 49288U, "TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256" },
			{ 49289U, "TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384" },
			{ 49290U, "TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256" },
			{ 49291U, "TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384" },
			{ 49292U, "TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256" },
			{ 49293U, "TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384" },
			{ 49294U, "TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256" },
			{ 49295U, "TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384" },
			{ 49296U, "TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256" },
			{ 49297U, "TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384" },
			{ 49298U, "TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256" },
			{ 49299U, "TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384" },
			{ 49300U, "TLS_PSK_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 49301U, "TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384" },
			{ 49302U, "TLS_DHE_PSK_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 49303U, "TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384" },
			{ 49304U, "TLS_RSA_PSK_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 49305U, "TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384" },
			{ 49306U, "TLS_ECDHE_PSK_WITH_CAMELLIA_128_CBC_SHA256" },
			{ 49307U, "TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384" },
			{ 49308U, "TLS_RSA_WITH_AES_128_CCM" },
			{ 49309U, "TLS_RSA_WITH_AES_256_CCM" },
			{ 49310U, "TLS_DHE_RSA_WITH_AES_128_CCM" },
			{ 49311U, "TLS_DHE_RSA_WITH_AES_256_CCM" },
			{ 49312U, "TLS_RSA_WITH_AES_128_CCM_8" },
			{ 49313U, "TLS_RSA_WITH_AES_256_CCM_8" },
			{ 49314U, "TLS_DHE_RSA_WITH_AES_128_CCM_8" },
			{ 49315U, "TLS_DHE_RSA_WITH_AES_256_CCM_8" },
			{ 49316U, "TLS_PSK_WITH_AES_128_CCM" },
			{ 49317U, "TLS_PSK_WITH_AES_256_CCM" },
			{ 49318U, "TLS_DHE_PSK_WITH_AES_128_CCM" },
			{ 49319U, "TLS_DHE_PSK_WITH_AES_256_CCM" },
			{ 49320U, "TLS_PSK_WITH_AES_128_CCM_8" },
			{ 49321U, "TLS_PSK_WITH_AES_256_CCM_8" },
			{ 49322U, "TLS_PSK_DHE_WITH_AES_128_CCM_8" },
			{ 49323U, "TLS_PSK_DHE_WITH_AES_256_CCM_8" },
			{ 49324U, "TLS_ECDHE_ECDSA_WITH_AES_128_CCM" },
			{ 49325U, "TLS_ECDHE_ECDSA_WITH_AES_256_CCM" },
			{ 49326U, "TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8" },
			{ 49327U, "TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8" },
			{ 49328U, "TLS_ECCPWD_WITH_AES_128_GCM_SHA256" },
			{ 49329U, "TLS_ECCPWD_WITH_AES_256_GCM_SHA384" },
			{ 49330U, "TLS_ECCPWD_WITH_AES_128_CCM_SHA256" },
			{ 49331U, "TLS_ECCPWD_WITH_AES_256_CCM_SHA384" },
			{ 49332U, "TLS_SHA256_SHA256" },
			{ 49333U, "TLS_SHA384_SHA384" },
			{ 49408U, "TLS_GOSTR341112_256_WITH_KUZNYECHIK_CTR_OMAC" },
			{ 49409U, "TLS_GOSTR341112_256_WITH_MAGMA_CTR_OMAC" },
			{ 49410U, "TLS_GOSTR341112_256_WITH_28147_CNT_IMIT" },
			{ 52392U, "TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256" },
			{ 52393U, "TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256" },
			{ 52394U, "TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256" },
			{ 52395U, "TLS_PSK_WITH_CHACHA20_POLY1305_SHA256" },
			{ 52396U, "TLS_ECDHE_PSK_WITH_CHACHA20_POLY1305_SHA256" },
			{ 52397U, "TLS_DHE_PSK_WITH_CHACHA20_POLY1305_SHA256" },
			{ 52398U, "TLS_RSA_PSK_WITH_CHACHA20_POLY1305_SHA256" },
			{ 53249U, "TLS_ECDHE_PSK_WITH_AES_128_GCM_SHA256" },
			{ 53250U, "TLS_ECDHE_PSK_WITH_AES_256_GCM_SHA384" },
			{ 53251U, "TLS_ECDHE_PSK_WITH_AES_128_CCM_8_SHA256" },
			{ 53253U, "TLS_ECDHE_PSK_WITH_AES_128_CCM_SHA256" }
		};
	}
}
