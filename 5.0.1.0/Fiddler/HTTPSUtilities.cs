using System;
using System.Collections.Generic;
using System.Text;

namespace Fiddler
{
	/// <summary>
	/// Utility functions common to parsing both ClientHello and ServerHello messages
	/// </summary>
	// Token: 0x02000044 RID: 68
	internal class HTTPSUtilities
	{
		/// <summary>
		/// Gets a textual string from a TLS extension
		/// </summary>
		// Token: 0x060002C0 RID: 704 RVA: 0x00018A68 File Offset: 0x00016C68
		internal static string GetExtensionString(byte[] arrData)
		{
			List<string> sItems = new List<string>();
			int iProtStrLen;
			for (int iPtr = 0; iPtr < arrData.Length; iPtr += 1 + iProtStrLen)
			{
				iProtStrLen = (int)arrData[iPtr];
				sItems.Add(Encoding.ASCII.GetString(arrData, iPtr + 1, iProtStrLen));
			}
			return string.Join(", ", sItems.ToArray());
		}

		/// <summary>
		/// Builds a string from an ALPN List of strings
		/// </summary>
		// Token: 0x060002C1 RID: 705 RVA: 0x00018AB4 File Offset: 0x00016CB4
		internal static string GetProtocolListAsString(byte[] arrData)
		{
			int iSize = ((int)arrData[0] << 8) + (int)arrData[1];
			byte[] arrList = new byte[iSize];
			Buffer.BlockCopy(arrData, 2, arrList, 0, arrList.Length);
			return HTTPSUtilities.GetExtensionString(arrList);
		}

		/// <summary>
		/// List Sig/Hash pairs from  RFC5246 and TLS/1.3 spec
		/// </summary>
		/// <param name="arrData"></param>
		/// <returns></returns>
		// Token: 0x060002C2 RID: 706 RVA: 0x00018AE4 File Offset: 0x00016CE4
		internal static string GetSignatureAndHashAlgsAsString(byte[] arrData)
		{
			int iSize = ((int)arrData[0] << 8) + (int)arrData[1];
			StringBuilder sbPairs = new StringBuilder();
			int ix = 2;
			while (ix < iSize + 2)
			{
				int num = ((int)arrData[ix] << 8) + (int)arrData[ix + 1];
				if (num <= 1027)
				{
					if (num <= 515)
					{
						if (num != 513)
						{
							if (num != 515)
							{
								goto IL_1DA;
							}
							sbPairs.Append("ecdsa_sha1");
						}
						else
						{
							sbPairs.Append("rsa_pkcs1_sha1");
						}
					}
					else if (num != 1025)
					{
						if (num != 1027)
						{
							goto IL_1DA;
						}
						sbPairs.Append("ecdsa_secp256r1_sha256");
					}
					else
					{
						sbPairs.Append("rsa_pkcs1_sha256");
					}
				}
				else if (num <= 1283)
				{
					if (num != 1281)
					{
						if (num != 1283)
						{
							goto IL_1DA;
						}
						sbPairs.Append("ecdsa_secp384r1_sha384");
					}
					else
					{
						sbPairs.Append("rsa_pkcs1_sha384");
					}
				}
				else if (num != 1537)
				{
					if (num != 1539)
					{
						switch (num)
						{
						case 2052:
							sbPairs.Append("rsa_pss_rsae_sha256");
							break;
						case 2053:
							sbPairs.Append("rsa_pss_rsae_sha384");
							break;
						case 2054:
							sbPairs.Append("rsa_pss_rsae_sha512");
							break;
						case 2055:
							sbPairs.Append("ed25519");
							break;
						case 2056:
							sbPairs.Append("ed448");
							break;
						case 2057:
							sbPairs.Append("rsa_pss_pss_sha256");
							break;
						case 2058:
							sbPairs.Append("rsa_pss_pss_sha384");
							break;
						case 2059:
							sbPairs.Append("rsa_pss_pss_sha512");
							break;
						default:
							goto IL_1DA;
						}
					}
					else
					{
						sbPairs.Append("ecdsa_secp521r1_sha512");
					}
				}
				else
				{
					sbPairs.Append("rsa_pkcs1_sha512");
				}
				IL_2F9:
				sbPairs.AppendFormat(", ", Array.Empty<object>());
				ix += 2;
				continue;
				IL_1DA:
				switch (arrData[ix + 1])
				{
				case 0:
					sbPairs.Append("NoSig");
					break;
				case 1:
					sbPairs.Append("rsa");
					break;
				case 2:
					sbPairs.Append("dsa");
					break;
				case 3:
					sbPairs.Append("ecdsa");
					break;
				default:
					sbPairs.AppendFormat("Unknown[0x{0:x}]", arrData[ix + 1]);
					break;
				}
				sbPairs.AppendFormat("_", Array.Empty<object>());
				switch (arrData[ix])
				{
				case 0:
					sbPairs.Append("NoHash");
					goto IL_2F9;
				case 1:
					sbPairs.Append("md4");
					goto IL_2F9;
				case 2:
					sbPairs.Append("sha1");
					goto IL_2F9;
				case 3:
					sbPairs.Append("sha224");
					goto IL_2F9;
				case 4:
					sbPairs.Append("sha256");
					goto IL_2F9;
				case 5:
					sbPairs.Append("sha384");
					goto IL_2F9;
				case 6:
					sbPairs.Append("sha512");
					goto IL_2F9;
				default:
					sbPairs.AppendFormat("Unknown[0x{0:x}]", arrData[ix]);
					goto IL_2F9;
				}
			}
			if (sbPairs.Length > 1)
			{
				sbPairs.Length -= 2;
			}
			return sbPairs.ToString();
		}

		/// <summary>
		/// Describes a block of padding, with a friendly summary if all bytes are 0s
		/// https://www.ietf.org/archive/id/draft-agl-tls-padding-03.txt
		/// </summary>
		// Token: 0x060002C3 RID: 707 RVA: 0x00018E28 File Offset: 0x00017028
		internal static string DescribePadding(byte[] arrPadding)
		{
			for (int ix = 0; ix < arrPadding.Length; ix++)
			{
				if (arrPadding[ix] != 0)
				{
					return Utilities.ByteArrayToString(arrPadding);
				}
			}
			return arrPadding.Length.ToString("N0") + " null bytes";
		}

		/// <summary>
		/// List defined Supported Groups &amp; ECC Curves from RFC4492
		/// </summary>
		/// <returns></returns>
		// Token: 0x060002C4 RID: 708 RVA: 0x00018E6C File Offset: 0x0001706C
		internal static string GetSupportedGroupsAsString(byte[] arrGroupData)
		{
			List<string> listECCs = new List<string>();
			if (arrGroupData.Length < 2)
			{
				return string.Empty;
			}
			int iSize = ((int)arrGroupData[0] << 8) + (int)arrGroupData[1];
			int iX = 2;
			while (iX < arrGroupData.Length - 1)
			{
				ushort uShort = (ushort)(((int)arrGroupData[iX] << 8) | (int)arrGroupData[iX + 1]);
				if (uShort <= 31354)
				{
					if (uShort <= 10794)
					{
						if (uShort <= 260)
						{
							switch (uShort)
							{
							case 1:
								listECCs.Add("sect163k1 [0x1]");
								break;
							case 2:
								listECCs.Add("sect163r1 [0x2]");
								break;
							case 3:
								listECCs.Add("sect163r2 [0x3]");
								break;
							case 4:
								listECCs.Add("sect193r1 [0x4]");
								break;
							case 5:
								listECCs.Add("sect193r2 [0x5]");
								break;
							case 6:
								listECCs.Add("sect233k1 [0x6]");
								break;
							case 7:
								listECCs.Add("sect233r1 [0x7]");
								break;
							case 8:
								listECCs.Add("sect239k1 [0x8]");
								break;
							case 9:
								listECCs.Add("sect283k1 [0x9]");
								break;
							case 10:
								listECCs.Add("sect283r1 [0xa]");
								break;
							case 11:
								listECCs.Add("sect409k1 [0xb]");
								break;
							case 12:
								listECCs.Add("sect409r1 [0xc]");
								break;
							case 13:
								listECCs.Add("sect571k1 [0xd]");
								break;
							case 14:
								listECCs.Add("sect571r1 [0xe]");
								break;
							case 15:
								listECCs.Add("secp160k1 [0xf]");
								break;
							case 16:
								listECCs.Add("secp160r1 [0x10]");
								break;
							case 17:
								listECCs.Add("secp160r2 [0x11]");
								break;
							case 18:
								listECCs.Add("secp192k1 [0x12]");
								break;
							case 19:
								listECCs.Add("secp192r1 [0x13]");
								break;
							case 20:
								listECCs.Add("secp224k1 [0x14]");
								break;
							case 21:
								listECCs.Add("secp224r1 [0x15]");
								break;
							case 22:
								listECCs.Add("secp256k1 [0x16]");
								break;
							case 23:
								listECCs.Add("secp256r1 [0x17]");
								break;
							case 24:
								listECCs.Add("secp384r1 [0x18]");
								break;
							case 25:
								listECCs.Add("secp521r1 [0x19]");
								break;
							case 26:
							case 27:
							case 28:
								goto IL_430;
							case 29:
								listECCs.Add("x25519 [0x1d]");
								break;
							case 30:
								listECCs.Add("x448 [0x1e]");
								break;
							default:
								switch (uShort)
								{
								case 256:
									listECCs.Add("ffdhe2048 [0x0100]");
									break;
								case 257:
									listECCs.Add("ffdhe3072 [0x0101]");
									break;
								case 258:
									listECCs.Add("ffdhe4096 [0x0102]");
									break;
								case 259:
									listECCs.Add("ffdhe6144 [0x0103]");
									break;
								case 260:
									listECCs.Add("ffdhe8192 [0x0104]");
									break;
								default:
									goto IL_430;
								}
								break;
							}
						}
						else
						{
							if (uShort != 2570 && uShort != 6682 && uShort != 10794)
							{
								goto IL_430;
							}
							goto IL_3F3;
						}
					}
					else if (uShort <= 19018)
					{
						if (uShort != 14906 && uShort != 19018)
						{
							goto IL_430;
						}
						goto IL_3F3;
					}
					else
					{
						if (uShort != 23130 && uShort != 27242 && uShort != 31354)
						{
							goto IL_430;
						}
						goto IL_3F3;
					}
				}
				else if (uShort <= 51914)
				{
					if (uShort <= 39578)
					{
						if (uShort != 35466 && uShort != 39578)
						{
							goto IL_430;
						}
						goto IL_3F3;
					}
					else
					{
						if (uShort != 43690 && uShort != 47802 && uShort != 51914)
						{
							goto IL_430;
						}
						goto IL_3F3;
					}
				}
				else if (uShort <= 60138)
				{
					if (uShort != 56026 && uShort != 60138)
					{
						goto IL_430;
					}
					goto IL_3F3;
				}
				else
				{
					if (uShort == 64250)
					{
						goto IL_3F3;
					}
					if (uShort != 65281)
					{
						if (uShort != 65282)
						{
							goto IL_430;
						}
						listECCs.Add("arbitrary_explicit_char2_curves [0xff02]");
					}
					else
					{
						listECCs.Add("arbitrary_explicit_prime_curves [0xff01]");
					}
				}
				IL_446:
				iX += 2;
				continue;
				IL_3F3:
				listECCs.Add("grease [0x" + uShort.ToString("x") + "]");
				goto IL_446;
				IL_430:
				listECCs.Add(string.Format("unknown [0x{0:x}]", uShort));
				goto IL_446;
			}
			return string.Join(", ", listECCs.ToArray());
		}

		/// <summary>
		/// List defined ECC Point Formats from RFC4492
		/// </summary>
		/// <param name="eccPoints"></param>
		/// <returns></returns>
		// Token: 0x060002C5 RID: 709 RVA: 0x000192E0 File Offset: 0x000174E0
		internal static string GetECCPointFormatsAsString(byte[] eccPoints)
		{
			List<string> listFormats = new List<string>();
			if (eccPoints.Length < 1)
			{
				return string.Empty;
			}
			for (int iX = 1; iX < eccPoints.Length; iX++)
			{
				switch (eccPoints[iX])
				{
				case 0:
					listFormats.Add("uncompressed [0x0]");
					break;
				case 1:
					listFormats.Add("ansiX962_compressed_prime [0x1]");
					break;
				case 2:
					listFormats.Add("ansiX962_compressed_char2 [0x2]");
					break;
				default:
					listFormats.Add(string.Format("unknown [0x{0:X}]", eccPoints[iX]));
					break;
				}
			}
			return string.Join(", ", listFormats.ToArray());
		}

		// Token: 0x060002C6 RID: 710 RVA: 0x00019374 File Offset: 0x00017574
		internal static string GetSupportedVersions(byte[] arrSupported)
		{
			List<string> listVersions = new List<string>();
			if (arrSupported.Length < 2)
			{
				return string.Empty;
			}
			for (int iX = 1; iX < arrSupported.Length - 2; iX += 2)
			{
				ushort uShort = (ushort)(((int)arrSupported[iX] << 8) | (int)arrSupported[iX + 1]);
				switch (uShort)
				{
				case 768:
					listVersions.Add("Ssl3.0");
					break;
				case 769:
					listVersions.Add("Tls1.0");
					break;
				case 770:
					listVersions.Add("Tls1.1");
					break;
				case 771:
					listVersions.Add("Tls1.2");
					break;
				case 772:
					listVersions.Add("Tls1.3");
					break;
				default:
				{
					string sDescription = "unknown";
					if ((uShort & 2570) == 2570 && uShort >> 8 == (int)(uShort & 255))
					{
						sDescription = "grease";
					}
					else if ((uShort & 32512) == 32512)
					{
						sDescription = "Tls1.3_draft" + ((int)(uShort & 255)).ToString();
					}
					listVersions.Add(string.Format("{0} [0x{1:x}]", sDescription, uShort));
					break;
				}
				}
			}
			return string.Join(", ", listVersions.ToArray());
		}

		/// <summary>
		/// Converts a HTTPS version to a "Major.Minor (Friendly)" string
		/// </summary>
		// Token: 0x060002C7 RID: 711 RVA: 0x0001949C File Offset: 0x0001769C
		internal static string HTTPSVersionToString(int iMajor, int iMinor)
		{
			string sFriendly = "Unknown";
			if (iMajor == 127)
			{
				sFriendly = "TLS/1.3, Draft " + iMinor.ToString();
			}
			else if (iMajor == 3 && iMinor == 4)
			{
				sFriendly = "TLS/1.3";
			}
			else if (iMajor == 3 && iMinor == 3)
			{
				sFriendly = "TLS/1.2";
			}
			else if (iMajor == 3 && iMinor == 2)
			{
				sFriendly = "TLS/1.1";
			}
			else if (iMajor == 3 && iMinor == 1)
			{
				sFriendly = "TLS/1.0";
			}
			else if (iMajor == 3 && iMinor == 0)
			{
				sFriendly = "SSL/3.0";
			}
			else if (iMajor == 2 && iMinor == 0)
			{
				sFriendly = "SSL/2.0";
			}
			return string.Format("{0}.{1} ({2})", iMajor, iMinor, sFriendly);
		}
	}
}
