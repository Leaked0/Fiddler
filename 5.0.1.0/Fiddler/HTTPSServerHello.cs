using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FiddlerCore.Utilities;

namespace Fiddler
{
	// Token: 0x02000046 RID: 70
	internal class HTTPSServerHello
	{
		/// <summary>
		/// Did client use ALPN to go to SPDY?
		/// http://tools.ietf.org/html/draft-ietf-tls-applayerprotoneg-01#section-3.1
		/// </summary>
		// Token: 0x17000082 RID: 130
		// (get) Token: 0x060002D4 RID: 724 RVA: 0x0001B967 File Offset: 0x00019B67
		// (set) Token: 0x060002D5 RID: 725 RVA: 0x0001B96F File Offset: 0x00019B6F
		public bool bALPNToSPDY { get; set; }

		/// <summary>
		///  Did this ServerHello Handshake specify an upgrade to SPDY?
		/// </summary>
		// Token: 0x17000083 RID: 131
		// (get) Token: 0x060002D6 RID: 726 RVA: 0x0001B978 File Offset: 0x00019B78
		// (set) Token: 0x060002D7 RID: 727 RVA: 0x0001B980 File Offset: 0x00019B80
		public bool bNPNToSPDY { get; set; }

		/// <summary>
		///  Did this ServerHello Handshake specify an upgrade to SPDY?
		/// </summary>
		// Token: 0x17000084 RID: 132
		// (get) Token: 0x060002D8 RID: 728 RVA: 0x0001B989 File Offset: 0x00019B89
		// (set) Token: 0x060002D9 RID: 729 RVA: 0x0001B991 File Offset: 0x00019B91
		public bool bALPNToHTTP2 { get; set; }

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x060002DA RID: 730 RVA: 0x0001B99A File Offset: 0x00019B9A
		private bool isTLS1Dot3OrLater
		{
			get
			{
				return this._MajorVersion > 3 || (this._MajorVersion == 3 && this._MinorVersion > 3);
			}
		}

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x060002DB RID: 731 RVA: 0x0001B9BC File Offset: 0x00019BBC
		private string CompressionSuite
		{
			get
			{
				if (this._iCompression < HTTPSClientHello.HTTPSCompressionSuites.Length)
				{
					return HTTPSClientHello.HTTPSCompressionSuites[this._iCompression];
				}
				return string.Format("Unrecognized compression format [0x{0:X2}]", this._iCompression);
			}
		}

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x060002DC RID: 732 RVA: 0x0001B9F0 File Offset: 0x00019BF0
		internal string CipherSuite
		{
			get
			{
				if ((ulong)this._iCipherSuite < (ulong)((long)HTTPSClientHello.SSL3CipherSuites.Length))
				{
					return HTTPSClientHello.SSL3CipherSuites[(int)this._iCipherSuite];
				}
				string sSuite;
				if (HTTPSClientHello.dictTLSCipherSuites.TryGetValue(this._iCipherSuite, out sSuite))
				{
					return sSuite;
				}
				return string.Format("Unrecognized cipher [0x{0:X4}] - See http://www.iana.org/assignments/tls-parameters/", this._iCipherSuite);
			}
		}

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x060002DD RID: 733 RVA: 0x0001BA46 File Offset: 0x00019C46
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

		// Token: 0x060002DE RID: 734 RVA: 0x0001BA64 File Offset: 0x00019C64
		public override string ToString()
		{
			StringBuilder sbOutput = new StringBuilder(512);
			if (this._HandshakeVersion == 2)
			{
				sbOutput.Append("A SSLv2-compatible ServerHello handshake was found. In v2, the ~client~ selects the active cipher after the ServerHello, when sending the Client-Master-Key message. Fiddler only parses the handshake.\n\n");
			}
			else
			{
				sbOutput.Append("A SSLv3-compatible ServerHello handshake was found. Fiddler extracted the parameters below.\n\n");
			}
			sbOutput.AppendFormat("Version: {0}\n", HTTPSUtilities.HTTPSVersionToString(this._MajorVersion, this._MinorVersion));
			if (!this.isTLS1Dot3OrLater)
			{
				sbOutput.AppendFormat("SessionID:\t{0}\n", Utilities.ByteArrayToString(this._SessionID));
			}
			if (this._HandshakeVersion == 3)
			{
				sbOutput.AppendFormat("Random:\t\t{0}\n", Utilities.ByteArrayToString(this._Random));
				sbOutput.AppendFormat("Cipher:\t\t{0} [0x{1:X4}]\n", this.CipherSuite, this._iCipherSuite);
			}
			if (!this.isTLS1Dot3OrLater)
			{
				sbOutput.AppendFormat("CompressionSuite:\t{0} [0x{1:X2}]\n", this.CompressionSuite, this._iCompression);
			}
			sbOutput.AppendFormat("Extensions:\n\t{0}\n", HTTPSServerHello.ExtensionListToString(this._Extensions));
			return sbOutput.ToString();
		}

		// Token: 0x060002DF RID: 735 RVA: 0x0001BB5A File Offset: 0x00019D5A
		private static string ExtensionListToString(List<string> slExts)
		{
			if (slExts == null || slExts.Count < 1)
			{
				return "\tnone";
			}
			return string.Join("\n\t", slExts.ToArray());
		}

		/// <summary>
		/// Parse a single extension using the list from http://tools.ietf.org/html/rfc6066
		/// </summary>
		/// <param name="iExtType"></param>
		/// <param name="arrData"></param>
		// Token: 0x060002E0 RID: 736 RVA: 0x0001BB80 File Offset: 0x00019D80
		private void ParseServerHelloExtension(int iExtType, byte[] arrData)
		{
			if (this._Extensions == null)
			{
				this._Extensions = new List<string>();
			}
			if (iExtType <= 13172)
			{
				switch (iExtType)
				{
				case 0:
					this._Extensions.Add(string.Format("\tserver_name\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
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
					this._Extensions.Add(string.Format("\tstatus_request (OCSP-stapling)\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 6:
					this._Extensions.Add(string.Format("\tuser_mapping\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 7:
				case 8:
				case 21:
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
				case 45:
				case 49:
					break;
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
					this._Extensions.Add(string.Format("\tILLEGAL EXTENSION signature_algorithms\t{0}", HTTPSUtilities.GetSignatureAndHashAlgsAsString(arrData)));
					return;
				case 14:
					this._Extensions.Add(string.Format("\tuse_srtp\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 15:
					this._Extensions.Add(string.Format("\theartbeat_rfc_6520\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 16:
				{
					string sProtocolList = HTTPSUtilities.GetProtocolListAsString(arrData);
					this._Extensions.Add(string.Format("\tALPN\t\t{0}", sProtocolList));
					if (sProtocolList.Contains("spdy/"))
					{
						this.bALPNToSPDY = true;
					}
					if (sProtocolList.Contains("h2"))
					{
						this.bALPNToHTTP2 = true;
						return;
					}
					return;
				}
				case 17:
					this._Extensions.Add(string.Format("\tstatus_request_v2 (RFC6961 OCSP-stapling v2)\t{0}", Utilities.ByteArrayToString(arrData)));
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
				case 46:
					this._Extensions.Add(string.Format("\tticket_early_data_info\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 47:
					this._Extensions.Add(string.Format("\tcertificate_authorities\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 48:
					this._Extensions.Add(string.Format("\toid_filters\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				case 50:
					this._Extensions.Add(string.Format("\tsignature_algorithms_cert\t{0}", HTTPSUtilities.GetSignatureAndHashAlgsAsString(arrData)));
					return;
				case 51:
					this._Extensions.Add(string.Format("\tkey_share\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				default:
					if (iExtType == 13172)
					{
						string sNPN = HTTPSUtilities.GetExtensionString(arrData);
						this._Extensions.Add(string.Format("\tNextProtocolNego\t{0}", sNPN));
						if (sNPN.Contains("spdy/"))
						{
							this.bNPNToSPDY = true;
							return;
						}
						return;
					}
					break;
				}
			}
			else
			{
				if (iExtType == 21760)
				{
					this._Extensions.Add(string.Format("\ttoken_binding(MSDraft)\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				}
				if (iExtType - 30031 <= 1)
				{
					this._Extensions.Add(string.Format("\tchannel_id(GoogleDraft)\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				}
				if (iExtType == 65281)
				{
					this._Extensions.Add(string.Format("\trenegotiation_info\t{0}", Utilities.ByteArrayToString(arrData)));
					return;
				}
			}
			this._Extensions.Add(string.Format("\t0x{0:x4}\t\t{1}", iExtType, Utilities.ByteArrayToString(arrData)));
		}

		// Token: 0x060002E1 RID: 737 RVA: 0x0001C148 File Offset: 0x0001A348
		private void ParseServerHelloExtensions(byte[] arrExtensionsData)
		{
			int iPtr = 0;
			try
			{
				while (iPtr < arrExtensionsData.Length)
				{
					int iExtensionType = ((int)arrExtensionsData[iPtr] << 8) + (int)arrExtensionsData[iPtr + 1];
					int iExtDataLen = ((int)arrExtensionsData[iPtr + 2] << 8) + (int)arrExtensionsData[iPtr + 3];
					byte[] arrExtData = new byte[iExtDataLen];
					Buffer.BlockCopy(arrExtensionsData, iPtr + 4, arrExtData, 0, arrExtData.Length);
					try
					{
						this.ParseServerHelloExtension(iExtensionType, arrExtData);
					}
					catch (Exception eX)
					{
						FiddlerApplication.Log.LogFormat("Error parsing server TLS extension. {0}", new object[] { Utilities.DescribeException(eX) });
					}
					iPtr += 4 + iExtDataLen;
				}
			}
			catch (Exception eX2)
			{
				FiddlerApplication.Log.LogFormat("Error parsing server TLS extensions. {0}", new object[] { Utilities.DescribeException(eX2) });
			}
		}

		// Token: 0x060002E2 RID: 738 RVA: 0x0001C204 File Offset: 0x0001A404
		internal bool LoadFromStream(Stream oNS)
		{
			int iProt = oNS.ReadByte();
			if (iProt == 22)
			{
				this._HandshakeVersion = 3;
				this._MajorVersion = oNS.ReadByte();
				this._MinorVersion = oNS.ReadByte();
				int iRecordLen = oNS.ReadByte() << 8;
				iRecordLen += oNS.ReadByte();
				int iMsgType = oNS.ReadByte();
				byte[] data = new byte[3];
				int cBytes = oNS.Read(data, 0, data.Length);
				this._MessageLen = ((int)data[0] << 16) + ((int)data[1] << 8) + (int)data[2];
				this._MajorVersion = oNS.ReadByte();
				this._MinorVersion = oNS.ReadByte();
				this._Random = new byte[32];
				cBytes = oNS.Read(this._Random, 0, 32);
				if (!this.isTLS1Dot3OrLater)
				{
					int iSessionIDLen = oNS.ReadByte();
					this._SessionID = new byte[iSessionIDLen];
					cBytes = oNS.Read(this._SessionID, 0, this._SessionID.Length);
				}
				this._iCipherSuite = (uint)((oNS.ReadByte() << 8) + oNS.ReadByte());
				if (!this.isTLS1Dot3OrLater)
				{
					this._iCompression = oNS.ReadByte();
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
					this.ParseServerHelloExtensions(data);
				}
				return true;
			}
			else
			{
				if (iProt == 21)
				{
					byte[] arrBytes = new byte[7];
					oNS.Read(arrBytes, 0, 7);
					FiddlerApplication.Log.LogFormat("Got a TLS alert from the server!\n{0}", new object[] { Utilities.ByteArrayToHexView(arrBytes, 8) });
					return false;
				}
				this._HandshakeVersion = 2;
				int oJunk = oNS.ReadByte();
				if (128 != (iProt & 128))
				{
					oJunk = oNS.ReadByte();
				}
				iProt = oNS.ReadByte();
				if (iProt != 4)
				{
					return false;
				}
				this._SessionID = new byte[1];
				oNS.Read(this._SessionID, 0, 1);
				oNS.ReadByte();
				this._MinorVersion = oNS.ReadByte();
				this._MajorVersion = oNS.ReadByte();
				return true;
			}
		}

		// Token: 0x060002E3 RID: 739 RVA: 0x0001C436 File Offset: 0x0001A636
		internal string ProtocolVersion()
		{
			return HTTPSUtilities.HTTPSVersionToString(this._MajorVersion, this._MinorVersion);
		}

		// Token: 0x0400013A RID: 314
		private int _HandshakeVersion;

		// Token: 0x0400013B RID: 315
		private int _MessageLen;

		// Token: 0x0400013C RID: 316
		private int _MajorVersion;

		// Token: 0x0400013D RID: 317
		private int _MinorVersion;

		// Token: 0x0400013E RID: 318
		private byte[] _Random;

		// Token: 0x0400013F RID: 319
		private byte[] _SessionID;

		// Token: 0x04000140 RID: 320
		private uint _iCipherSuite;

		// Token: 0x04000141 RID: 321
		private int _iCompression;

		// Token: 0x04000142 RID: 322
		private List<string> _Extensions;
	}
}
