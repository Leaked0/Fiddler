using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using FiddlerCore.Utilities;
using FiddlerCore.Utilities.SmartAssembly.Attributes;

namespace Fiddler
{
	/// <summary>
	/// The Session object manages the complete HTTP session including the UI listitem, the ServerChatter, and the ClientChatter.
	/// </summary>
	// Token: 0x0200005A RID: 90
	[DebuggerDisplay("Session #{m_requestID}, {m_state}, {fullUrl}, [{BitFlags}]")]
	public class Session
	{
		/// <summary>
		/// This event fires when new session is created.
		/// </summary>
		// Token: 0x14000012 RID: 18
		// (add) Token: 0x060003CB RID: 971 RVA: 0x00025CB0 File Offset: 0x00023EB0
		// (remove) Token: 0x060003CC RID: 972 RVA: 0x00025CE4 File Offset: 0x00023EE4
		public static event EventHandler<Session> SessionCreated;

		/// <summary>
		/// This event fires when one of its fields is changed
		/// </summary>
		// Token: 0x14000013 RID: 19
		// (add) Token: 0x060003CD RID: 973 RVA: 0x00025D18 File Offset: 0x00023F18
		// (remove) Token: 0x060003CE RID: 974 RVA: 0x00025D4C File Offset: 0x00023F4C
		internal static event EventHandler<Session> SessionFieldChanged;

		/// <summary>
		/// Bitflags of commonly-queried session attributes
		/// </summary>
		// Token: 0x170000AD RID: 173
		// (get) Token: 0x060003CF RID: 975 RVA: 0x00025D7F File Offset: 0x00023F7F
		// (set) Token: 0x060003D0 RID: 976 RVA: 0x00025D88 File Offset: 0x00023F88
		public SessionFlags BitFlags
		{
			get
			{
				return this._bitFlags;
			}
			internal set
			{
				if (CONFIG.bDebugSpew && value != this._bitFlags)
				{
					FiddlerApplication.DebugSpew("Session #{0} bitflags adjusted from {1} to {2} @ {3}", new object[]
					{
						this.id,
						this._bitFlags,
						value,
						Environment.StackTrace
					});
				}
				this._bitFlags = value;
			}
		}

		/// <summary>
		/// DO NOT USE. TEMPORARY WHILE REFACTORING VISIBILITY OF MEMBERS
		/// </summary>
		/// <param name="FlagsToSet"></param>
		/// <param name="b"></param>
		// Token: 0x060003D1 RID: 977 RVA: 0x00025DE9 File Offset: 0x00023FE9
		public void UNSTABLE_SetBitFlag(SessionFlags FlagsToSet, bool b)
		{
			this.SetBitFlag(FlagsToSet, b);
		}

		/// <summary>
		/// Sets or unsets the specified SessionFlag(s)
		/// </summary>
		/// <param name="FlagsToSet">SessionFlags</param>
		/// <param name="b">Desired set value</param>
		// Token: 0x060003D2 RID: 978 RVA: 0x00025DF3 File Offset: 0x00023FF3
		internal void SetBitFlag(SessionFlags FlagsToSet, bool b)
		{
			if (b)
			{
				this.BitFlags = this._bitFlags | FlagsToSet;
				return;
			}
			this.BitFlags = this._bitFlags & ~FlagsToSet;
		}

		/// <summary>
		/// Test the session's BitFlags
		/// </summary>
		/// <param name="FlagsToTest">One or more (OR'd) SessionFlags</param>
		/// <returns>TRUE if ALL specified flag(s) are set</returns>
		// Token: 0x060003D3 RID: 979 RVA: 0x00025E16 File Offset: 0x00024016
		public bool isFlagSet(SessionFlags FlagsToTest)
		{
			return FlagsToTest == (this._bitFlags & FlagsToTest);
		}

		/// <summary>
		/// Test the session's BitFlags
		/// </summary>
		/// <param name="FlagsToTest">One or more (OR'd) SessionFlags</param>
		/// <returns>TRUE if ANY of specified flag(s) are set</returns>
		// Token: 0x060003D4 RID: 980 RVA: 0x00025E23 File Offset: 0x00024023
		public bool isAnyFlagSet(SessionFlags FlagsToTest)
		{
			return (this._bitFlags & FlagsToTest) > SessionFlags.None;
		}

		/// <summary>
		/// Returns True if this is a HTTP CONNECT tunnel.
		/// </summary>
		// Token: 0x170000AE RID: 174
		// (get) Token: 0x060003D5 RID: 981 RVA: 0x00025E30 File Offset: 0x00024030
		// (set) Token: 0x060003D6 RID: 982 RVA: 0x00025E38 File Offset: 0x00024038
		public bool isTunnel
		{
			get; [DoNotObfuscate]
			internal set;
		}

		/// <summary>
		/// A common use for the Tag property is to store data that is closely associated with the Session.
		/// It is NOT marshalled during drag/drop and is NOT serialized to a SAZ file.
		/// </summary>
		// Token: 0x170000AF RID: 175
		// (get) Token: 0x060003D7 RID: 983 RVA: 0x00025E41 File Offset: 0x00024041
		// (set) Token: 0x060003D8 RID: 984 RVA: 0x00025E49 File Offset: 0x00024049
		public object Tag { get; set; }

		/// <summary>
		/// This event fires at any time the session's State changes. Use with caution due to the potential for performance impact.
		/// </summary>
		// Token: 0x14000014 RID: 20
		// (add) Token: 0x060003D9 RID: 985 RVA: 0x00025E54 File Offset: 0x00024054
		// (remove) Token: 0x060003DA RID: 986 RVA: 0x00025E8C File Offset: 0x0002408C
		public event EventHandler<StateChangeEventArgs> OnStateChanged;

		/// <summary>
		/// This event fires if this Session automatically yields a new one, for instance, if Fiddler is configured to automatically
		/// follow redirects or perform multi-leg authentication (X-AutoAuth).
		/// </summary>
		// Token: 0x14000015 RID: 21
		// (add) Token: 0x060003DB RID: 987 RVA: 0x00025EC4 File Offset: 0x000240C4
		// (remove) Token: 0x060003DC RID: 988 RVA: 0x00025EFC File Offset: 0x000240FC
		public event EventHandler<ContinueTransactionEventArgs> OnContinueTransaction;

		// Token: 0x14000016 RID: 22
		// (add) Token: 0x060003DD RID: 989 RVA: 0x00025F34 File Offset: 0x00024134
		// (remove) Token: 0x060003DE RID: 990 RVA: 0x00025F6C File Offset: 0x0002416C
		public event EventHandler<EventArgs> OnCompleteTransaction;

		/// <summary>
		/// If this session is a Tunnel, and the tunnel's IsOpen property is TRUE, returns TRUE. Otherwise returns FALSE.
		/// </summary>
		// Token: 0x170000B0 RID: 176
		// (get) Token: 0x060003DF RID: 991 RVA: 0x00025FA1 File Offset: 0x000241A1
		public bool TunnelIsOpen
		{
			get
			{
				return this.__oTunnel != null && this.__oTunnel.IsOpen;
			}
		}

		/// <summary>
		/// If this session is a Tunnel, returns number of bytes sent from the Server to the Client
		/// </summary>
		// Token: 0x170000B1 RID: 177
		// (get) Token: 0x060003E0 RID: 992 RVA: 0x00025FB8 File Offset: 0x000241B8
		public long TunnelIngressByteCount
		{
			get
			{
				if (this.__oTunnel != null)
				{
					return this.__oTunnel.IngressByteCount;
				}
				return 0L;
			}
		}

		/// <summary>
		/// If this session is a Tunnel, returns number of bytes sent from the Client to the Server
		/// </summary>
		// Token: 0x170000B2 RID: 178
		// (get) Token: 0x060003E1 RID: 993 RVA: 0x00025FD0 File Offset: 0x000241D0
		public long TunnelEgressByteCount
		{
			get
			{
				if (this.__oTunnel != null)
				{
					return this.__oTunnel.EgressByteCount;
				}
				return 0L;
			}
		}

		// Token: 0x170000B3 RID: 179
		// (get) Token: 0x060003E2 RID: 994 RVA: 0x00025FE8 File Offset: 0x000241E8
		[CodeDescription("Gets Request Headers, or empty headers if headers do not exist")]
		public HTTPRequestHeaders RequestHeaders
		{
			get
			{
				HTTPRequestHeaders oRH = null;
				if (Utilities.HasHeaders(this.oRequest))
				{
					oRH = this.oRequest.headers;
				}
				if (oRH == null)
				{
					oRH = new HTTPRequestHeaders("/", null);
				}
				return oRH;
			}
		}

		// Token: 0x170000B4 RID: 180
		// (get) Token: 0x060003E3 RID: 995 RVA: 0x00026020 File Offset: 0x00024220
		[CodeDescription("Gets Response Headers, or empty headers if headers do not exist")]
		public HTTPResponseHeaders ResponseHeaders
		{
			get
			{
				HTTPResponseHeaders oRH = null;
				if (Utilities.HasHeaders(this.oResponse))
				{
					oRH = this.oResponse.headers;
				}
				if (oRH == null)
				{
					oRH = new HTTPResponseHeaders(0, "HEADERS NOT SET", null);
				}
				return oRH;
			}
		}

		/// <summary>
		/// Gets or Sets the HTTP Request body bytes. 
		/// Setter adjusts Content-Length header, and removes Transfer-Encoding and Content-Encoding headers.
		/// Setter DOES NOT CLONE the passed array.
		/// Setter will throw if the Request object does not exist for some reason.
		/// Use utilSetRequestBody(sStr) to ensure proper character encoding if you need to use a string.
		/// </summary>
		// Token: 0x170000B5 RID: 181
		// (get) Token: 0x060003E4 RID: 996 RVA: 0x00026059 File Offset: 0x00024259
		// (set) Token: 0x060003E5 RID: 997 RVA: 0x0002606C File Offset: 0x0002426C
		[CodeDescription("Gets or Sets the Request body bytes; Setter fixes up headers.")]
		public byte[] RequestBody
		{
			get
			{
				return this.requestBodyBytes ?? Utilities.emptyByteArray;
			}
			set
			{
				if (value == null)
				{
					value = Utilities.emptyByteArray;
				}
				this.oRequest.headers.Remove("Transfer-Encoding");
				this.oRequest.headers.Remove("Content-Encoding");
				this.requestBodyBytes = value;
				this.oRequest.headers["Content-Length"] = ((long)value.Length).ToString();
			}
		}

		// Token: 0x170000B6 RID: 182
		// (get) Token: 0x060003E6 RID: 998 RVA: 0x000260D4 File Offset: 0x000242D4
		// (set) Token: 0x060003E7 RID: 999 RVA: 0x000260F9 File Offset: 0x000242F9
		[CodeDescription("Gets or Sets the request's Method (e.g. GET, POST, etc).")]
		public string RequestMethod
		{
			get
			{
				if (!Utilities.HasHeaders(this.oRequest))
				{
					return string.Empty;
				}
				return this.oRequest.headers.HTTPMethod;
			}
			set
			{
				if (!Utilities.HasHeaders(this.oRequest))
				{
					return;
				}
				this.oRequest.headers.HTTPMethod = value;
			}
		}

		/// <summary>
		/// Gets or Sets the HTTP Response body bytes.
		/// Setter adjusts Content-Length header, and removes Transfer-Encoding and Content-Encoding headers.
		/// Setter DOES NOT CLONE the passed array.
		/// Setter will throw if the Response object has not yet been created. (See utilCreateResponseAndBypassServer)
		/// Use utilSetResponseBody(sStr) to ensure proper character encoding if you need to use a string.
		/// </summary>
		// Token: 0x170000B7 RID: 183
		// (get) Token: 0x060003E8 RID: 1000 RVA: 0x0002611A File Offset: 0x0002431A
		// (set) Token: 0x060003E9 RID: 1001 RVA: 0x0002612C File Offset: 0x0002432C
		[CodeDescription("Gets or Sets the Response body bytes; Setter fixes up headers.")]
		public byte[] ResponseBody
		{
			get
			{
				return this.responseBodyBytes ?? Utilities.emptyByteArray;
			}
			set
			{
				if (value == null)
				{
					value = Utilities.emptyByteArray;
				}
				this.oResponse.headers.Remove("Transfer-Encoding");
				this.oResponse.headers.Remove("Content-Encoding");
				this.responseBodyBytes = value;
				this.oResponse.headers["Content-Length"] = ((long)value.Length).ToString();
			}
		}

		/// <summary>
		/// When true, this session was conducted using the HTTPS protocol.
		/// </summary>
		// Token: 0x170000B8 RID: 184
		// (get) Token: 0x060003EA RID: 1002 RVA: 0x00026194 File Offset: 0x00024394
		[CodeDescription("When true, this session was conducted using the HTTPS protocol.")]
		public bool isHTTPS
		{
			get
			{
				return Utilities.HasHeaders(this.oRequest) && "HTTPS".OICEquals(this.oRequest.headers.UriScheme);
			}
		}

		/// <summary>
		/// When true, this session was conducted using the FTP protocol.
		/// </summary>
		// Token: 0x170000B9 RID: 185
		// (get) Token: 0x060003EB RID: 1003 RVA: 0x000261BF File Offset: 0x000243BF
		[CodeDescription("When true, this session was conducted using the FTP protocol.")]
		public bool isFTP
		{
			get
			{
				return Utilities.HasHeaders(this.oRequest) && "FTP".OICEquals(this.oRequest.headers.UriScheme);
			}
		}

		/// <summary>
		/// Returns TRUE if the Session's HTTP Method is available and matches the target method.
		/// </summary>
		/// <param name="sTestFor">The target HTTP Method being compared.</param>
		/// <returns>true, if the method is specified and matches sTestFor (case-insensitive); otherwise false.</returns>
		// Token: 0x060003EC RID: 1004 RVA: 0x000261EA File Offset: 0x000243EA
		[CodeDescription("Returns TRUE if the Session's HTTP Method is available and matches the target method.")]
		public bool HTTPMethodIs(string sTestFor)
		{
			return this.RequestMethod.OICEquals(sTestFor);
		}

		/// <summary>
		/// Returns TRUE if the Session's target hostname (no port) matches sTestHost (case-insensitively).
		/// </summary>
		/// <param name="sTestHost">The host to which this session's host should be compared.</param>
		/// <returns>True if this session is targeted to the specified host.</returns>
		// Token: 0x060003ED RID: 1005 RVA: 0x000261F8 File Offset: 0x000243F8
		[CodeDescription("Returns TRUE if the Session's target hostname (no port) matches sTestHost (case-insensitively).")]
		public bool HostnameIs(string sTestHost)
		{
			if (this.oRequest == null)
			{
				return false;
			}
			int ixToken = this.oRequest.host.LastIndexOf(':');
			if (ixToken > -1 && ixToken > this.oRequest.host.LastIndexOf(']'))
			{
				return string.Compare(this.oRequest.host, 0, sTestHost, 0, ixToken, StringComparison.OrdinalIgnoreCase) == 0;
			}
			return this.oRequest.host.OICEquals(sTestHost);
		}

		/// <summary>
		/// Get the process ID of the application which made this request, or 0 if it cannot be determined.
		/// </summary>
		// Token: 0x170000BA RID: 186
		// (get) Token: 0x060003EE RID: 1006 RVA: 0x00026265 File Offset: 0x00024465
		[CodeDescription("Get the process ID of the application which made this request, or 0 if it cannot be determined.")]
		public int LocalProcessID
		{
			get
			{
				return this._LocalProcessID;
			}
		}

		/// <summary>
		/// Get the Process Info of the application which made this request, or String.Empty if it is not known
		/// </summary>
		// Token: 0x170000BB RID: 187
		// (get) Token: 0x060003EF RID: 1007 RVA: 0x0002626D File Offset: 0x0002446D
		[CodeDescription("Get the Process Info the application which made this request, or String.Empty if it cannot be determined.")]
		public string LocalProcess
		{
			get
			{
				if (!this.oFlags.ContainsKey("X-ProcessInfo"))
				{
					return string.Empty;
				}
				return this.oFlags["X-ProcessInfo"];
			}
		}

		/// <summary>
		/// Replaces any characters in a filename that are unsafe with safe equivalents, and trim to 160 characters.
		/// </summary>
		/// <param name="sFilename"></param>
		/// <returns></returns>
		// Token: 0x060003F0 RID: 1008 RVA: 0x00026298 File Offset: 0x00024498
		private static string _MakeSafeFilename(string sFilename)
		{
			char[] arrCharUnsafe = Path.GetInvalidFileNameChars();
			if (sFilename.IndexOfAny(arrCharUnsafe) < 0)
			{
				return Utilities.TrimTo(sFilename, 160);
			}
			StringBuilder sbFilename = new StringBuilder(sFilename);
			for (int x = 0; x < sbFilename.Length; x++)
			{
				if (Array.IndexOf<char>(arrCharUnsafe, sFilename[x]) > -1)
				{
					sbFilename[x] = '-';
				}
			}
			return Utilities.TrimTo(sbFilename.ToString(), 160);
		}

		/// <summary>
		/// Gets a path-less filename suitable for saving the Response entity. Uses Content-Disposition if available.
		/// </summary>
		// Token: 0x170000BC RID: 188
		// (get) Token: 0x060003F1 RID: 1009 RVA: 0x00026304 File Offset: 0x00024504
		[CodeDescription("Gets a path-less filename suitable for saving the Response entity. Uses Content-Disposition if available.")]
		public string SuggestedFilename
		{
			get
			{
				if (!Utilities.HasHeaders(this.oResponse))
				{
					return this.id.ToString() + ".txt";
				}
				if (Utilities.IsNullOrEmpty(this.responseBodyBytes))
				{
					string sFormat = "{0}_Status{1}.txt";
					return string.Format(sFormat, this.id.ToString(), this.responseCode.ToString());
				}
				string sResult = this.oResponse.headers.GetTokenValue("Content-Disposition", "filename*");
				if (sResult != null && sResult.Length > 7 && sResult.OICStartsWith("utf-8''"))
				{
					return Utilities.UrlDecode(sResult.Substring(7));
				}
				sResult = this.oResponse.headers.GetTokenValue("Content-Disposition", "filename");
				if (sResult != null)
				{
					return Session._MakeSafeFilename(sResult);
				}
				string sCandidateFilename = Utilities.TrimBeforeLast(Utilities.TrimAfter(this.url, '?'), '/');
				if (sCandidateFilename.Length > 0 && sCandidateFilename.Length < 64 && sCandidateFilename.Contains(".") && sCandidateFilename.LastIndexOf('.') == sCandidateFilename.IndexOf('.'))
				{
					string sFilename = Session._MakeSafeFilename(sCandidateFilename);
					string sNewExtension = string.Empty;
					if (this.url.Contains("?") || sFilename.Length < 1 || sFilename.OICEndsWithAny(new string[] { ".aspx", ".php", ".jsp", ".asp", ".asmx", ".cgi", ".cfm", ".ashx" }))
					{
						sNewExtension = this._GetSuggestedFilenameExt();
						if (sFilename.OICEndsWith(sNewExtension))
						{
							sNewExtension = string.Empty;
						}
					}
					string sFormat2 = (FiddlerApplication.Prefs.GetBoolPref("fiddler.session.prependIDtosuggestedfilename", false) ? "{0}_{1}{2}" : "{1}{2}");
					return string.Format(sFormat2, this.id.ToString(), sFilename, sNewExtension);
				}
				StringBuilder sbResult = new StringBuilder(32);
				sbResult.Append(this.id);
				sbResult.Append("_");
				sbResult.Append(this._GetSuggestedFilenameExt());
				return sbResult.ToString();
			}
		}

		/// <summary>
		/// Examines the MIME type, and if ambiguous, returns sniffs the body.
		/// </summary>
		/// <returns></returns>
		// Token: 0x060003F2 RID: 1010 RVA: 0x0002652C File Offset: 0x0002472C
		private string _GetSuggestedFilenameExt()
		{
			string extension = Utilities.FileExtensionForMIMEType(this.oResponse.MIMEType);
			if (extension != ".txt")
			{
				return extension;
			}
			string sniffedExtension;
			if (MimeSniffer.Instance.TrySniff(this.responseBodyBytes, out sniffedExtension))
			{
				return sniffedExtension;
			}
			return extension;
		}

		/// <summary>
		/// Set to true in OnBeforeRequest if this request should bypass the gateway
		/// </summary>
		// Token: 0x170000BD RID: 189
		// (get) Token: 0x060003F3 RID: 1011 RVA: 0x00026570 File Offset: 0x00024770
		// (set) Token: 0x060003F4 RID: 1012 RVA: 0x00026578 File Offset: 0x00024778
		[CodeDescription("Set to true in OnBeforeRequest if this request should bypass the gateway")]
		public bool bypassGateway
		{
			get
			{
				return this._bypassGateway;
			}
			set
			{
				this._bypassGateway = value;
			}
		}

		/// <summary>
		/// Returns the port used by the client to communicate to Fiddler.
		/// </summary>
		// Token: 0x170000BE RID: 190
		// (get) Token: 0x060003F5 RID: 1013 RVA: 0x00026581 File Offset: 0x00024781
		[CodeDescription("Returns the port used by the client to communicate to Fiddler.")]
		public int clientPort
		{
			get
			{
				return this.m_clientPort;
			}
		}

		/// <summary>
		/// State of session. Note Side-Effects: If setting to .Aborted, calls FinishUISession. If setting to/from a Tamper state, calls RefreshMyInspectors
		/// </summary>
		// Token: 0x170000BF RID: 191
		// (get) Token: 0x060003F6 RID: 1014 RVA: 0x00026589 File Offset: 0x00024789
		// (set) Token: 0x060003F7 RID: 1015 RVA: 0x00026594 File Offset: 0x00024794
		[CodeDescription("Enumerated state of the current session.")]
		public SessionStates state
		{
			get
			{
				return this.m_state;
			}
			set
			{
				SessionStates oldState = this.m_state;
				this.m_state = value;
				if (this.m_state == SessionStates.Aborted)
				{
					this.oFlags["X-Aborted-When"] = oldState.ToString();
				}
				this.RaiseOnStateChangedIfNotIgnored(oldState, value);
				if (this.m_state >= SessionStates.Done)
				{
					this.OnStateChanged = null;
					this.FireCompleteTransaction();
				}
			}
		}

		// Token: 0x060003F8 RID: 1016 RVA: 0x000265F5 File Offset: 0x000247F5
		private void SaveNewSessionPPID(string sessionProcessInfo, int PPID)
		{
			Session._sessionProcessInfoToPPID[sessionProcessInfo] = PPID;
		}

		// Token: 0x060003F9 RID: 1017 RVA: 0x00026604 File Offset: 0x00024804
		private int GetSessionPPID(string sessionProcessInfo)
		{
			string[] sessionProcessInfoArr = sessionProcessInfo.Split(':', StringSplitOptions.None);
			bool isProcessNameValid = sessionProcessInfoArr.Length != 0 && !string.IsNullOrEmpty(sessionProcessInfoArr[0]);
			int processId;
			bool isProcessIdValid = sessionProcessInfoArr.Length > 1 && !string.IsNullOrEmpty(sessionProcessInfoArr[1]) && int.TryParse(sessionProcessInfoArr[1], out processId) && processId != 0;
			int sessionPPID = 0;
			if (isProcessNameValid && isProcessIdValid)
			{
				if (Session._sessionProcessInfoToPPID.ContainsKey(sessionProcessInfo))
				{
					sessionPPID = Session._sessionProcessInfoToPPID[sessionProcessInfo];
				}
				else
				{
					sessionPPID = FiddlerProcessHelper.TryGetParentProcessId(this._LocalProcessID);
					this.SaveNewSessionPPID(sessionProcessInfo, sessionPPID);
				}
			}
			return sessionPPID;
		}

		// Token: 0x060003FA RID: 1018 RVA: 0x00026690 File Offset: 0x00024890
		private void SetDecryptFlagIfSessionIsFromInstrumentedBrowserProcess(string sessionProcessInfo)
		{
			if (CONFIG.InstrumentedBrowserProcessIDs.Count > 0 && !string.IsNullOrEmpty(sessionProcessInfo))
			{
				int sessionPPID = this.GetSessionPPID(sessionProcessInfo);
				if (CONFIG.InstrumentedBrowserProcessIDs.Contains(sessionPPID))
				{
					this.oFlags["x-instrumented-browser-decrypt"] = "DecryptSessionFlag";
				}
			}
		}

		// Token: 0x060003FB RID: 1019 RVA: 0x000266DC File Offset: 0x000248DC
		private void RaiseOnStateChangedIfNotIgnored(SessionStates oldState, SessionStates newState)
		{
			if (!this.isFlagSet(SessionFlags.Ignored))
			{
				EventHandler<StateChangeEventArgs> oToNotify = this.OnStateChanged;
				if (oToNotify != null)
				{
					StateChangeEventArgs eaSC = new StateChangeEventArgs(oldState, newState);
					oToNotify(this, eaSC);
				}
			}
		}

		/// <summary>
		/// Notify extensions if this Session naturally led to another (e.g. due to redirect chasing or Automatic Authentication)
		/// </summary>
		/// <param name="oOrig">The original session</param>
		/// <param name="oNew">The new session created</param>
		// Token: 0x060003FC RID: 1020 RVA: 0x0002670C File Offset: 0x0002490C
		private void FireContinueTransaction(Session oOrig, Session oNew, ContinueTransactionReason oReason)
		{
			EventHandler<ContinueTransactionEventArgs> oToNotify = this.OnContinueTransaction;
			if (this.OnCompleteTransaction != null)
			{
				oNew.OnCompleteTransaction = this.OnCompleteTransaction;
				this.OnCompleteTransaction = null;
			}
			this.OnContinueTransaction = null;
			if (oToNotify != null)
			{
				ContinueTransactionEventArgs eaCT = new ContinueTransactionEventArgs(oOrig, oNew, oReason);
				oToNotify(this, eaCT);
			}
		}

		// Token: 0x060003FD RID: 1021 RVA: 0x00026758 File Offset: 0x00024958
		private void FireCompleteTransaction()
		{
			EventHandler<EventArgs> oToNotify = this.OnCompleteTransaction;
			this.OnContinueTransaction = null;
			this.OnCompleteTransaction = null;
			if (oToNotify != null)
			{
				oToNotify(this, new EventArgs());
			}
		}

		/// <summary>
		/// Returns the path and query part of the URL. (For a CONNECT request, returns the host:port to be connected.)
		/// </summary>
		// Token: 0x170000C0 RID: 192
		// (get) Token: 0x060003FE RID: 1022 RVA: 0x0002678C File Offset: 0x0002498C
		// (set) Token: 0x060003FF RID: 1023 RVA: 0x000267B4 File Offset: 0x000249B4
		[CodeDescription("Returns the path and query part of the URL. (For a CONNECT request, returns the host:port to be connected.)")]
		public string PathAndQuery
		{
			get
			{
				HTTPRequestHeaders oRH = this.oRequest.headers;
				if (oRH == null)
				{
					return string.Empty;
				}
				return oRH.RequestPath;
			}
			set
			{
				this.oRequest.headers.RequestPath = value;
			}
		}

		/// <summary>
		/// Retrieves the complete URI, including protocol/scheme, in the form http://www.host.com/filepath?query.
		/// Or sets the complete URI, adjusting the UriScheme and/or Host.
		/// </summary>
		// Token: 0x170000C1 RID: 193
		// (get) Token: 0x06000400 RID: 1024 RVA: 0x000267C7 File Offset: 0x000249C7
		// (set) Token: 0x06000401 RID: 1025 RVA: 0x000267FC File Offset: 0x000249FC
		[CodeDescription("Retrieves the complete URI, including protocol/scheme, in the form http://www.host.com/filepath?query.")]
		public string fullUrl
		{
			get
			{
				if (!Utilities.HasHeaders(this.oRequest))
				{
					return string.Empty;
				}
				return string.Format("{0}://{1}", this.oRequest.headers.UriScheme, this.url);
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					throw new ArgumentException("Must specify a complete URI");
				}
				string sScheme = Utilities.TrimAfter(value, "://").ToLowerInvariant();
				string sRemainder = Utilities.TrimBefore(value, "://");
				if (sScheme != "http" && sScheme != "https" && sScheme != "ftp")
				{
					throw new ArgumentException("URI scheme must be http, https, or ftp");
				}
				this.oRequest.headers.UriScheme = sScheme;
				this.url = sRemainder;
			}
		}

		/// <summary>
		/// Gets or sets the URL (without protocol) being requested from the server, in the form www.host.com/filepath?query.
		/// </summary>
		// Token: 0x170000C2 RID: 194
		// (get) Token: 0x06000402 RID: 1026 RVA: 0x00026883 File Offset: 0x00024A83
		// (set) Token: 0x06000403 RID: 1027 RVA: 0x000268AC File Offset: 0x00024AAC
		[CodeDescription("Gets or sets the URL (without protocol) being requested from the server, in the form www.host.com/filepath?query.")]
		public string url
		{
			get
			{
				if (this.HTTPMethodIs("CONNECT"))
				{
					return this.PathAndQuery;
				}
				return this.host + this.PathAndQuery;
			}
			set
			{
				if (value.OICStartsWithAny(new string[] { "http://", "https://", "ftp://" }))
				{
					throw new ArgumentException("If you wish to specify a protocol, use the fullUrl property instead. Input was: " + value);
				}
				if (this.HTTPMethodIs("CONNECT"))
				{
					this.PathAndQuery = value;
					this.host = value;
					return;
				}
				int ixToken = value.IndexOfAny(new char[] { '/', '?' });
				if (ixToken > -1)
				{
					this.host = value.Substring(0, ixToken);
					this.PathAndQuery = value.Substring(ixToken);
					return;
				}
				this.host = value;
				this.PathAndQuery = "/";
			}
		}

		/// <summary>
		/// DNS Name of the host server targeted by this request. May include IPv6 literal brackets. NB: a port# may be included.
		/// </summary>
		// Token: 0x170000C3 RID: 195
		// (get) Token: 0x06000404 RID: 1028 RVA: 0x00026957 File Offset: 0x00024B57
		// (set) Token: 0x06000405 RID: 1029 RVA: 0x00026972 File Offset: 0x00024B72
		[CodeDescription("Gets/Sets the host to which this request is targeted. MAY include IPv6 literal brackets. MAY include a trailing port#.")]
		public string host
		{
			get
			{
				if (this.oRequest == null)
				{
					return string.Empty;
				}
				return this.oRequest.host;
			}
			set
			{
				if (this.oRequest != null)
				{
					this.oRequest.host = value;
				}
			}
		}

		/// <summary>
		/// DNS Name of the host server (no port) targeted by this request. Will include IPv6-literal brackets for IPv6-literal addresses
		/// </summary>
		// Token: 0x170000C4 RID: 196
		// (get) Token: 0x06000406 RID: 1030 RVA: 0x00026988 File Offset: 0x00024B88
		// (set) Token: 0x06000407 RID: 1031 RVA: 0x000269DC File Offset: 0x00024BDC
		[CodeDescription("Gets/Sets the hostname to which this request is targeted; does NOT include any port# but will include IPv6-literal brackets for IPv6 literals.")]
		public string hostname
		{
			get
			{
				string sHost = this.oRequest.host;
				if (sHost.Length < 1)
				{
					return string.Empty;
				}
				int ixToken = sHost.LastIndexOf(':');
				if (ixToken > -1 && ixToken > sHost.LastIndexOf(']'))
				{
					return sHost.Substring(0, ixToken);
				}
				return this.oRequest.host;
			}
			set
			{
				int ixToken = value.LastIndexOf(':');
				if (ixToken > -1 && ixToken > value.LastIndexOf(']'))
				{
					throw new ArgumentException("Do not specify a port when setting hostname; use host property instead.");
				}
				string sOldHost = (this.HTTPMethodIs("CONNECT") ? this.PathAndQuery : this.host);
				ixToken = sOldHost.LastIndexOf(':');
				if (ixToken > -1 && ixToken > sOldHost.LastIndexOf(']'))
				{
					this.host = value + sOldHost.Substring(ixToken);
					return;
				}
				this.host = value;
			}
		}

		/// <summary>
		/// Returns the server port to which this request is targeted.
		/// </summary>
		// Token: 0x170000C5 RID: 197
		// (get) Token: 0x06000408 RID: 1032 RVA: 0x00026A5C File Offset: 0x00024C5C
		// (set) Token: 0x06000409 RID: 1033 RVA: 0x00026ABD File Offset: 0x00024CBD
		[CodeDescription("Returns the server port to which this request is targeted.")]
		public int port
		{
			get
			{
				string sHost = (this.HTTPMethodIs("CONNECT") ? this.oRequest.headers.RequestPath : this.oRequest.host);
				int iPort = (this.isHTTPS ? 443 : (this.isFTP ? 21 : 80));
				string sDontCare;
				Utilities.CrackHostAndPort(sHost, out sDontCare, ref iPort);
				return iPort;
			}
			set
			{
				if (value < 0 || value > 65535)
				{
					throw new ArgumentException("A valid target port value (0-65535) must be specified.");
				}
				this.host = string.Format("{0}:{1}", this.hostname, value);
			}
		}

		/// <summary>
		/// Returns the sequential number of this session. Note, by default numbering is restarted at zero when the session list is cleared.
		/// </summary>
		// Token: 0x170000C6 RID: 198
		// (get) Token: 0x0600040A RID: 1034 RVA: 0x00026AF2 File Offset: 0x00024CF2
		[CodeDescription("Returns the sequential number of this request.")]
		public int id
		{
			get
			{
				return this.m_requestID;
			}
		}

		/// <summary>
		/// Returns the Address used by the client to communicate to Fiddler.
		/// </summary>
		// Token: 0x170000C7 RID: 199
		// (get) Token: 0x0600040B RID: 1035 RVA: 0x00026AFA File Offset: 0x00024CFA
		[CodeDescription("Returns the Address used by the client to communicate to Fiddler.")]
		public string clientIP
		{
			get
			{
				if (this.m_clientIP != null)
				{
					return this.m_clientIP;
				}
				return "0.0.0.0";
			}
		}

		/// <summary>
		/// Gets or Sets the HTTP Status code of the server's response
		/// </summary>
		// Token: 0x170000C8 RID: 200
		// (get) Token: 0x0600040C RID: 1036 RVA: 0x00026B10 File Offset: 0x00024D10
		// (set) Token: 0x0600040D RID: 1037 RVA: 0x00026B31 File Offset: 0x00024D31
		[CodeDescription("Gets or Sets the HTTP Status code of the server's response")]
		public int responseCode
		{
			get
			{
				if (Utilities.HasHeaders(this.oResponse))
				{
					return this.oResponse.headers.HTTPResponseCode;
				}
				return 0;
			}
			set
			{
				if (Utilities.HasHeaders(this.oResponse))
				{
					this.oResponse.headers.SetStatus(value, "Fiddled");
				}
			}
		}

		/// <summary>
		/// Returns HTML representing the Session. Call Utilities.StringToCF_HTML on the result of this function before placing it on the clipboard.
		/// </summary>
		/// <param name="HeadersOnly">TRUE if only the headers should be copied.</param>
		/// <returns>A HTML-formatted fragment representing the current session.</returns>
		// Token: 0x0600040E RID: 1038 RVA: 0x00026B58 File Offset: 0x00024D58
		public string ToHTMLFragment(bool HeadersOnly)
		{
			if (!Utilities.HasHeaders(this.oRequest))
			{
				return string.Empty;
			}
			StringBuilder sbOutput = new StringBuilder();
			sbOutput.Append("<span class='REQUEST'>");
			sbOutput.Append(Utilities.HtmlEncode(this.oRequest.headers.ToString(true, true, true)).Replace("\r\n", "<br />"));
			if (!HeadersOnly && !Utilities.IsNullOrEmpty(this.requestBodyBytes))
			{
				Encoding oEnc = this.GetRequestBodyEncoding();
				sbOutput.Append(Utilities.HtmlEncode(oEnc.GetString(this.requestBodyBytes)).Replace("\r\n", "<br />"));
			}
			sbOutput.Append("</span><br />");
			if (Utilities.HasHeaders(this.oResponse))
			{
				sbOutput.Append("<span class='RESPONSE'>");
				sbOutput.Append(Utilities.HtmlEncode(this.oResponse.headers.ToString(true, true)).Replace("\r\n", "<br />"));
				if (!HeadersOnly && !Utilities.IsNullOrEmpty(this.responseBodyBytes))
				{
					Encoding oEnc2 = Utilities.getResponseBodyEncoding(this);
					sbOutput.Append(Utilities.HtmlEncode(oEnc2.GetString(this.responseBodyBytes)).Replace("\r\n", "<br />"));
				}
				sbOutput.Append("</span>");
			}
			return sbOutput.ToString();
		}

		/// <summary>
		/// Store this session's request and response to a string.
		/// </summary>
		/// <param name="HeadersOnly">If true, return only the request and response headers</param>
		/// <returns>String representing this session</returns>
		// Token: 0x0600040F RID: 1039 RVA: 0x00026C9C File Offset: 0x00024E9C
		public string ToString(bool HeadersOnly)
		{
			if (!Utilities.HasHeaders(this.oRequest))
			{
				return string.Empty;
			}
			StringBuilder sbOutput = new StringBuilder();
			sbOutput.Append(this.oRequest.headers.ToString(true, true, true));
			if (!HeadersOnly && !Utilities.IsNullOrEmpty(this.requestBodyBytes))
			{
				Encoding oEnc = this.GetRequestBodyEncoding();
				sbOutput.Append(oEnc.GetString(this.requestBodyBytes));
			}
			sbOutput.Append("\r\n");
			if (Utilities.HasHeaders(this.oResponse))
			{
				sbOutput.Append(this.oResponse.headers.ToString(true, true));
				if (!HeadersOnly && !Utilities.IsNullOrEmpty(this.responseBodyBytes))
				{
					Encoding oEnc2 = Utilities.getResponseBodyEncoding(this);
					sbOutput.Append(oEnc2.GetString(this.responseBodyBytes));
				}
				sbOutput.Append("\r\n");
			}
			return sbOutput.ToString();
		}

		/// <summary>
		/// Store this session's request and response to a string.
		/// </summary>
		/// <returns>A string containing the content of the request and response.</returns>
		// Token: 0x06000410 RID: 1040 RVA: 0x00026D74 File Offset: 0x00024F74
		public override string ToString()
		{
			return this.ToString(false);
		}

		/// <summary>
		/// This method resumes the Session's thread in response to "Continue" commands from the UI
		/// </summary>
		// Token: 0x06000411 RID: 1041 RVA: 0x00026D80 File Offset: 0x00024F80
		public void ThreadResume()
		{
			if (this.oSyncEvent == null)
			{
				return;
			}
			try
			{
				this.oSyncEvent.Set();
			}
			catch (Exception eX)
			{
			}
		}

		/// <summary>
		/// Set the SessionFlags.Ignore bit for this Session, also configuring it to stream, drop read data, and bypass event handlers.
		/// For a CONNECT Tunnel, traffic will be blindly shuffled back and forth. Session will be hidden.
		/// </summary>
		// Token: 0x06000412 RID: 1042 RVA: 0x00026DB8 File Offset: 0x00024FB8
		[CodeDescription("Sets the SessionFlags.Ignore bit for this Session, hiding it and ignoring its traffic.")]
		public void Ignore()
		{
			this.SetBitFlag(SessionFlags.Ignored, true);
			if (this.HTTPMethodIs("CONNECT"))
			{
				this.oFlags["x-no-decrypt"] = "IgnoreFlag";
				this.oFlags["x-no-parse"] = "IgnoreFlag";
			}
			else
			{
				this.oFlags["log-drop-response-body"] = "IgnoreFlag";
				this.oFlags["log-drop-request-body"] = "IgnoreFlag";
			}
			this.bBufferResponse = false;
		}

		/// <summary>
		/// Called by an AcceptConnection-spawned background thread, create a new session object from a client socket 
		/// and execute the session
		/// </summary>
		/// <param name="oParams">Parameter object defining client socket and endpoint's HTTPS certificate, if present</param>
		// Token: 0x06000413 RID: 1043 RVA: 0x00026E38 File Offset: 0x00025038
		internal static void CreateAndExecute(object oParams)
		{
			try
			{
				DateTime dtNow = DateTime.Now;
				ProxyExecuteParams oPEP = (ProxyExecuteParams)oParams;
				Interlocked.Add(ref COUNTERS.TOTAL_DELAY_ACCEPT_CONNECTION, (long)(dtNow - oPEP.dtConnectionAccepted).TotalMilliseconds);
				Interlocked.Increment(ref COUNTERS.CONNECTIONS_ACCEPTED);
				Socket sockRequest = oPEP.oSocket;
				ClientPipe pipeRequest = new ClientPipe(sockRequest, oPEP.dtConnectionAccepted);
				Session newSession = new Session(pipeRequest, null);
				FiddlerApplication.DoAfterSocketAccept(newSession, sockRequest);
				if (oPEP.oServerCert == null || newSession.AcceptHTTPSRequest(oPEP.oServerCert))
				{
					newSession.Execute(null);
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogString(eX.ToString());
			}
		}

		/// <summary>
		/// Call this method to AuthenticateAsServer on the client pipe (e.g. Fiddler itself is acting as a HTTPS server). 
		/// If configured, the pipe will first sniff the request's TLS ClientHello ServerNameIndicator extension.
		/// </summary>
		/// <param name="oCert">The default certificate to use</param>
		/// <returns>TRUE if a HTTPS handshake was achieved; FALSE for any exceptions or other errors.</returns>
		// Token: 0x06000414 RID: 1044 RVA: 0x00026EEC File Offset: 0x000250EC
		private bool AcceptHTTPSRequest(X509Certificate2 oCert)
		{
			try
			{
				if (CONFIG.bUseSNIForCN)
				{
					byte[] arrSniff = new byte[1024];
					int iPeekCount = this.oRequest.pipeClient.GetRawSocket().Receive(arrSniff, SocketFlags.Peek);
					HTTPSClientHello oHello = new HTTPSClientHello();
					if (oHello.LoadFromStream(new MemoryStream(arrSniff, 0, iPeekCount, false)))
					{
						this.oFlags["https-Client-SessionID"] = oHello.SessionID;
						if (!string.IsNullOrEmpty(oHello.ServerNameIndicator))
						{
							FiddlerApplication.DebugSpew("Secure Endpoint request with SNI of '{0}'", new object[] { oHello.ServerNameIndicator });
							this.oFlags["https-Client-SNIHostname"] = oHello.ServerNameIndicator;
							oCert = CertMaker.FindCert(oHello.ServerNameIndicator);
						}
					}
				}
				if (!this.oRequest.pipeClient.SecureClientPipeDirect(oCert))
				{
					FiddlerApplication.Log.LogString("Failed to secure client connection when acting as Secure Endpoint.");
					return false;
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("Failed to secure client connection when acting as Secure Endpoint: {0}", new object[] { eX.ToString() });
			}
			return true;
		}

		/// <summary>
		/// Call this function while in the "reading response" state to update the responseBodyBytes array with
		/// the partially read response.
		/// </summary>
		/// <returns>TRUE if the peek succeeded; FALSE if not in the ReadingResponse state</returns>
		// Token: 0x06000415 RID: 1045 RVA: 0x00026FFC File Offset: 0x000251FC
		public bool COMETPeek()
		{
			if (this.state != SessionStates.ReadingResponse)
			{
				return false;
			}
			bool result;
			try
			{
				this.responseBodyBytes = this.oResponse._PeekAtBody();
				result = true;
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogString(eX.ToString());
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Prevents the server pipe from this session from being pooled for reuse
		/// </summary>
		// Token: 0x06000416 RID: 1046 RVA: 0x00027050 File Offset: 0x00025250
		public void PoisonServerPipe()
		{
			if (this.oResponse != null)
			{
				this.oResponse._PoisonPipe();
			}
		}

		/// <summary>
		/// Ensures that, after the response is complete, the client socket is closed and not reused.
		/// Does NOT (and must not) close the pipe.
		/// </summary>
		// Token: 0x06000417 RID: 1047 RVA: 0x00027065 File Offset: 0x00025265
		public void PoisonClientPipe()
		{
			this._bAllowClientPipeReuse = false;
		}

		/// <summary>
		/// Immediately close client and server sockets. Call in the event of errors-- doesn't queue server pipes for future reuse.
		/// </summary>
		/// <param name="bNullThemToo"></param>
		// Token: 0x06000418 RID: 1048 RVA: 0x00027070 File Offset: 0x00025270
		private void CloseSessionPipes(bool bNullThemToo)
		{
			if (CONFIG.bDebugSpew)
			{
				FiddlerApplication.DebugSpew("CloseSessionPipes() for Session #{0}", new object[] { this.id });
			}
			if (this.oRequest != null && this.oRequest.pipeClient != null)
			{
				if (CONFIG.bDebugSpew)
				{
					FiddlerApplication.DebugSpew("Closing client pipe...", new object[] { this.id });
				}
				this.oRequest.pipeClient.End();
				if (bNullThemToo)
				{
					this.oRequest.pipeClient = null;
				}
			}
			if (this.oResponse != null && this.oResponse.pipeServer != null)
			{
				FiddlerApplication.DebugSpew("Closing server pipe...", new object[] { this.id });
				this.oResponse.pipeServer.End();
				if (bNullThemToo)
				{
					this.oResponse.pipeServer = null;
				}
			}
		}

		/// <summary>
		/// Closes both client and server pipes and moves state to Aborted; unpauses thread if paused.
		/// </summary>
		// Token: 0x06000419 RID: 1049 RVA: 0x00027150 File Offset: 0x00025350
		public void Abort()
		{
			try
			{
				if (this.isAnyFlagSet(SessionFlags.IsBlindTunnel | SessionFlags.IsDecryptingTunnel | SessionFlags.IsWebSocketTunnel))
				{
					if (this.__oTunnel != null)
					{
						this.__oTunnel.CloseTunnel();
						this.oFlags["x-Fiddler-Aborted"] = "true";
						this.state = SessionStates.Aborted;
					}
				}
				else if (this.m_state < SessionStates.Done)
				{
					this.CloseSessionPipes(true);
					this.oFlags["x-Fiddler-Aborted"] = "true";
					this.state = SessionStates.Aborted;
					this.ThreadResume();
				}
			}
			catch (Exception eX)
			{
			}
		}

		/// <summary>
		/// Checks whether this is a WebSocket, and if so, whether it has logged any parsed messages.
		/// </summary>
		// Token: 0x170000C9 RID: 201
		// (get) Token: 0x0600041A RID: 1050 RVA: 0x000271E8 File Offset: 0x000253E8
		public bool bHasWebSocketMessages
		{
			get
			{
				if (!this.isAnyFlagSet(SessionFlags.IsWebSocketTunnel) || this.HTTPMethodIs("CONNECT"))
				{
					return false;
				}
				WebSocket oWS = this.__oTunnel as WebSocket;
				return oWS != null && oWS.MessageCount > 0;
			}
		}

		/// <summary>
		/// Returns TRUE if this session's State &gt; ReadingResponse, and oResponse, oResponse.headers, and responseBodyBytes are all non-null. Note that
		/// bHasResponse returns FALSE if the session is currently reading, even if a body was copied using the COMETPeek feature
		/// </summary>
		// Token: 0x170000CA RID: 202
		// (get) Token: 0x0600041B RID: 1051 RVA: 0x0002722B File Offset: 0x0002542B
		[CodeDescription("Returns TRUE if this session state>ReadingResponse and oResponse not null.")]
		public bool bHasResponse
		{
			get
			{
				return this.state > SessionStates.ReadingResponse && this.oResponse != null && this.oResponse.headers != null && this.responseBodyBytes != null;
			}
		}

		/// <summary>
		/// Save HTTP response body to Fiddler Captures folder. You likely want to call utilDecodeResponse first.
		/// </summary>
		/// <returns>True if the response body was successfully saved</returns>
		// Token: 0x0600041C RID: 1052 RVA: 0x00027258 File Offset: 0x00025458
		[CodeDescription("Save HTTP response body to Fiddler Captures folder.")]
		public bool SaveResponseBody()
		{
			string sPath = CONFIG.GetPath("Captures");
			StringBuilder sbFilename = new StringBuilder();
			sbFilename.Append(this.SuggestedFilename);
			while (File.Exists(sPath + sbFilename.ToString()))
			{
				sbFilename.Insert(0, this.id.ToString() + "_");
			}
			sbFilename.Insert(0, sPath);
			return this.SaveResponseBody(sbFilename.ToString());
		}

		/// <summary>
		/// Save HTTP response body to specified location. You likely want to call utilDecodeResponse first.
		/// </summary>
		/// <param name="sFilename">The name of the file to which the response body should be saved.</param>
		/// <returns>True if the file was successfully written.</returns>
		// Token: 0x0600041D RID: 1053 RVA: 0x000272D0 File Offset: 0x000254D0
		[CodeDescription("Save HTTP response body to specified location.")]
		public bool SaveResponseBody(string sFilename)
		{
			bool result;
			try
			{
				Utilities.WriteArrayToFile(sFilename, this.responseBodyBytes);
				result = true;
			}
			catch (Exception eX)
			{
				string title = "Save Failed";
				string message = eX.Message + "\n\n" + sFilename;
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[] { title, message });
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Save the request body to a file. You likely want to call utilDecodeRequest first.
		/// </summary>
		/// <param name="sFilename">The name of the file to which the request body should be saved.</param>
		/// <returns>True if the file was successfully written.</returns>
		// Token: 0x0600041E RID: 1054 RVA: 0x00027338 File Offset: 0x00025538
		[CodeDescription("Save HTTP request body to specified location.")]
		public bool SaveRequestBody(string sFilename)
		{
			bool result;
			try
			{
				Utilities.WriteArrayToFile(sFilename, this.requestBodyBytes);
				result = true;
			}
			catch (Exception eX)
			{
				string title = "Save Failed";
				string message = eX.Message + "\n\n" + sFilename;
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[] { title, message });
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Save the request and response to a single file.
		/// </summary>
		/// <param name="sFilename">The filename to which the session should be saved.</param>
		/// <param name="bHeadersOnly">TRUE if only the headers should be written.</param>
		// Token: 0x0600041F RID: 1055 RVA: 0x000273A0 File Offset: 0x000255A0
		public void SaveSession(string sFilename, bool bHeadersOnly)
		{
			Utilities.EnsureOverwritable(sFilename);
			using (FileStream fs = new FileStream(sFilename, FileMode.Create, FileAccess.Write))
			{
				this.WriteToStream(fs, bHeadersOnly);
			}
		}

		/// <summary>
		/// Save the request to a file.
		/// The headers' Request Line will not contain the scheme or host, which is probably not what you want.
		/// </summary>
		/// <param name="sFilename">The name of the file to which the request should be saved.</param>
		/// <param name="bHeadersOnly">TRUE to save only the headers</param>
		// Token: 0x06000420 RID: 1056 RVA: 0x000273E4 File Offset: 0x000255E4
		public void SaveRequest(string sFilename, bool bHeadersOnly)
		{
			this.SaveRequest(sFilename, bHeadersOnly, false);
		}

		/// <summary>
		/// Save the request to a file. Throws if file cannot be written.
		/// </summary>
		/// <param name="sFilename">The name of the file to which the request should be saved.</param>
		/// <param name="bHeadersOnly">TRUE to save only the headers.</param>
		/// <param name="bIncludeSchemeAndHostInPath">TRUE to include the Scheme and Host in the Request Line.</param>
		// Token: 0x06000421 RID: 1057 RVA: 0x000273F0 File Offset: 0x000255F0
		public void SaveRequest(string sFilename, bool bHeadersOnly, bool bIncludeSchemeAndHostInPath)
		{
			Utilities.EnsureOverwritable(sFilename);
			using (FileStream oFS = new FileStream(sFilename, FileMode.Create, FileAccess.Write))
			{
				if (this.oRequest.headers != null)
				{
					byte[] arrRequest = this.oRequest.headers.ToByteArray(true, true, bIncludeSchemeAndHostInPath, this.oFlags["X-OverrideHost"]);
					oFS.Write(arrRequest, 0, arrRequest.Length);
					if (!bHeadersOnly && this.requestBodyBytes != null)
					{
						oFS.Write(this.requestBodyBytes, 0, this.requestBodyBytes.Length);
					}
				}
			}
		}

		/// <summary>
		/// Read metadata about this session from a stream. NB: Closes the Stream when done.
		/// </summary>
		/// <param name="strmMetadata">The stream of XML text from which session metadata will be loaded.</param>
		/// <param name="skipOriginalIdComment">Specifies if the sessions without comments should ge their original Id as an auto-generated comment of not.</param>
		/// <returns>True if the Metadata was successfully loaded; False if any exceptions were trapped.</returns>
		// Token: 0x06000422 RID: 1058 RVA: 0x00027484 File Offset: 0x00025684
		public bool LoadMetadata(Stream strmMetadata, bool skipOriginalIdComment = false)
		{
			string sXMLTrue = XmlConvert.ToString(true);
			SessionFlags sfInferredFlags = SessionFlags.None;
			string sOriginalID = null;
			bool result;
			try
			{
				XmlTextReader oXML = new XmlTextReader(strmMetadata);
				oXML.WhitespaceHandling = WhitespaceHandling.None;
				while (oXML.Read())
				{
					XmlNodeType nodeType = oXML.NodeType;
					if (nodeType == XmlNodeType.Element)
					{
						string name = oXML.Name;
						if (!(name == "Session"))
						{
							if (!(name == "SessionFlag"))
							{
								if (!(name == "SessionTimers"))
								{
									if (!(name == "TunnelInfo"))
									{
										if (name == "PipeInfo")
										{
											this.bBufferResponse = sXMLTrue != oXML.GetAttribute("Streamed");
											if (!this.bBufferResponse)
											{
												sfInferredFlags |= SessionFlags.ResponseStreamed;
											}
											if (sXMLTrue == oXML.GetAttribute("CltReuse"))
											{
												sfInferredFlags |= SessionFlags.ClientPipeReused;
											}
											if (sXMLTrue == oXML.GetAttribute("Reused"))
											{
												sfInferredFlags |= SessionFlags.ServerPipeReused;
											}
											if (this.oResponse != null)
											{
												this.oResponse.m_bWasForwarded = sXMLTrue == oXML.GetAttribute("Forwarded");
												if (this.oResponse.m_bWasForwarded)
												{
													sfInferredFlags |= SessionFlags.SentToGateway;
												}
											}
										}
									}
									else
									{
										long lngBytesEgress = 0L;
										long lngBytesIngress = 0L;
										if (long.TryParse(oXML.GetAttribute("BytesEgress"), out lngBytesEgress) && long.TryParse(oXML.GetAttribute("BytesIngress"), out lngBytesIngress))
										{
											this.__oTunnel = new MockTunnel(lngBytesEgress, lngBytesIngress);
										}
									}
								}
								else
								{
									this.Timers.ClientConnected = XmlConvert.ToDateTime(oXML.GetAttribute("ClientConnected"), XmlDateTimeSerializationMode.RoundtripKind);
									string sTemp = oXML.GetAttribute("ClientBeginRequest");
									if (sTemp != null)
									{
										this.Timers.ClientBeginRequest = XmlConvert.ToDateTime(sTemp, XmlDateTimeSerializationMode.RoundtripKind);
									}
									sTemp = oXML.GetAttribute("GotRequestHeaders");
									if (sTemp != null)
									{
										this.Timers.FiddlerGotRequestHeaders = XmlConvert.ToDateTime(sTemp, XmlDateTimeSerializationMode.RoundtripKind);
									}
									this.Timers.ClientDoneRequest = XmlConvert.ToDateTime(oXML.GetAttribute("ClientDoneRequest"), XmlDateTimeSerializationMode.RoundtripKind);
									sTemp = oXML.GetAttribute("GatewayTime");
									if (sTemp != null)
									{
										this.Timers.GatewayDeterminationTime = XmlConvert.ToInt32(sTemp);
									}
									sTemp = oXML.GetAttribute("DNSTime");
									if (sTemp != null)
									{
										this.Timers.DNSTime = XmlConvert.ToInt32(sTemp);
									}
									sTemp = oXML.GetAttribute("TCPConnectTime");
									if (sTemp != null)
									{
										this.Timers.TCPConnectTime = XmlConvert.ToInt32(sTemp);
									}
									sTemp = oXML.GetAttribute("HTTPSHandshakeTime");
									if (sTemp != null)
									{
										this.Timers.HTTPSHandshakeTime = XmlConvert.ToInt32(sTemp);
									}
									sTemp = oXML.GetAttribute("ServerConnected");
									if (sTemp != null)
									{
										this.Timers.ServerConnected = XmlConvert.ToDateTime(sTemp, XmlDateTimeSerializationMode.RoundtripKind);
									}
									sTemp = oXML.GetAttribute("FiddlerBeginRequest");
									if (sTemp != null)
									{
										this.Timers.FiddlerBeginRequest = XmlConvert.ToDateTime(sTemp, XmlDateTimeSerializationMode.RoundtripKind);
									}
									this.Timers.ServerGotRequest = XmlConvert.ToDateTime(oXML.GetAttribute("ServerGotRequest"), XmlDateTimeSerializationMode.RoundtripKind);
									sTemp = oXML.GetAttribute("ServerBeginResponse");
									if (sTemp != null)
									{
										this.Timers.ServerBeginResponse = XmlConvert.ToDateTime(sTemp, XmlDateTimeSerializationMode.RoundtripKind);
									}
									sTemp = oXML.GetAttribute("GotResponseHeaders");
									if (sTemp != null)
									{
										this.Timers.FiddlerGotResponseHeaders = XmlConvert.ToDateTime(sTemp, XmlDateTimeSerializationMode.RoundtripKind);
									}
									this.Timers.ServerDoneResponse = XmlConvert.ToDateTime(oXML.GetAttribute("ServerDoneResponse"), XmlDateTimeSerializationMode.RoundtripKind);
									this.Timers.ClientBeginResponse = XmlConvert.ToDateTime(oXML.GetAttribute("ClientBeginResponse"), XmlDateTimeSerializationMode.RoundtripKind);
									this.Timers.ClientDoneResponse = XmlConvert.ToDateTime(oXML.GetAttribute("ClientDoneResponse"), XmlDateTimeSerializationMode.RoundtripKind);
								}
							}
							else
							{
								this.oFlags.Add(oXML.GetAttribute("N"), oXML.GetAttribute("V"));
							}
						}
						else
						{
							if (oXML.GetAttribute("Aborted") != null)
							{
								SessionStates oldState = this.m_state;
								this.m_state = SessionStates.Aborted;
								this.RaiseOnStateChangedIfNotIgnored(oldState, this.m_state);
							}
							if (oXML.GetAttribute("BitFlags") != null)
							{
								this.BitFlags = (SessionFlags)uint.Parse(oXML.GetAttribute("BitFlags"), NumberStyles.HexNumber);
							}
							if (oXML.GetAttribute("SID") != null)
							{
								sOriginalID = oXML.GetAttribute("SID");
							}
						}
					}
				}
				if (this.BitFlags == SessionFlags.None)
				{
					this.BitFlags = sfInferredFlags;
				}
				if (this.Timers.ClientBeginRequest.Ticks < 1L)
				{
					this.Timers.ClientBeginRequest = this.Timers.ClientConnected;
				}
				if (this.Timers.FiddlerBeginRequest.Ticks < 1L)
				{
					this.Timers.FiddlerBeginRequest = this.Timers.ServerGotRequest;
				}
				if (this.Timers.FiddlerGotRequestHeaders.Ticks < 1L)
				{
					this.Timers.FiddlerGotRequestHeaders = this.Timers.ClientBeginRequest;
				}
				if (this.Timers.FiddlerGotResponseHeaders.Ticks < 1L)
				{
					this.Timers.FiddlerGotResponseHeaders = this.Timers.ServerBeginResponse;
				}
				if (this.m_clientPort == 0 && this.oFlags.ContainsKey("X-ClientPort"))
				{
					int.TryParse(this.oFlags["X-ClientPort"], out this.m_clientPort);
				}
				int i;
				if (this.oFlags.ContainsKey("X-ProcessInfo") && int.TryParse(Utilities.TrimBefore(this.oFlags["X-ProcessInfo"], ':'), out i))
				{
					this._LocalProcessID = i;
				}
				if (sOriginalID != null)
				{
					if (CONFIG.bReloadSessionIDAsFlag || this.oFlags.ContainsKey("ui-comments"))
					{
						this.oFlags["x-OriginalSessionID"] = sOriginalID;
					}
					else if (!skipOriginalIdComment)
					{
						this.oFlags["ui-comments"] = string.Format("[#{0}]", sOriginalID);
					}
				}
				oXML.Close();
				result = true;
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogString(eX.ToString());
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Writes this session's metadata to a file.
		/// </summary>
		/// <param name="sFilename">The name of the file to which the metadata should be saved in XML format.</param>
		/// <returns>True if the file was successfully written.</returns>
		// Token: 0x06000423 RID: 1059 RVA: 0x00027A60 File Offset: 0x00025C60
		public bool SaveMetadata(string sFilename)
		{
			bool result;
			try
			{
				FileStream oFS = new FileStream(sFilename, FileMode.Create, FileAccess.Write);
				this.WriteMetadataToStream(oFS);
				oFS.Close();
				result = true;
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogString(eX.ToString());
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Saves the response (headers and body) to a file
		/// </summary>
		/// <param name="sFilename">The File to write</param>
		/// <param name="bHeadersOnly">TRUE if only heaers should be written</param>
		// Token: 0x06000424 RID: 1060 RVA: 0x00027AB0 File Offset: 0x00025CB0
		public void SaveResponse(string sFilename, bool bHeadersOnly)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(sFilename));
			FileStream oFS = new FileStream(sFilename, FileMode.Create, FileAccess.Write);
			if (this.oResponse.headers != null)
			{
				byte[] arrResponse = this.oResponse.headers.ToByteArray(true, true);
				oFS.Write(arrResponse, 0, arrResponse.Length);
				if (!bHeadersOnly && this.responseBodyBytes != null)
				{
					oFS.Write(this.responseBodyBytes, 0, this.responseBodyBytes.Length);
				}
			}
			oFS.Close();
		}

		/// <summary>
		/// Write the metadata about this Session to a stream. The Stream is left open!
		/// </summary>
		/// <param name="strmMetadata">The Stream to write to</param>
		// Token: 0x06000425 RID: 1061 RVA: 0x00027B24 File Offset: 0x00025D24
		public void WriteMetadataToStream(Stream strmMetadata)
		{
			XmlTextWriter oXML = new XmlTextWriter(strmMetadata, Encoding.UTF8);
			oXML.Formatting = Formatting.Indented;
			oXML.WriteStartDocument();
			oXML.WriteStartElement("Session");
			oXML.WriteAttributeString("SID", this.id.ToString());
			oXML.WriteAttributeString("BitFlags", ((uint)this.BitFlags).ToString("x"));
			if (this.m_state == SessionStates.Aborted)
			{
				oXML.WriteAttributeString("Aborted", XmlConvert.ToString(true));
			}
			oXML.WriteStartElement("SessionTimers");
			oXML.WriteAttributeString("ClientConnected", XmlConvert.ToString(this.Timers.ClientConnected, XmlDateTimeSerializationMode.RoundtripKind));
			oXML.WriteAttributeString("ClientBeginRequest", XmlConvert.ToString(this.Timers.ClientBeginRequest, XmlDateTimeSerializationMode.RoundtripKind));
			oXML.WriteAttributeString("GotRequestHeaders", XmlConvert.ToString(this.Timers.FiddlerGotRequestHeaders, XmlDateTimeSerializationMode.RoundtripKind));
			oXML.WriteAttributeString("ClientDoneRequest", XmlConvert.ToString(this.Timers.ClientDoneRequest, XmlDateTimeSerializationMode.RoundtripKind));
			oXML.WriteAttributeString("GatewayTime", XmlConvert.ToString(this.Timers.GatewayDeterminationTime));
			oXML.WriteAttributeString("DNSTime", XmlConvert.ToString(this.Timers.DNSTime));
			oXML.WriteAttributeString("TCPConnectTime", XmlConvert.ToString(this.Timers.TCPConnectTime));
			oXML.WriteAttributeString("HTTPSHandshakeTime", XmlConvert.ToString(this.Timers.HTTPSHandshakeTime));
			oXML.WriteAttributeString("ServerConnected", XmlConvert.ToString(this.Timers.ServerConnected, XmlDateTimeSerializationMode.RoundtripKind));
			oXML.WriteAttributeString("FiddlerBeginRequest", XmlConvert.ToString(this.Timers.FiddlerBeginRequest, XmlDateTimeSerializationMode.RoundtripKind));
			oXML.WriteAttributeString("ServerGotRequest", XmlConvert.ToString(this.Timers.ServerGotRequest, XmlDateTimeSerializationMode.RoundtripKind));
			oXML.WriteAttributeString("ServerBeginResponse", XmlConvert.ToString(this.Timers.ServerBeginResponse, XmlDateTimeSerializationMode.RoundtripKind));
			oXML.WriteAttributeString("GotResponseHeaders", XmlConvert.ToString(this.Timers.FiddlerGotResponseHeaders, XmlDateTimeSerializationMode.RoundtripKind));
			oXML.WriteAttributeString("ServerDoneResponse", XmlConvert.ToString(this.Timers.ServerDoneResponse, XmlDateTimeSerializationMode.RoundtripKind));
			oXML.WriteAttributeString("ClientBeginResponse", XmlConvert.ToString(this.Timers.ClientBeginResponse, XmlDateTimeSerializationMode.RoundtripKind));
			oXML.WriteAttributeString("ClientDoneResponse", XmlConvert.ToString(this.Timers.ClientDoneResponse, XmlDateTimeSerializationMode.RoundtripKind));
			oXML.WriteEndElement();
			oXML.WriteStartElement("PipeInfo");
			if (!this.bBufferResponse)
			{
				oXML.WriteAttributeString("Streamed", XmlConvert.ToString(true));
			}
			if (this.oRequest != null && this.oRequest.bClientSocketReused)
			{
				oXML.WriteAttributeString("CltReuse", XmlConvert.ToString(true));
			}
			if (this.oResponse != null)
			{
				if (this.oResponse.bServerSocketReused)
				{
					oXML.WriteAttributeString("Reused", XmlConvert.ToString(true));
				}
				if (this.oResponse.bWasForwarded)
				{
					oXML.WriteAttributeString("Forwarded", XmlConvert.ToString(true));
				}
			}
			oXML.WriteEndElement();
			if (this.isTunnel && this.__oTunnel != null)
			{
				oXML.WriteStartElement("TunnelInfo");
				oXML.WriteAttributeString("BytesEgress", XmlConvert.ToString(this.__oTunnel.EgressByteCount));
				oXML.WriteAttributeString("BytesIngress", XmlConvert.ToString(this.__oTunnel.IngressByteCount));
				oXML.WriteEndElement();
			}
			oXML.WriteStartElement("SessionFlags");
			foreach (object obj in this.oFlags.Keys)
			{
				string sKey = (string)obj;
				oXML.WriteStartElement("SessionFlag");
				oXML.WriteAttributeString("N", sKey);
				oXML.WriteAttributeString("V", this.oFlags[sKey]);
				oXML.WriteEndElement();
			}
			oXML.WriteEndElement();
			oXML.WriteEndElement();
			oXML.WriteEndDocument();
			oXML.Flush();
		}

		/// <summary>
		/// Write the session's Request to the specified stream 
		/// </summary>
		/// <param name="bHeadersOnly">TRUE if only the headers should be be written</param>
		/// <param name="bIncludeProtocolAndHostWithPath">TRUE if the Scheme and Host should be written in the Request Line</param>
		/// <param name="oFS">The Stream to which the request should be written</param>
		/// <returns>True if the request was written to the stream. False if the request headers do not exist. Throws on other stream errors.</returns>
		// Token: 0x06000426 RID: 1062 RVA: 0x00027F08 File Offset: 0x00026108
		public bool WriteRequestToStream(bool bHeadersOnly, bool bIncludeProtocolAndHostWithPath, Stream oFS)
		{
			return this.WriteRequestToStream(bHeadersOnly, bIncludeProtocolAndHostWithPath, false, oFS);
		}

		/// <summary>
		/// Write the session's Request to the specified stream 
		/// </summary>
		/// <param name="bHeadersOnly">TRUE if only the headers should be be written</param>
		/// <param name="bIncludeProtocolAndHostWithPath">TRUE if the Scheme and Host should be written in the Request Line</param>
		/// <param name="bEncodeIfBinary">TRUE if binary bodies should be encoded in base64 for text-safe transport (e.g. used by Composer drag/drop)</param>
		/// <param name="oFS">The Stream to which the request should be written</param>
		/// <returns>True if the request was written to the stream. False if the request headers do not exist. Throws on other stream errors.</returns>
		// Token: 0x06000427 RID: 1063 RVA: 0x00027F14 File Offset: 0x00026114
		public bool WriteRequestToStream(bool bHeadersOnly, bool bIncludeProtocolAndHostWithPath, bool bEncodeIfBinary, Stream oFS)
		{
			if (!Utilities.HasHeaders(this.oRequest))
			{
				return false;
			}
			bool bEncode = bEncodeIfBinary && !bHeadersOnly && this.requestBodyBytes != null && Utilities.arrayContainsNonText(this.requestBodyBytes);
			HTTPRequestHeaders oRH = this.oRequest.headers;
			if (bEncode)
			{
				oRH = (HTTPRequestHeaders)oRH.Clone();
				oRH["Fiddler-Encoding"] = "base64";
			}
			byte[] arrData = oRH.ToByteArray(true, true, bIncludeProtocolAndHostWithPath, this.oFlags["X-OverrideHost"]);
			oFS.Write(arrData, 0, arrData.Length);
			if (bEncode)
			{
				byte[] oEncArr = Encoding.ASCII.GetBytes(Convert.ToBase64String(this.requestBodyBytes));
				oFS.Write(oEncArr, 0, oEncArr.Length);
				return true;
			}
			if (!bHeadersOnly && !Utilities.IsNullOrEmpty(this.requestBodyBytes))
			{
				oFS.Write(this.requestBodyBytes, 0, this.requestBodyBytes.Length);
			}
			return true;
		}

		/// <summary>
		/// Write the session's Response to the specified stream
		/// </summary>
		/// <param name="oFS">The stream to which the response should be written</param>
		/// <param name="bHeadersOnly">TRUE if only the headers should be written</param>
		/// <returns>TRUE if the response was written to the stream. False if the response headers do not exist. Throws on other stream errors.</returns>
		// Token: 0x06000428 RID: 1064 RVA: 0x00027FEC File Offset: 0x000261EC
		public bool WriteResponseToStream(Stream oFS, bool bHeadersOnly)
		{
			if (!Utilities.HasHeaders(this.oResponse))
			{
				return false;
			}
			byte[] arrData = this.oResponse.headers.ToByteArray(true, true);
			oFS.Write(arrData, 0, arrData.Length);
			if (!bHeadersOnly && !Utilities.IsNullOrEmpty(this.responseBodyBytes))
			{
				oFS.Write(this.responseBodyBytes, 0, this.responseBodyBytes.Length);
			}
			return true;
		}

		// Token: 0x06000429 RID: 1065 RVA: 0x0002804C File Offset: 0x0002624C
		internal bool WriteWebSocketMessagesToStream(Stream oFS)
		{
			WebSocket oWS = this.__oTunnel as WebSocket;
			return oWS != null && oWS.WriteWebSocketMessageListToStream(oFS);
		}

		/// <summary>
		/// Write the session to the specified stream
		/// </summary>
		/// <param name="oFS">The stream to which the session should be written</param>
		/// <param name="bHeadersOnly">TRUE if only the request and response headers should be written</param>
		/// <returns>False on any exceptions; True otherwise</returns>
		// Token: 0x0600042A RID: 1066 RVA: 0x00028074 File Offset: 0x00026274
		[CodeDescription("Write the session (or session headers) to the specified stream")]
		public bool WriteToStream(Stream oFS, bool bHeadersOnly)
		{
			bool result;
			try
			{
				this.WriteRequestToStream(bHeadersOnly, true, oFS);
				oFS.WriteByte(13);
				oFS.WriteByte(10);
				this.WriteResponseToStream(oFS, bHeadersOnly);
				result = true;
			}
			catch (Exception eX)
			{
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Replace HTTP request body using the specified file.
		/// </summary>
		/// <param name="sFilename">The file containing the request</param>
		/// <returns>True if the file was successfully loaded as the request body</returns>
		// Token: 0x0600042B RID: 1067 RVA: 0x000280C0 File Offset: 0x000262C0
		[CodeDescription("Replace HTTP request headers and body using the specified file.")]
		public bool LoadRequestBodyFromFile(string sFilename)
		{
			if (!Utilities.HasHeaders(this.oRequest))
			{
				return false;
			}
			sFilename = Utilities.EnsurePathIsAbsolute(CONFIG.GetPath("Requests"), sFilename);
			return this.oRequest.ReadRequestBodyFromFile(sFilename);
		}

		// Token: 0x0600042C RID: 1068 RVA: 0x000280F0 File Offset: 0x000262F0
		private bool LoadResponse(Stream strmResponse, string sResponseFile, string sOptionalContentTypeHint)
		{
			bool bUseStream = string.IsNullOrEmpty(sResponseFile);
			this.oResponse = new ServerChatter(this, "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n");
			this.responseBodyBytes = Utilities.emptyByteArray;
			this.bBufferResponse = true;
			this.BitFlags |= SessionFlags.ResponseGeneratedByFiddler;
			this.oFlags["x-Fiddler-Generated"] = (bUseStream ? "LoadResponseFromStream" : "LoadResponseFromFile");
			bool bReturn;
			if (bUseStream)
			{
				bReturn = this.oResponse.ReadResponseFromStream(strmResponse, sOptionalContentTypeHint);
			}
			else
			{
				bReturn = this.oResponse.ReadResponseFromFile(sResponseFile, sOptionalContentTypeHint);
			}
			if (this.HTTPMethodIs("HEAD"))
			{
				this.responseBodyBytes = Utilities.emptyByteArray;
			}
			this._EnsureStateAtLeast(SessionStates.AutoTamperResponseBefore);
			return bReturn;
		}

		/// <summary>
		/// Replace HTTP response headers and body using the specified stream.
		/// </summary>
		/// <param name="strmResponse">The stream containing the response.</param>
		/// <returns>True if the Stream was successfully loaded.</returns>
		// Token: 0x0600042D RID: 1069 RVA: 0x00028199 File Offset: 0x00026399
		public bool LoadResponseFromStream(Stream strmResponse, string sOptionalContentTypeHint)
		{
			return this.LoadResponse(strmResponse, null, sOptionalContentTypeHint);
		}

		/// <summary>
		/// Replace HTTP response headers and body using the specified file.
		/// </summary>
		/// <param name="sFilename">The file containing the response.</param>
		/// <returns>True if the file was successfully loaded.</returns>
		// Token: 0x0600042E RID: 1070 RVA: 0x000281A4 File Offset: 0x000263A4
		[CodeDescription("Replace HTTP response headers and body using the specified file.")]
		public bool LoadResponseFromFile(string sFilename)
		{
			sFilename = Utilities.GetFirstLocalResponse(sFilename);
			try
			{
				FileInfo oFI = new FileInfo(sFilename);
				if (oFI.Length > (long)CONFIG._cb_STREAM_LARGE_FILES)
				{
					this.SetBitFlag(SessionFlags.ResponseGeneratedByFiddler | SessionFlags.ResponseBodyDropped, true);
					this.oFlags["x-Fiddler-Generated"] = "StreamResponseFromFile";
					this.oResponse.GenerateHeadersForLocalFile(sFilename);
					this.__sResponseFileToStream = sFilename;
					this.responseBodyBytes = Utilities.emptyByteArray;
					return true;
				}
			}
			catch (Exception eX)
			{
			}
			string sContentTypeHint = Utilities.ContentTypeForFilename(sFilename);
			return this.LoadResponse(null, sFilename, sContentTypeHint);
		}

		/// <summary>
		/// Return a string generated from the request body, decoding it and converting from a codepage if needed. Throws on errors.
		/// </summary>
		/// <returns>A string containing the request body.</returns>
		// Token: 0x0600042F RID: 1071 RVA: 0x00028238 File Offset: 0x00026438
		[CodeDescription("Return a string generated from the request body, decoding it and converting from a codepage if needed. Possibly expensive due to decompression and will throw on malformed content. Throws on errors.")]
		public string GetRequestBodyAsString()
		{
			if (!this._HasRequestBody() || !Utilities.HasHeaders(this.oRequest))
			{
				return string.Empty;
			}
			byte[] arrCopy;
			if (this.oRequest.headers.ExistsAny(new string[] { "Content-Encoding", "Transfer-Encoding" }))
			{
				arrCopy = Utilities.Dupe(this.requestBodyBytes);
				Utilities.utilDecodeHTTPBody(this.oRequest.headers, ref arrCopy);
			}
			else
			{
				arrCopy = this.requestBodyBytes;
			}
			Encoding oEncoding = Utilities.getEntityBodyEncoding(this.oRequest.headers, arrCopy);
			return Utilities.GetStringFromArrayRemovingBOM(arrCopy, oEncoding);
		}

		/// <summary>
		/// Return a string generated from the response body, decoding it and converting from a codepage if needed. Throws on errors.
		/// </summary>
		/// <returns>A string containing the response body.</returns>
		// Token: 0x06000430 RID: 1072 RVA: 0x000282C8 File Offset: 0x000264C8
		[CodeDescription("Return a string generated from the response body, decoding it and converting from a codepage if needed. Possibly expensive due to decompression and will throw on malformed content. Throws on errors.")]
		public string GetResponseBodyAsString()
		{
			if (!this._HasResponseBody() || !Utilities.HasHeaders(this.oResponse))
			{
				return string.Empty;
			}
			byte[] arrCopy;
			if (this.oResponse.headers.ExistsAny(new string[] { "Content-Encoding", "Transfer-Encoding" }))
			{
				arrCopy = Utilities.Dupe(this.responseBodyBytes);
				Utilities.utilDecodeHTTPBody(this.oResponse.headers, ref arrCopy);
			}
			else
			{
				arrCopy = this.responseBodyBytes;
			}
			Encoding oEncoding = Utilities.getEntityBodyEncoding(this.oResponse.headers, arrCopy);
			return Utilities.GetStringFromArrayRemovingBOM(arrCopy, oEncoding);
		}

		// Token: 0x06000431 RID: 1073 RVA: 0x00028358 File Offset: 0x00026558
		[CodeDescription("Return a string md5, sha1, sha256, sha384, or sha512 hash of an unchunked and decompressed copy of the response body. Throws on errors.")]
		public string GetResponseBodyHash(string sHashAlg)
		{
			if (!"md5".OICEquals(sHashAlg) && !"sha1".OICEquals(sHashAlg) && !"sha256".OICEquals(sHashAlg) && !"sha384".OICEquals(sHashAlg) && !"sha512".OICEquals(sHashAlg))
			{
				throw new NotImplementedException("Hash algorithm " + sHashAlg + " is not implemented");
			}
			if (!this._HasResponseBody() || !Utilities.HasHeaders(this.oResponse))
			{
				return string.Empty;
			}
			byte[] arrCopy = Utilities.Dupe(this.responseBodyBytes);
			Utilities.utilDecodeHTTPBody(this.oResponse.headers, ref arrCopy);
			if (sHashAlg.OICEquals("sha256"))
			{
				return Utilities.GetSHA256Hash(arrCopy);
			}
			if (sHashAlg.OICEquals("sha1"))
			{
				return Utilities.GetSHA1Hash(arrCopy);
			}
			if (sHashAlg.OICEquals("sha512"))
			{
				return Utilities.GetSHA512Hash(arrCopy);
			}
			if (sHashAlg.OICEquals("sha384"))
			{
				return Utilities.GetSHA384Hash(arrCopy);
			}
			if (sHashAlg.OICEquals("md5"))
			{
				return Utilities.GetMD5Hash(arrCopy);
			}
			throw new Exception("Unknown failure");
		}

		// Token: 0x06000432 RID: 1074 RVA: 0x00028464 File Offset: 0x00026664
		[CodeDescription("Return a base64 string md5, sha1, sha256, sha384, or sha512 hash of an unchunked and decompressed copy of the response body. Throws on errors.")]
		public string GetResponseBodyHashAsBase64(string sHashAlgorithm)
		{
			if (!this._HasResponseBody() || !Utilities.HasHeaders(this.oResponse))
			{
				return string.Empty;
			}
			byte[] arrCopy = Utilities.Dupe(this.responseBodyBytes);
			Utilities.utilDecodeHTTPBody(this.oResponse.headers, ref arrCopy);
			return Utilities.GetHashAsBase64(sHashAlgorithm, arrCopy);
		}

		/// <summary>
		/// Find the text encoding of the request
		/// WARNING: Will not decompress body to scan for indications of the character set
		/// </summary>
		/// <returns>Returns the Encoding of the requestBodyBytes</returns>
		// Token: 0x06000433 RID: 1075 RVA: 0x000284B1 File Offset: 0x000266B1
		[CodeDescription("Returns the Encoding of the requestBodyBytes")]
		public Encoding GetRequestBodyEncoding()
		{
			return Utilities.getEntityBodyEncoding(this.oRequest.headers, this.requestBodyBytes);
		}

		/// <summary>
		/// Find the text encoding of the response
		/// WARNING: Will not decompress body to scan for indications of the character set
		/// </summary>
		/// <returns>The Encoding of the responseBodyBytes</returns>
		// Token: 0x06000434 RID: 1076 RVA: 0x000284C9 File Offset: 0x000266C9
		[CodeDescription("Returns the Encoding of the responseBodyBytes")]
		public Encoding GetResponseBodyEncoding()
		{
			return Utilities.getResponseBodyEncoding(this);
		}

		/// <summary>
		/// Returns true if the absolute request URI contains the specified string. Case-insensitive.
		/// </summary>
		/// <param name="sLookfor">Case-insensitive string to find</param>
		/// <returns>TRUE if the URI contains the string</returns>
		// Token: 0x06000435 RID: 1077 RVA: 0x000284D1 File Offset: 0x000266D1
		[CodeDescription("Returns true if request URI contains the specified string. Case-insensitive.")]
		public bool uriContains(string sLookfor)
		{
			return this.fullUrl.OICContains(sLookfor);
		}

		/// <summary>
		/// Removes chunking and HTTP Compression from the Response. Adds or updates Content-Length header.
		/// </summary>
		/// <returns>Returns TRUE if the response was decoded; returns FALSE on failure, or if response didn't have headers that showed encoding.</returns>
		// Token: 0x06000436 RID: 1078 RVA: 0x000284DF File Offset: 0x000266DF
		[CodeDescription("Removes chunking and HTTP Compression from the response. Adds or updates Content-Length header.")]
		public bool utilDecodeResponse()
		{
			return this.utilDecodeResponse(false);
		}

		/// <summary>
		/// Removes chunking and HTTP Compression from the Response. Adds or updates Content-Length header.
		/// </summary>
		/// <param name="bSilent">TRUE if error messages should be suppressed. False otherwise.</param>
		/// <returns>TRUE if the decoding was successsful.</returns>
		// Token: 0x06000437 RID: 1079 RVA: 0x000284E8 File Offset: 0x000266E8
		public bool utilDecodeResponse(bool bSilent)
		{
			if (!Utilities.HasHeaders(this.oResponse) || (!this.oResponse.headers.Exists("Transfer-Encoding") && !this.oResponse.headers.Exists("Content-Encoding")))
			{
				return false;
			}
			try
			{
				Utilities.utilTryDecode(this.oResponse.headers, ref this.responseBodyBytes, bSilent);
			}
			catch (Exception eX)
			{
				if (CONFIG.bDebugSpew)
				{
					FiddlerApplication.DebugSpew("utilDecodeResponse failed. The HTTP response body was malformed. " + Utilities.DescribeException(eX));
				}
				if (!bSilent)
				{
					string title = "utilDecodeResponse failed for Session #" + this.id.ToString();
					string message = "The HTTP response body was malformed.";
					FiddlerApplication.Log.LogFormat("{0}: {1}" + Environment.NewLine + "{2}", new object[]
					{
						title,
						message,
						eX.ToString()
					});
				}
				this.oFlags["x-UtilDecodeResponse"] = Utilities.DescribeException(eX);
				this.oFlags["ui-backcolor"] = "LightYellow";
				return false;
			}
			return true;
		}

		/// <summary>
		/// Removes chunking and HTTP Compression from the Request. Adds or updates Content-Length header.
		/// </summary>
		/// <returns>Returns TRUE if the request was decoded; returns FALSE on failure, or if request didn't have headers that showed encoding.</returns>
		// Token: 0x06000438 RID: 1080 RVA: 0x00028608 File Offset: 0x00026808
		[CodeDescription("Removes chunking and HTTP Compression from the Request. Adds or updates Content-Length header.")]
		public bool utilDecodeRequest()
		{
			return this.utilDecodeRequest(false);
		}

		// Token: 0x06000439 RID: 1081 RVA: 0x00028614 File Offset: 0x00026814
		public bool utilDecodeRequest(bool bSilent)
		{
			if (!Utilities.HasHeaders(this.oRequest) || (!this.oRequest.headers.Exists("Transfer-Encoding") && !this.oRequest.headers.Exists("Content-Encoding")))
			{
				return false;
			}
			try
			{
				Utilities.utilTryDecode(this.oRequest.headers, ref this.requestBodyBytes, bSilent);
			}
			catch (Exception eX)
			{
				if (!bSilent)
				{
					string title = "utilDecodeResponse failed for Session #" + this.id.ToString();
					string message = "The HTTP request body was malformed.";
					FiddlerApplication.Log.LogFormat("{0}: {1}" + Environment.NewLine + "{2}", new object[]
					{
						title,
						message,
						eX.ToString()
					});
				}
				this.oFlags["x-UtilDecodeRequest"] = Utilities.DescribeException(eX);
				this.oFlags["ui-backcolor"] = "LightYellow";
				return false;
			}
			return true;
		}

		/// <summary>
		/// Use GZIP to compress the request body. Throws exceptions to caller.
		/// </summary>
		/// <returns>TRUE if compression succeeded</returns>
		// Token: 0x0600043A RID: 1082 RVA: 0x00028718 File Offset: 0x00026918
		[CodeDescription("Use GZIP to compress the request body. Throws exceptions to caller.")]
		public bool utilGZIPRequest()
		{
			if (!this._mayCompressRequest())
			{
				return false;
			}
			this.requestBodyBytes = Utilities.GzipCompress(this.requestBodyBytes);
			this.oRequest.headers["Content-Encoding"] = "gzip";
			this.oRequest.headers["Content-Length"] = ((this.requestBodyBytes == null) ? "0" : ((long)this.requestBodyBytes.Length).ToString());
			return true;
		}

		/// <summary>
		/// Use GZIP to compress the response body. Throws exceptions to caller.
		/// </summary>
		/// <returns>TRUE if compression succeeded</returns>
		// Token: 0x0600043B RID: 1083 RVA: 0x00028790 File Offset: 0x00026990
		[CodeDescription("Use GZIP to compress the response body. Throws exceptions to caller.")]
		public bool utilGZIPResponse()
		{
			if (!this._mayCompressResponse())
			{
				return false;
			}
			this.responseBodyBytes = Utilities.GzipCompress(this.responseBodyBytes);
			this.oResponse.headers["Content-Encoding"] = "gzip";
			this.oResponse.headers["Content-Length"] = ((this.responseBodyBytes == null) ? "0" : ((long)this.responseBodyBytes.Length).ToString());
			return true;
		}

		/// <summary>
		/// Use DEFLATE to compress the response body. Throws exceptions to caller.
		/// </summary>
		/// <returns>TRUE if compression succeeded</returns>
		// Token: 0x0600043C RID: 1084 RVA: 0x00028808 File Offset: 0x00026A08
		[CodeDescription("Use DEFLATE to compress the response body. Throws exceptions to caller.")]
		public bool utilDeflateResponse()
		{
			if (!this._mayCompressResponse())
			{
				return false;
			}
			this.responseBodyBytes = Utilities.DeflaterCompress(this.responseBodyBytes);
			this.oResponse.headers["Content-Encoding"] = "deflate";
			this.oResponse.headers["Content-Length"] = ((this.responseBodyBytes == null) ? "0" : ((long)this.responseBodyBytes.Length).ToString());
			return true;
		}

		/// <summary>
		/// Use BZIP2 to compress the response body. Throws exceptions to caller.
		/// </summary>
		/// <returns>TRUE if compression succeeded</returns>
		// Token: 0x0600043D RID: 1085 RVA: 0x00028880 File Offset: 0x00026A80
		[CodeDescription("Use BZIP2 to compress the response body. Throws exceptions to caller.")]
		public bool utilBZIP2Response()
		{
			if (!this._mayCompressResponse())
			{
				return false;
			}
			this.responseBodyBytes = Utilities.bzip2Compress(this.responseBodyBytes);
			this.oResponse.headers["Content-Encoding"] = "bzip2";
			this.oResponse.headers["Content-Length"] = ((this.responseBodyBytes == null) ? "0" : ((long)this.responseBodyBytes.Length).ToString());
			return true;
		}

		// Token: 0x0600043E RID: 1086 RVA: 0x000288F7 File Offset: 0x00026AF7
		private bool _mayCompressRequest()
		{
			return this._HasRequestBody() && !this.oRequest.headers.Exists("Content-Encoding") && !this.oRequest.headers.Exists("Transfer-Encoding");
		}

		// Token: 0x0600043F RID: 1087 RVA: 0x00028932 File Offset: 0x00026B32
		private bool _mayCompressResponse()
		{
			return this._HasResponseBody() && !this.oResponse.headers.Exists("Content-Encoding") && !this.oResponse.headers.Exists("Transfer-Encoding");
		}

		/// <summary>
		/// Introduces HTTP Chunked encoding on the response body
		/// </summary>
		/// <param name="iSuggestedChunkCount">The number of chunks to try to create</param>
		/// <returns>TRUE if the chunking could be performed.</returns>
		// Token: 0x06000440 RID: 1088 RVA: 0x00028970 File Offset: 0x00026B70
		[CodeDescription("Apply Transfer-Encoding: chunked to the response, if possible.")]
		public bool utilChunkResponse(int iSuggestedChunkCount)
		{
			if (!Utilities.HasHeaders(this.oRequest) || !"HTTP/1.1".OICEquals(this.oRequest.headers.HTTPVersion) || this.HTTPMethodIs("HEAD") || this.HTTPMethodIs("CONNECT") || !Utilities.HasHeaders(this.oResponse) || !Utilities.HTTPStatusAllowsBody(this.oResponse.headers.HTTPResponseCode) || (this.responseBodyBytes != null && (long)this.responseBodyBytes.Length > 2147483647L) || this.oResponse.headers.Exists("Transfer-Encoding"))
			{
				return false;
			}
			this.responseBodyBytes = Utilities.doChunk(this.responseBodyBytes, iSuggestedChunkCount);
			this.oResponse.headers.Remove("Content-Length");
			this.oResponse.headers["Transfer-Encoding"] = "chunked";
			return true;
		}

		/// <summary>
		/// Perform a string replacement on the request body. Adjusts the Content-Length header if needed.
		/// </summary>
		/// <param name="sSearchFor">The case-sensitive string to search for.</param>
		/// <param name="sReplaceWith">The text to replace.</param>
		/// <returns>TRUE if one or more replacements occurred.</returns>
		// Token: 0x06000441 RID: 1089 RVA: 0x00028A5C File Offset: 0x00026C5C
		[CodeDescription("Perform a case-sensitive string replacement on the request body (not URL!). Updates Content-Length header. Returns TRUE if replacements occur.")]
		public bool utilReplaceInRequest(string sSearchFor, string sReplaceWith)
		{
			if (!this._HasRequestBody() || !Utilities.HasHeaders(this.oRequest))
			{
				return false;
			}
			string sBody = this.GetRequestBodyAsString();
			string sNewBody = sBody.Replace(sSearchFor, sReplaceWith);
			if (sBody != sNewBody)
			{
				this.utilSetRequestBody(sNewBody);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Call inside OnBeforeRequest to create a response object and bypass the server.
		/// </summary>
		// Token: 0x06000442 RID: 1090 RVA: 0x00028AA4 File Offset: 0x00026CA4
		[CodeDescription("Call inside OnBeforeRequest to create a Response object and bypass the server.")]
		public void utilCreateResponseAndBypassServer()
		{
			if (this.state > SessionStates.SendingRequest)
			{
				throw new InvalidOperationException("Too late, we're already talking to the server.");
			}
			if (this.isFlagSet(SessionFlags.RequestStreamed))
			{
				this.oResponse.StreamRequestBody();
			}
			this.oResponse = new ServerChatter(this, "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n");
			this.responseBodyBytes = Utilities.emptyByteArray;
			this.oFlags["x-Fiddler-Generated"] = "utilCreateResponseAndBypassServer";
			this.BitFlags |= SessionFlags.ResponseGeneratedByFiddler;
			this.bBufferResponse = true;
			this.state = SessionStates.AutoTamperResponseBefore;
		}

		// Token: 0x06000443 RID: 1091 RVA: 0x00028B2C File Offset: 0x00026D2C
		[CodeDescription("Copy an existing Session's response to this Session, bypassing the server if not already contacted")]
		public void utilAssignResponse(Session oFromSession)
		{
			this.utilAssignResponse(oFromSession.oResponse.headers, oFromSession.responseBodyBytes);
		}

		// Token: 0x06000444 RID: 1092 RVA: 0x00028B48 File Offset: 0x00026D48
		[CodeDescription("Copy an existing response to this Session, bypassing the server if not already contacted")]
		public void utilAssignResponse(HTTPResponseHeaders oRH, byte[] arrBody)
		{
			if (this.oResponse == null)
			{
				this.oResponse = new ServerChatter(this, "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n");
			}
			if (oRH == null)
			{
				this.oResponse.headers = new HTTPResponseHeaders();
				this.oResponse.headers.SetStatus(200, "Fiddler-Generated");
			}
			else
			{
				this.oResponse.headers = (HTTPResponseHeaders)oRH.Clone();
			}
			this.responseBodyBytes = arrBody ?? Utilities.emptyByteArray;
			this.oFlags["x-Fiddler-Generated"] = "utilAssignResponse";
			this.BitFlags |= SessionFlags.ResponseGeneratedByFiddler;
			this.bBufferResponse = true;
			this.state = SessionStates.AutoTamperResponseBefore;
		}

		/// <summary>
		/// Perform a regex-based string replacement on the response body. Adjusts the Content-Length header if needed. 
		/// </summary>
		/// <param name="sSearchForRegEx">The regular expression used to search the body. Specify RegEx Options via leading Inline Flags, e.g. (?im) for case-Insensitive Multi-line.</param>
		/// <param name="sReplaceWithExpression">The text or expression used to replace</param>
		/// <returns>TRUE if replacements occured</returns>
		// Token: 0x06000445 RID: 1093 RVA: 0x00028BF8 File Offset: 0x00026DF8
		[CodeDescription("Perform a regex-based replacement on the response body. Specify RegEx Options via leading Inline Flags, e.g. (?im) for case-Insensitive Multi-line. Updates Content-Length header. Note, you should call utilDecodeResponse first!  Returns TRUE if replacements occur.")]
		public bool utilReplaceRegexInResponse(string sSearchForRegEx, string sReplaceWithExpression)
		{
			if (!this._HasResponseBody())
			{
				return false;
			}
			Encoding oEncoding = Utilities.getResponseBodyEncoding(this);
			string sArray = oEncoding.GetString(this.responseBodyBytes);
			string sArray2 = Regex.Replace(sArray, sSearchForRegEx, sReplaceWithExpression, RegexOptions.ExplicitCapture | RegexOptions.Singleline);
			if (sArray != sArray2)
			{
				this.responseBodyBytes = oEncoding.GetBytes(sArray2);
				this.oResponse["Content-Length"] = ((long)this.responseBodyBytes.Length).ToString();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Perform a string replacement on the response body (potentially multiple times). Adjust the Content-Length header if needed. 
		/// </summary>
		/// <param name="sSearchFor">String to find (case-sensitive)</param>
		/// <param name="sReplaceWith">String to use to replace</param>
		/// <returns>TRUE if replacements occurred</returns>
		// Token: 0x06000446 RID: 1094 RVA: 0x00028C67 File Offset: 0x00026E67
		[CodeDescription("Perform a case-sensitive string replacement on the response body. Updates Content-Length header. Note, you should call utilDecodeResponse first!  Returns TRUE if replacements occur.")]
		public bool utilReplaceInResponse(string sSearchFor, string sReplaceWith)
		{
			return this._innerReplaceInResponse(sSearchFor, sReplaceWith, true, true);
		}

		/// <summary>
		/// Perform a one-time string replacement on the response body. Adjust the Content-Length header if needed. 
		/// </summary>
		/// <param name="sSearchFor">String to find (case-sensitive)</param>
		/// <param name="sReplaceWith">String to use to replace</param>
		/// <param name="bCaseSensitive">TRUE for Case-Sensitive</param>
		/// <returns>TRUE if a replacement occurred</returns>
		// Token: 0x06000447 RID: 1095 RVA: 0x00028C73 File Offset: 0x00026E73
		[CodeDescription("Perform a single case-sensitive string replacement on the response body. Updates Content-Length header. Note, you should call utilDecodeResponse first! Returns TRUE if replacements occur.")]
		public bool utilReplaceOnceInResponse(string sSearchFor, string sReplaceWith, bool bCaseSensitive)
		{
			return this._innerReplaceInResponse(sSearchFor, sReplaceWith, false, bCaseSensitive);
		}

		// Token: 0x06000448 RID: 1096 RVA: 0x00028C80 File Offset: 0x00026E80
		private bool _innerReplaceInResponse(string sSearchFor, string sReplaceWith, bool bReplaceAll, bool bCaseSensitive)
		{
			if (!this._HasResponseBody())
			{
				return false;
			}
			Encoding oEncoding = Utilities.getResponseBodyEncoding(this);
			string sArray = oEncoding.GetString(this.responseBodyBytes);
			string sArray2;
			if (bReplaceAll)
			{
				sArray2 = sArray.Replace(sSearchFor, sReplaceWith);
			}
			else
			{
				int iX = sArray.IndexOf(sSearchFor, bCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
				if (iX == 0)
				{
					sArray2 = sReplaceWith + sArray.Substring(sSearchFor.Length);
				}
				else
				{
					if (iX <= 0)
					{
						return false;
					}
					sArray2 = sArray.Substring(0, iX) + sReplaceWith + sArray.Substring(iX + sSearchFor.Length);
				}
			}
			if (sArray != sArray2)
			{
				this.responseBodyBytes = oEncoding.GetBytes(sArray2);
				this.oResponse["Content-Length"] = ((long)this.responseBodyBytes.Length).ToString();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Replaces the request body with sString. Sets Content-Length header and removes Transfer-Encoding/Content-Encoding.
		/// </summary>
		/// <param name="sString">The desired request Body as a string</param>
		// Token: 0x06000449 RID: 1097 RVA: 0x00028D40 File Offset: 0x00026F40
		[CodeDescription("Replaces the request body with sString. Sets Content-Length header & removes Transfer-Encoding/Content-Encoding")]
		public void utilSetRequestBody(string sString)
		{
			if (sString == null)
			{
				sString = string.Empty;
			}
			this.oRequest.headers.Remove("Transfer-Encoding");
			this.oRequest.headers.Remove("Content-Encoding");
			Encoding oEnc = Utilities.getEntityBodyEncoding(this.oRequest.headers, null);
			this.requestBodyBytes = oEnc.GetBytes(sString);
			this.oRequest["Content-Length"] = ((long)this.requestBodyBytes.Length).ToString();
		}

		/// <summary>
		/// Replaces the response body with sString. Sets Content-Length header and removes Transfer-Encoding/Content-Encoding
		/// </summary>
		/// <param name="sString">The desired response Body as a string</param>
		// Token: 0x0600044A RID: 1098 RVA: 0x00028DC0 File Offset: 0x00026FC0
		[CodeDescription("Replaces the response body with sString. Sets Content-Length header & removes Transfer-Encoding/Content-Encoding")]
		public void utilSetResponseBody(string sString)
		{
			if (sString == null)
			{
				sString = string.Empty;
			}
			this.oResponse.headers.Remove("Transfer-Encoding");
			this.oResponse.headers.Remove("Content-Encoding");
			Encoding oEnc = Utilities.getResponseBodyEncoding(this);
			this.responseBodyBytes = oEnc.GetBytes(sString);
			this.oResponse["Content-Length"] = ((long)this.responseBodyBytes.Length).ToString();
		}

		/// <summary>
		/// Add a string to the top of the response body, updating Content-Length. (Call utilDecodeResponse first!)
		/// </summary>
		/// <param name="sString">The string to prepend</param>
		// Token: 0x0600044B RID: 1099 RVA: 0x00028E38 File Offset: 0x00027038
		[CodeDescription("Prepend a string to the response body. Updates Content-Length header. Note, you should call utilDecodeResponse first!")]
		public void utilPrependToResponseBody(string sString)
		{
			if (this.responseBodyBytes == null)
			{
				this.responseBodyBytes = Utilities.emptyByteArray;
			}
			Encoding oEnc = Utilities.getResponseBodyEncoding(this);
			this.responseBodyBytes = Utilities.JoinByteArrays(oEnc.GetBytes(sString), this.responseBodyBytes);
			this.oResponse.headers["Content-Length"] = ((long)this.responseBodyBytes.Length).ToString();
		}

		/// <summary>
		/// Find a string in the request body. Return its index, or -1.
		/// </summary>
		/// <param name="sSearchFor">Term to search for</param>
		/// <param name="bCaseSensitive">Require case-sensitive match?</param>
		/// <returns>Location of sSearchFor,or -1</returns>
		// Token: 0x0600044C RID: 1100 RVA: 0x00028E9C File Offset: 0x0002709C
		[CodeDescription("Find a string in the request body. Return its index or -1.")]
		public int utilFindInRequest(string sSearchFor, bool bCaseSensitive)
		{
			if (!this._HasRequestBody())
			{
				return -1;
			}
			string sBody = Utilities.getEntityBodyEncoding(this.oRequest.headers, this.requestBodyBytes).GetString(this.requestBodyBytes);
			return sBody.IndexOf(sSearchFor, bCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
		}

		// Token: 0x0600044D RID: 1101 RVA: 0x00028EE3 File Offset: 0x000270E3
		private bool _HasRequestBody()
		{
			return !Utilities.IsNullOrEmpty(this.requestBodyBytes);
		}

		// Token: 0x0600044E RID: 1102 RVA: 0x00028EF3 File Offset: 0x000270F3
		private bool _HasResponseBody()
		{
			return !Utilities.IsNullOrEmpty(this.responseBodyBytes);
		}

		/// <summary>
		/// Find a string in the response body. Return its index, or -1.
		/// </summary>
		/// <param name="sSearchFor">Term to search for</param>
		/// <param name="bCaseSensitive">Require case-sensitive match?</param>
		/// <returns>Location of sSearchFor,or -1</returns>
		// Token: 0x0600044F RID: 1103 RVA: 0x00028F04 File Offset: 0x00027104
		[CodeDescription("Find a string in the response body. Return its index or -1. Note, you should call utilDecodeResponse first!")]
		public int utilFindInResponse(string sSearchFor, bool bCaseSensitive)
		{
			if (!this._HasResponseBody())
			{
				return -1;
			}
			string sBody = Utilities.getResponseBodyEncoding(this).GetString(this.responseBodyBytes);
			return sBody.IndexOf(sSearchFor, bCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Reset the SessionID counter to 0. This method can lead to confusing UI, so use sparingly.
		/// </summary>
		// Token: 0x06000450 RID: 1104 RVA: 0x00028F3B File Offset: 0x0002713B
		[CodeDescription("Reset the SessionID counter to 0. This method can lead to confusing UI, so use sparingly.")]
		internal static void ResetSessionCounter()
		{
			Session.OnBeforeSessionCounterReset();
			Interlocked.Exchange(ref Session.cRequests, 0);
		}

		// Token: 0x14000017 RID: 23
		// (add) Token: 0x06000451 RID: 1105 RVA: 0x00028F4E File Offset: 0x0002714E
		// (remove) Token: 0x06000452 RID: 1106 RVA: 0x00028F65 File Offset: 0x00027165
		internal static event EventHandler BeforeSessionCounterReset
		{
			[DoNotObfuscate]
			add
			{
				Session.beforeSessionCounterReset = (EventHandler)Delegate.Combine(Session.beforeSessionCounterReset, value);
			}
			[DoNotObfuscate]
			remove
			{
				Session.beforeSessionCounterReset = (EventHandler)Delegate.Remove(Session.beforeSessionCounterReset, value);
			}
		}

		// Token: 0x06000453 RID: 1107 RVA: 0x00028F7C File Offset: 0x0002717C
		private static void OnBeforeSessionCounterReset()
		{
			EventHandler handler = Session.beforeSessionCounterReset;
			if (handler != null)
			{
				handler(null, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Create a Session object from two byte[] representing request and response.
		/// </summary>
		/// <param name="arrRequest">The client data bytes</param>
		/// <param name="arrResponse">The server data bytes</param>
		/// <param name="skipNewSessionEvent">Specifies if the Session.SessionCreated event should be raised of not.</param>
		// Token: 0x06000454 RID: 1108 RVA: 0x00028FA0 File Offset: 0x000271A0
		public Session(byte[] arrRequest, byte[] arrResponse, bool skipNewSessionEvent = false)
		{
			this.ConstructSession(arrRequest, arrResponse, SessionFlags.None);
			if (!skipNewSessionEvent)
			{
				this.RaiseSessionCreated();
			}
		}

		/// <summary>
		/// Create a Session object from a (serializable) SessionData object
		/// </summary>
		/// <param name="oSD"></param>
		// Token: 0x06000455 RID: 1109 RVA: 0x00028FF8 File Offset: 0x000271F8
		public Session(SessionData oSD)
		{
			this.ConstructSession(oSD.arrRequest, oSD.arrResponse, SessionFlags.None);
			this.LoadMetadata(new MemoryStream(oSD.arrMetadata), false);
			if (oSD.arrWebSocketMessages != null && oSD.arrWebSocketMessages.Length != 0)
			{
				WebSocket.LoadWebSocketMessagesFromStream(this, new MemoryStream(oSD.arrWebSocketMessages));
			}
			this.RaiseSessionCreated();
		}

		/// <summary>
		/// Create a Session object from two byte[] representing request and response. This is used when loading a Session Archive Zip.
		/// </summary>
		/// <param name="arrRequest">The client data bytes</param>
		/// <param name="arrResponse">The server data bytes</param>
		/// <param name="oSF">SessionFlags for this session</param>
		/// <param name="skipNewSessionEvent">Specifies if the Session.SessionCreated event should be raised of not.</param>
		// Token: 0x06000456 RID: 1110 RVA: 0x0002908C File Offset: 0x0002728C
		public Session(byte[] arrRequest, byte[] arrResponse, SessionFlags oSF, bool skipNewSessionEvent)
		{
			this.ConstructSession(arrRequest, arrResponse, oSF);
			if (!skipNewSessionEvent)
			{
				this.RaiseSessionCreated();
			}
		}

		// Token: 0x06000457 RID: 1111 RVA: 0x000290E8 File Offset: 0x000272E8
		private void ConstructSession(byte[] arrRequest, byte[] arrResponse, SessionFlags oSF)
		{
			if (Utilities.IsNullOrEmpty(arrRequest))
			{
				arrRequest = Encoding.ASCII.GetBytes("GET http://MISSING-REQUEST/? HTTP/0.0\r\nHost:MISSING-REQUEST\r\nX-Fiddler-Generated: Request Data was missing\r\n\r\n");
			}
			if (Utilities.IsNullOrEmpty(arrResponse))
			{
				arrResponse = Encoding.ASCII.GetBytes("HTTP/1.1 0 FIDDLER GENERATED - RESPONSE DATA WAS MISSING\r\n\r\n");
			}
			this.state = SessionStates.Done;
			this.m_requestID = Interlocked.Increment(ref Session.cRequests);
			this.BitFlags = oSF;
			int iRequestHeadersLen;
			int iRequestEntityOffset;
			HTTPHeaderParseWarnings hpwDontCare;
			if (!Parser.FindEntityBodyOffsetFromArray(arrRequest, out iRequestHeadersLen, out iRequestEntityOffset, out hpwDontCare))
			{
				throw new InvalidDataException("Request corrupt, unable to find end of headers.");
			}
			int iResponseHeadersLen;
			int iResponseEntityOffset;
			if (!Parser.FindEntityBodyOffsetFromArray(arrResponse, out iResponseHeadersLen, out iResponseEntityOffset, out hpwDontCare))
			{
				throw new InvalidDataException("Response corrupt, unable to find end of headers.");
			}
			this.requestBodyBytes = new byte[arrRequest.Length - iRequestEntityOffset];
			this.responseBodyBytes = new byte[arrResponse.Length - iResponseEntityOffset];
			Buffer.BlockCopy(arrRequest, iRequestEntityOffset, this.requestBodyBytes, 0, this.requestBodyBytes.Length);
			Buffer.BlockCopy(arrResponse, iResponseEntityOffset, this.responseBodyBytes, 0, this.responseBodyBytes.Length);
			string sRequestHeaders = CONFIG.oHeaderEncoding.GetString(arrRequest, 0, iRequestHeadersLen) + "\r\n\r\n";
			string sResponseHeaders = CONFIG.oHeaderEncoding.GetString(arrResponse, 0, iResponseHeadersLen) + "\r\n\r\n";
			this.oRequest = new ClientChatter(this, sRequestHeaders);
			this.oResponse = new ServerChatter(this, sResponseHeaders);
		}

		/// <summary>
		/// Creates a new session and attaches it to the pipes passed as arguments
		/// </summary>
		/// <param name="clientPipe">The client pipe from which the request is read and to which the response is written.</param>
		/// <param name="serverPipe">The server pipe to which the request is sent and from which the response is read. May be null.</param>
		// Token: 0x06000458 RID: 1112 RVA: 0x00029214 File Offset: 0x00027414
		internal Session(ClientPipe clientPipe, ServerPipe serverPipe)
		{
			if (CONFIG.bDebugSpew)
			{
				this.OnStateChanged += delegate(object s, StateChangeEventArgs ea)
				{
					FiddlerApplication.DebugSpew("onstatechange>#{0} moving from state '{1}' to '{2}' {3}", new object[]
					{
						this.id.ToString(),
						ea.oldState,
						ea.newState,
						Environment.StackTrace
					});
				};
			}
			if (clientPipe != null)
			{
				this.Timers.ClientConnected = clientPipe.dtAccepted;
				this.m_clientIP = ((clientPipe.Address == null) ? null : clientPipe.Address.ToString());
				this.m_clientPort = clientPipe.Port;
				this.oFlags["x-clientIP"] = this.m_clientIP;
				this.oFlags["x-clientport"] = this.m_clientPort.ToString();
				if (clientPipe.LocalProcessID != 0)
				{
					this._LocalProcessID = clientPipe.LocalProcessID;
					string sessionProcessInfo = string.Format("{0}:{1}", clientPipe.LocalProcessName, this._LocalProcessID);
					this.SetDecryptFlagIfSessionIsFromInstrumentedBrowserProcess(sessionProcessInfo);
					this.oFlags["x-ProcessInfo"] = sessionProcessInfo;
				}
			}
			else
			{
				this.Timers.ClientConnected = DateTime.Now;
			}
			this.oResponse = new ServerChatter(this);
			this.oRequest = new ClientChatter(this);
			this.oRequest.pipeClient = clientPipe;
			this.oResponse.pipeServer = serverPipe;
			this.RaiseSessionCreated();
		}

		/// <summary>
		/// Initialize a new session from a given request headers and body request builder data. Note: No Session ID is assigned here.
		/// </summary>
		/// <param name="oRequestHeaders">NB: If you're copying an existing request, use oRequestHeaders.Clone()</param>
		/// <param name="arrRequestBody">The bytes of the request's body</param>
		// Token: 0x06000459 RID: 1113 RVA: 0x00029374 File Offset: 0x00027574
		public Session(HTTPRequestHeaders oRequestHeaders, byte[] arrRequestBody, bool skipNewSessionEvent = false)
		{
			this.ConstructSession(oRequestHeaders, arrRequestBody);
			if (!skipNewSessionEvent)
			{
				this.RaiseSessionCreated();
			}
		}

		// Token: 0x0600045A RID: 1114 RVA: 0x000293CC File Offset: 0x000275CC
		private void ConstructSession(HTTPRequestHeaders oRequestHeaders, byte[] arrRequestBody)
		{
			if (oRequestHeaders == null)
			{
				throw new ArgumentNullException("oRequestHeaders", "oRequestHeaders must not be null when creating a new Session.");
			}
			if (arrRequestBody == null)
			{
				arrRequestBody = Utilities.emptyByteArray;
			}
			if (CONFIG.bDebugSpew)
			{
				this.OnStateChanged += delegate(object s, StateChangeEventArgs ea)
				{
					FiddlerApplication.DebugSpew("onstatechange>#{0} moving from state '{1}' to '{2}' {3}", new object[]
					{
						this.id.ToString(),
						ea.oldState,
						ea.newState,
						Environment.StackTrace
					});
				};
			}
			this.Timers.ClientConnected = (this.Timers.ClientBeginRequest = (this.Timers.FiddlerGotRequestHeaders = DateTime.Now));
			this.m_clientIP = null;
			this.m_clientPort = 0;
			this.oFlags["x-clientIP"] = this.m_clientIP;
			this.oFlags["x-clientport"] = this.m_clientPort.ToString();
			this.oResponse = new ServerChatter(this);
			this.oRequest = new ClientChatter(this);
			this.oRequest.pipeClient = null;
			this.oResponse.pipeServer = null;
			this.oRequest.headers = oRequestHeaders;
			this.requestBodyBytes = arrRequestBody;
			this.m_state = SessionStates.AutoTamperRequestBefore;
		}

		/// <summary>
		/// Copy Constructor. <seealso cref="M:Fiddler.Session.BuildFromData(System.Boolean,Fiddler.HTTPRequestHeaders,System.Byte[],Fiddler.HTTPResponseHeaders,System.Byte[],Fiddler.SessionFlags)" />.
		/// </summary>
		/// <param name="toDeepCopy">Session to clone into a new Session instance</param>
		// Token: 0x0600045B RID: 1115 RVA: 0x000294C4 File Offset: 0x000276C4
		public Session(Session toDeepCopy)
		{
			this.ConstructSession((HTTPRequestHeaders)toDeepCopy.RequestHeaders.Clone(), Utilities.Dupe(toDeepCopy.requestBodyBytes));
			this._AssignID();
			this.SetBitFlag(toDeepCopy._bitFlags, true);
			foreach (object obj in toDeepCopy.oFlags.Keys)
			{
				string sKey = (string)obj;
				this.oFlags[sKey] = toDeepCopy.oFlags[sKey];
			}
			this.oResponse.headers = (HTTPResponseHeaders)toDeepCopy.ResponseHeaders.Clone();
			this.responseBodyBytes = Utilities.Dupe(toDeepCopy.responseBodyBytes);
			this.state = SessionStates.Done;
			this.Timers = toDeepCopy.Timers.Clone();
			this.RaiseSessionCreated();
		}

		/// <summary>
		/// Factory constructor
		/// </summary>
		/// <param name="bClone"></param>
		/// <param name="headersRequest"></param>
		/// <param name="arrRequestBody"></param>
		/// <param name="headersResponse"></param>
		/// <param name="arrResponseBody"></param>
		/// <param name="oSF"></param>
		/// <returns></returns>
		// Token: 0x0600045C RID: 1116 RVA: 0x000295EC File Offset: 0x000277EC
		public static Session BuildFromData(bool bClone, HTTPRequestHeaders headersRequest, byte[] arrRequestBody, HTTPResponseHeaders headersResponse, byte[] arrResponseBody, SessionFlags oSF)
		{
			if (headersRequest == null)
			{
				headersRequest = new HTTPRequestHeaders();
				headersRequest.HTTPMethod = "GET";
				headersRequest.HTTPVersion = "HTTP/1.1";
				headersRequest.UriScheme = "http";
				headersRequest.Add("Host", "localhost");
				headersRequest.RequestPath = "/" + DateTime.Now.Ticks.ToString();
			}
			else if (bClone)
			{
				headersRequest = (HTTPRequestHeaders)headersRequest.Clone();
			}
			if (headersResponse == null)
			{
				headersResponse = new HTTPResponseHeaders();
				headersResponse.SetStatus(200, "OK");
				headersResponse.HTTPVersion = "HTTP/1.1";
				headersResponse.Add("Connection", "close");
			}
			else if (bClone)
			{
				headersResponse = (HTTPResponseHeaders)headersResponse.Clone();
			}
			if (arrRequestBody == null)
			{
				arrRequestBody = Utilities.emptyByteArray;
			}
			else if (bClone)
			{
				arrRequestBody = (byte[])arrRequestBody.Clone();
			}
			if (arrResponseBody == null)
			{
				arrResponseBody = Utilities.emptyByteArray;
			}
			else if (bClone)
			{
				arrResponseBody = (byte[])arrResponseBody.Clone();
			}
			Session oResult = new Session(headersRequest, arrRequestBody, false);
			oResult._AssignID();
			oResult.SetBitFlag(oSF, true);
			oResult.oResponse.headers = headersResponse;
			oResult.responseBodyBytes = arrResponseBody;
			oResult.state = SessionStates.Done;
			return oResult;
		}

		/// <summary>
		/// Indexer property into SESSION flags, REQUEST headers, and RESPONSE headers. e.g. oSession["Request", "Host"] returns string value for the Request host header. If null, returns String.Empty
		/// </summary>
		/// <param name="sCollection">SESSION, REQUEST or RESPONSE</param>
		/// <param name="sName">The name of the flag or header</param>
		/// <returns>String value or String.Empty</returns>
		// Token: 0x170000CB RID: 203
		[CodeDescription("Indexer property into SESSION flags, REQUEST headers, and RESPONSE headers. e.g. oSession[\"Request\", \"Host\"] returns string value for the Request host header. If null, returns String.Empty")]
		public string this[string sCollection, string sName]
		{
			get
			{
				if ("SESSION".OICEquals(sCollection))
				{
					string sValue = this.oFlags[sName];
					return sValue ?? string.Empty;
				}
				if ("REQUEST".OICEquals(sCollection))
				{
					if (!Utilities.HasHeaders(this.oRequest))
					{
						return string.Empty;
					}
					return this.oRequest[sName];
				}
				else
				{
					if (!"RESPONSE".OICEquals(sCollection))
					{
						return "undefined";
					}
					if (!Utilities.HasHeaders(this.oResponse))
					{
						return string.Empty;
					}
					return this.oResponse[sName];
				}
			}
		}

		/// <summary>
		/// Simple indexer into the Session's oFlags object; returns null if flag is not present.
		/// </summary>
		/// <returns>
		/// Returns the string value if the specified flag is present, or null if it is not.
		/// </returns>
		// Token: 0x170000CC RID: 204
		[CodeDescription("Indexer property into session flags collection. oSession[\"Flagname\"] returns string value (or null if missing!).")]
		public string this[string sFlag]
		{
			get
			{
				return this.oFlags[sFlag];
			}
			set
			{
				if (value == null)
				{
					this.oFlags.Remove(sFlag);
					return;
				}
				this.oFlags[sFlag] = value;
			}
		}

		// Token: 0x06000460 RID: 1120 RVA: 0x000297E2 File Offset: 0x000279E2
		internal void ExecuteOnThreadPool()
		{
			ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback(this.Execute), DateTime.Now);
		}

		// Token: 0x06000461 RID: 1121 RVA: 0x00029800 File Offset: 0x00027A00
		internal void ExecuteWhenDataAvailable()
		{
			if (this.m_state > SessionStates.ReadingRequest)
			{
				this.ExecuteOnThreadPool();
				return;
			}
			if (this.oRequest == null || this.oRequest.pipeClient == null)
			{
				return;
			}
			if (this.oRequest.pipeClient.HasDataAvailable())
			{
				this.ExecuteOnThreadPool();
				return;
			}
			Socket oSock = this.oRequest.pipeClient.GetRawSocket();
			if (oSock != null)
			{
				oSock.ReceiveTimeout = ClientPipe._timeoutIdle;
				Interlocked.Increment(ref COUNTERS.ASYNC_WAIT_CLIENT_REUSE);
				Interlocked.Increment(ref COUNTERS.TOTAL_ASYNC_WAIT_CLIENT_REUSE);
				SocketError err;
				oSock.BeginReceive(new byte[1], 0, 1, SocketFlags.Peek, out err, delegate(IAsyncResult arOutcome)
				{
					Interlocked.Decrement(ref COUNTERS.ASYNC_WAIT_CLIENT_REUSE);
					int iOut = 0;
					try
					{
						SocketError serr;
						iOut = oSock.EndReceive(arOutcome, out serr);
					}
					catch (Exception eX)
					{
						if (CONFIG.bDebugSpew)
						{
							FiddlerApplication.DebugSpew("! SocketReuse EndReceive threw {0} for {1}", new object[]
							{
								Utilities.DescribeException(eX),
								(oSock.RemoteEndPoint as IPEndPoint).Port
							});
						}
						iOut = -1;
					}
					if (iOut < 1)
					{
						if (this.oRequest.pipeClient != null)
						{
							this.oRequest.pipeClient.End();
						}
						return;
					}
					this.Execute(null);
				}, null);
				if (err != SocketError.Success && err != SocketError.IOPending)
				{
					Interlocked.Decrement(ref COUNTERS.ASYNC_WAIT_CLIENT_REUSE);
					if (this.oRequest.pipeClient != null)
					{
						this.oRequest.pipeClient.End();
					}
				}
			}
		}

		// Token: 0x06000462 RID: 1122 RVA: 0x000298F4 File Offset: 0x00027AF4
		internal Task ExecuteAsync(object objThreadState)
		{
			return Task.Factory.StartNew(delegate()
			{
				ManualResetEvent resetEvent = new ManualResetEvent(false);
				this.OnStateChanged += delegate(object s, StateChangeEventArgs e)
				{
					if (e.newState >= SessionStates.Done)
					{
						resetEvent.Set();
					}
				};
				this.Execute(objThreadState);
				resetEvent.WaitOne();
			});
		}

		/// <summary>
		/// Called when the Session is ready to begin processing. Eats exceptions to prevent unhandled exceptions on background threads from killing the application.
		/// </summary>
		/// <param name="objThreadState">Unused parameter (required by ThreadPool)</param>
		// Token: 0x06000463 RID: 1123 RVA: 0x0002992C File Offset: 0x00027B2C
		internal void Execute(object objThreadState)
		{
			try
			{
				this.InnerExecute();
			}
			catch (Exception eX)
			{
				string title = "Uncaught Exception in Session #" + this.id.ToString();
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[]
				{
					title,
					eX.ToString()
				});
			}
		}

		// Token: 0x06000464 RID: 1124 RVA: 0x00029990 File Offset: 0x00027B90
		internal void RunStateMachine()
		{
			bool bAsyncExit = false;
			for (;;)
			{
				switch (this._pState)
				{
				case ProcessingStates.GetRequestStart:
					if (!this._executeObtainRequest())
					{
						this._pState = ProcessingStates.Finished;
					}
					else
					{
						this._pState += 1;
					}
					break;
				case ProcessingStates.GetRequestHeadersEnd:
				case ProcessingStates.RunRequestRulesStart:
				case ProcessingStates.RunRequestRulesEnd:
				case ProcessingStates.DetermineGatewayStart:
				case ProcessingStates.DetermineGatewayEnd:
				case ProcessingStates.DNSStart:
				case ProcessingStates.DNSEnd:
				case ProcessingStates.ConnectEnd:
				case ProcessingStates.HTTPSHandshakeStart:
				case ProcessingStates.HTTPSHandshakeEnd:
				case ProcessingStates.SendRequestEnd:
				case ProcessingStates.GetResponseHeadersEnd:
				case ProcessingStates.RunResponseRulesEnd:
				case ProcessingStates.DoAfterSessionEventEnd:
					goto IL_9D5;
				case ProcessingStates.PauseForRequestTampering:
					this._pState += 1;
					break;
				case ProcessingStates.ResumeFromRequestTampering:
					this._EnsureStateAtLeast(SessionStates.AutoTamperRequestAfter);
					this._pState += 1;
					break;
				case ProcessingStates.GetRequestEnd:
					if (this.m_state >= SessionStates.Done)
					{
						return;
					}
					this._smCheckForAutoReply();
					if (this.HTTPMethodIs("CONNECT"))
					{
						this.isTunnel = true;
						if (this.oFlags.ContainsKey("x-replywithtunnel"))
						{
							this._ReturnSelfGeneratedCONNECTTunnel(this.hostname);
							this._pState = ProcessingStates.Finished;
							break;
						}
					}
					if (this.m_state >= SessionStates.ReadingResponse)
					{
						if (this.isAnyFlagSet(SessionFlags.ResponseGeneratedByFiddler))
						{
							FiddlerApplication.DoResponseHeadersAvailable(this);
						}
						this._pState = ProcessingStates.ReadResponseEnd;
					}
					else
					{
						this._smValidateRequestPort();
						if (this._smReplyWithFile())
						{
							this._pState = ProcessingStates.ReadResponseEnd;
						}
						else
						{
							if (this._isDirectRequestToFiddler())
							{
								if (this.oRequest.headers.RequestPath.OICEndsWith(".pac"))
								{
									if (this.oRequest.headers.RequestPath.OICEndsWith("/proxy.pac"))
									{
										this._returnPACFileResponse();
										this._pState = ProcessingStates.Finished;
										break;
									}
									if (this.oRequest.headers.RequestPath.OICEndsWith("/UpstreamProxy.pac"))
									{
										this._returnUpstreamPACFileResponse();
										this._pState = ProcessingStates.Finished;
										break;
									}
								}
								if (this.oRequest.headers.RequestPath.OICEndsWith("/fiddlerroot.cer"))
								{
									Session._returnRootCert(this);
									this._pState = ProcessingStates.Finished;
									break;
								}
								if (CONFIG.iReverseProxyForPort == 0)
								{
									this._returnEchoServiceResponse();
									this._pState = ProcessingStates.Finished;
									break;
								}
								this.oFlags.Add("X-ReverseProxy", "1");
								this.host = string.Format("{0}:{1}", CONFIG.sReverseProxyHostname, CONFIG.iReverseProxyForPort);
							}
							if (this._pState == ProcessingStates.GetRequestEnd)
							{
								this._pState += 1;
							}
						}
					}
					break;
				case ProcessingStates.ConnectStart:
					this.state = SessionStates.SendingRequest;
					if (this.isFTP && !this.isFlagSet(SessionFlags.SentToGateway))
					{
						this._pState = ProcessingStates.ReadResponseStart;
					}
					else
					{
						this.oResponse.BeginAsyncConnectToHost(delegate(IAsyncResult unused)
						{
							if (this.state >= SessionStates.Done)
							{
								this._pState = ProcessingStates.Finished;
							}
							else
							{
								this._pState += 1;
							}
							this.RunStateMachine();
						});
						bAsyncExit = true;
					}
					break;
				case ProcessingStates.SendRequestStart:
				{
					this._EnsureStateAtLeast(SessionStates.SendingRequest);
					bool bSendSucceeded = false;
					try
					{
						this.oResponse.SendRequest();
						bSendSucceeded = true;
					}
					catch (Exception eX)
					{
						if (this.oResponse._MayRetryWhenSendFailed())
						{
							this.oResponse.pipeServer = null;
							StringDictionary stringDictionary = this.oFlags;
							stringDictionary["x-RetryOnFailedSend"] = stringDictionary["x-RetryOnFailedSend"] + "*";
							FiddlerApplication.DebugSpew("[{0}] ServerSocket Reuse failed during SendRequest(). Restarting fresh.", new object[] { this.id });
							this._pState = ProcessingStates.ConnectStart;
							break;
						}
						FiddlerApplication.DebugSpew("SendRequest() failed: {0}", new object[] { Utilities.DescribeException(eX) });
						this.oRequest.FailSession(504, "Fiddler - Send Failure", "[Fiddler] SendRequest() failed: " + Utilities.DescribeException(eX));
					}
					if (this.oFlags.ContainsKey("log-drop-request-body") && !Utilities.IsNullOrEmpty(this.requestBodyBytes) && !this.isAnyFlagSet(SessionFlags.RequestStreamed | SessionFlags.IsRPCTunnel))
					{
						this._smDropRequestBody();
					}
					if (!bSendSucceeded)
					{
						this.CloseSessionPipes(true);
						this.state = SessionStates.Aborted;
						this._pState = ProcessingStates.Finished;
					}
					else if (this.isFlagSet(SessionFlags.RequestStreamed) && !this.oResponse.StreamRequestBody())
					{
						this.CloseSessionPipes(true);
						this.state = SessionStates.Aborted;
						this._pState = ProcessingStates.Finished;
					}
					else
					{
						this.Timers.ServerGotRequest = DateTime.Now;
						if (this.isFlagSet(SessionFlags.IsRPCTunnel))
						{
							bool bTunnelResponseImmediately = false;
							GenericTunnel.CreateTunnel(this, bTunnelResponseImmediately);
							if (bTunnelResponseImmediately)
							{
								this._pState = ProcessingStates.Finished;
								break;
							}
						}
						this._pState += 1;
					}
					break;
				}
				case ProcessingStates.ReadResponseStart:
					this.state = SessionStates.ReadingResponse;
					if (this.HTTPMethodIs("CONNECT") && !this.oResponse.m_bWasForwarded)
					{
						this._BuildConnectionEstablishedReply();
					}
					else if (!this.oResponse.ReadResponse())
					{
						if (this._MayRetryWhenReceiveFailed())
						{
							FiddlerApplication.DebugSpew("[{0}] ServerSocket Reuse failed. Restarting fresh.", new object[] { this.id });
							StringDictionary stringDictionary = this.oFlags;
							stringDictionary["x-RetryOnFailedReceive"] = stringDictionary["x-RetryOnFailedReceive"] + "*";
							this.oResponse.Initialize(true);
							this._pState = ProcessingStates.ConnectStart;
							break;
						}
						FiddlerApplication.DebugSpew("Failed to read server response and retry is forbidden. Aborting Session #{0}", new object[] { this.id });
						this.oResponse.FreeResponseDataBuffer();
						if (this.state != SessionStates.Aborted)
						{
							string sErrorBody = string.Empty;
							if (!Utilities.IsNullOrEmpty(this.responseBodyBytes))
							{
								sErrorBody = Encoding.UTF8.GetString(this.responseBodyBytes);
							}
							sErrorBody = string.Format("[Fiddler] ReadResponse() failed: The server did not return a complete response for this request. Server returned {0:N0} bytes. {1}", this.oResponse.m_responseTotalDataCount, sErrorBody);
							if (!this.oResponse.bLeakedHeaders)
							{
								this.oRequest.FailSession(504, "Fiddler - Receive Failure", sErrorBody);
							}
							else
							{
								try
								{
									this._BuildReceiveFailureReply(sErrorBody);
									this.oRequest.pipeClient.EndWithRST();
								}
								catch
								{
								}
							}
						}
						this.CloseSessionPipes(true);
						this.state = SessionStates.Aborted;
						this._pState = ProcessingStates.Finished;
						break;
					}
					else
					{
						if (200 == this.responseCode && this.isFlagSet(SessionFlags.IsRPCTunnel))
						{
							this._smInitiateRPCStreaming();
							this._pState = ProcessingStates.Finished;
							break;
						}
						if (this.isAnyFlagSet(SessionFlags.ResponseBodyDropped))
						{
							this.responseBodyBytes = Utilities.emptyByteArray;
							this.oResponse.FreeResponseDataBuffer();
						}
						else
						{
							this.responseBodyBytes = this.oResponse.TakeEntity();
							long iEntityLength;
							if (this.oResponse.headers.Exists("Content-Length") && !this.HTTPMethodIs("HEAD") && long.TryParse(this.oResponse.headers["Content-Length"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out iEntityLength) && iEntityLength != (long)this.responseBodyBytes.Length)
							{
								FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, true, true, string.Format("Content-Length mismatch: Response Header indicated {0:N0} bytes, but server sent {1:N0} bytes.", iEntityLength, (long)this.responseBodyBytes.Length));
							}
						}
					}
					this._pState += 1;
					break;
				case ProcessingStates.ReadResponseEnd:
					if (!this.isFlagSet(SessionFlags.ResponseBodyDropped))
					{
						this.oFlags["x-ResponseBodyTransferLength"] = ((this.responseBodyBytes == null) ? "0" : ((long)this.responseBodyBytes.Length).ToString("N0"));
					}
					this.state = SessionStates.AutoTamperResponseBefore;
					this._pState += 1;
					break;
				case ProcessingStates.RunResponseRulesStart:
					FiddlerApplication.DoBeforeResponse(this);
					if (this._smIsResponseAutoHandled())
					{
						this._pState = ProcessingStates.Finished;
					}
					else
					{
						if (this.m_state >= SessionStates.Done || this.isFlagSet(SessionFlags.ResponseStreamed))
						{
							this.FinishUISession();
							if (this.isFlagSet(SessionFlags.ResponseStreamed) && this.oFlags.ContainsKey("log-drop-response-body"))
							{
								this.SetBitFlag(SessionFlags.ResponseBodyDropped, true);
								this.responseBodyBytes = Utilities.emptyByteArray;
							}
							this.bLeakedResponseAlready = true;
						}
						if (this.bLeakedResponseAlready)
						{
							this._EnsureStateAtLeast(SessionStates.Done);
							FiddlerApplication.DoAfterSessionComplete(this);
							this._pState = ProcessingStates.ReturnBufferedResponseStart;
						}
						else
						{
							if (this.oFlags.ContainsKey("x-replywithfile"))
							{
								this.LoadResponseFromFile(this.oFlags["x-replywithfile"]);
								this.oFlags["x-replacedwithfile"] = this.oFlags["x-replywithfile"];
								this.oFlags.Remove("x-replywithfile");
							}
							this._pState += 1;
						}
					}
					break;
				case ProcessingStates.PauseForResponseTampering:
					if (!this.oFlags.ContainsKey("x-breakresponse"))
					{
						goto IL_9D5;
					}
					if (FiddlerApplication._AutoResponder.IsEnabled)
					{
						FiddlerApplication._AutoResponder.DoMatchAfterResponse(this);
					}
					this._pState += 1;
					break;
				case ProcessingStates.ResumeFromResponseTampering:
					if (this.oSyncEvent != null)
					{
						this.oSyncEvent.Close();
						this.oSyncEvent = null;
					}
					if (this.m_state >= SessionStates.Done)
					{
						this._pState = ProcessingStates.Finished;
					}
					else
					{
						this.state = SessionStates.AutoTamperResponseAfter;
						this._pState += 1;
					}
					break;
				case ProcessingStates.ReturnBufferedResponseStart:
				{
					bool bIsNTLMType2 = false;
					if (this._isResponseMultiStageAuthChallenge())
					{
						bIsNTLMType2 = this._isNTLMType2();
					}
					if (this.m_state >= SessionStates.Done)
					{
						this.FinishUISession();
						this.bLeakedResponseAlready = true;
					}
					if (!this.bLeakedResponseAlready)
					{
						this.ReturnResponse(bIsNTLMType2);
					}
					if (this.bLeakedResponseAlready && this.oRequest.pipeClient != null)
					{
						bool bMayReuseClientSocket = bIsNTLMType2 || this._MayReuseMyClientPipe();
						if (bMayReuseClientSocket)
						{
							this._createNextSession(bIsNTLMType2);
						}
						else
						{
							this.oRequest.pipeClient.End();
						}
						this.oRequest.pipeClient = null;
					}
					this._pState += 1;
					break;
				}
				case ProcessingStates.ReturnBufferedResponseEnd:
					this.oResponse.releaseServerPipe();
					this._pState += 1;
					break;
				case ProcessingStates.DoAfterSessionEventStart:
					this._pState += 1;
					break;
				case ProcessingStates.Finished:
					this._EnsureStateAtLeast(SessionStates.Done);
					if (this.nextSession != null)
					{
						this.nextSession.ExecuteWhenDataAvailable();
						this.nextSession = null;
					}
					bAsyncExit = true;
					break;
				default:
					goto IL_9D5;
				}
				IL_A2F:
				if (bAsyncExit)
				{
					return;
				}
				continue;
				IL_9D5:
				if (this._pState > ProcessingStates.Finished)
				{
					FiddlerApplication.Log.LogFormat("! CRITICAL ERROR: State machine will live forever... Session {0} with state {1} has pState {2}", new object[] { this.id, this.state, this._pState });
					bAsyncExit = true;
				}
				this._pState += 1;
				goto IL_A2F;
			}
		}

		/// <summary>
		/// InnerExecute() implements Fiddler's HTTP Pipeline
		/// </summary>
		// Token: 0x06000465 RID: 1125 RVA: 0x0002A3F0 File Offset: 0x000285F0
		private void InnerExecute()
		{
			if (this.oRequest == null || this.oResponse == null)
			{
				return;
			}
			this.RunStateMachine();
		}

		/// <summary>
		/// Initiate bi-directional streaming on the RPC connection
		/// </summary>
		// Token: 0x06000466 RID: 1126 RVA: 0x0002A40C File Offset: 0x0002860C
		private void _smInitiateRPCStreaming()
		{
			this.responseBodyBytes = this.oResponse.TakeEntity();
			try
			{
				this.oRequest.pipeClient.Send(this.oResponse.headers.ToByteArray(true, true));
				this.oRequest.pipeClient.Send(this.responseBodyBytes);
				this.SetBitFlag(SessionFlags.ResponseBodyDropped, true);
				this.responseBodyBytes = Utilities.emptyByteArray;
				(this.__oTunnel as GenericTunnel).BeginResponseStreaming();
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("Failed to create RPC Tunnel {0}", new object[] { Utilities.DescribeException(eX) });
			}
		}

		// Token: 0x06000467 RID: 1127 RVA: 0x0002A4BC File Offset: 0x000286BC
		private void _smDropRequestBody()
		{
			this.oFlags["x-RequestBodyLength"] = this.requestBodyBytes.Length.ToString("N0");
			this.requestBodyBytes = Utilities.emptyByteArray;
			this.SetBitFlag(SessionFlags.RequestBodyDropped, true);
		}

		// Token: 0x06000468 RID: 1128 RVA: 0x0002A505 File Offset: 0x00028705
		private bool _smIsResponseAutoHandled()
		{
			if (Utilities.HasHeaders(this.oResponse))
			{
				if (this._handledAsAutomaticRedirect())
				{
					return true;
				}
				if (this._handledAsAutomaticAuth())
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000469 RID: 1129 RVA: 0x0002A52C File Offset: 0x0002872C
		private bool _smReplyWithFile()
		{
			if (!this.oFlags.ContainsKey("x-replywithfile"))
			{
				return false;
			}
			this.oResponse = new ServerChatter(this, "HTTP/1.1 200 OK\r\nServer: Fiddler\r\n\r\n");
			if (this.LoadResponseFromFile(this.oFlags["x-replywithfile"]) && this.isAnyFlagSet(SessionFlags.ResponseGeneratedByFiddler))
			{
				FiddlerApplication.DoResponseHeadersAvailable(this);
			}
			this.oFlags["x-repliedwithfile"] = this.oFlags["x-replywithfile"];
			this.oFlags.Remove("x-replywithfile");
			return true;
		}

		// Token: 0x0600046A RID: 1130 RVA: 0x0002A5BA File Offset: 0x000287BA
		private void _smValidateRequestPort()
		{
			if (this.port < 0 || this.port > 65535)
			{
				FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, true, false, "HTTP Request specified an invalid port number.");
			}
		}

		// Token: 0x0600046B RID: 1131 RVA: 0x0002A5E4 File Offset: 0x000287E4
		private void _BuildReceiveFailureReply(string sErrorBody)
		{
			this.oResponse.headers = new HTTPResponseHeaders(CONFIG.oHeaderEncoding);
			this.oResponse.headers.SetStatus(504, "Fiddler - Receive Failure while streaming");
			this.oResponse.headers.Add("Date", DateTime.UtcNow.ToString("r"));
			this.oResponse.headers.Add("Content-Type", "text/html; charset=UTF-8");
			this.oResponse.headers.Add("Connection", "close");
			this.oResponse.headers.Add("Cache-Control", "no-cache, must-revalidate");
			this.oResponse.headers.Add("Timestamp", DateTime.Now.ToString("HH:mm:ss.fff"));
			this.responseBodyBytes = Encoding.ASCII.GetBytes(sErrorBody);
		}

		// Token: 0x0600046C RID: 1132 RVA: 0x0002A6D4 File Offset: 0x000288D4
		private void _BuildConnectionEstablishedReply()
		{
			this.SetBitFlag(SessionFlags.ResponseGeneratedByFiddler, true);
			this.oResponse.headers = new HTTPResponseHeaders();
			this.oResponse.headers.HTTPVersion = this.oRequest.headers.HTTPVersion;
			this.oResponse.headers.SetStatus(200, "Connection Established");
			this.oResponse.headers.Add("FiddlerGateway", "Direct");
			this.oResponse.headers.Add("StartTime", DateTime.Now.ToString("HH:mm:ss.fff"));
			if (!this.oFlags.ContainsKey("x-ConnectResponseRemoveConnectionClose"))
			{
				this.oResponse.headers.Add("Connection", "close");
			}
			this.responseBodyBytes = Utilities.emptyByteArray;
		}

		/// <summary>
		/// Ensure that the Session's state is &gt;= ss, updating state if necessary
		/// </summary>
		/// <param name="ss">TargetState</param>
		// Token: 0x0600046D RID: 1133 RVA: 0x0002A7B4 File Offset: 0x000289B4
		private void _EnsureStateAtLeast(SessionStates ss)
		{
			if (this.m_state < ss)
			{
				SessionStates oldState = this.m_state;
				this.m_state = ss;
				this.RaiseOnStateChangedIfNotIgnored(oldState, this.m_state);
			}
		}

		/// <summary>
		/// May this Session be resent on a different connection because reading of the response did not succeed?
		/// </summary>
		/// <returns>TRUE if the entire session may be resent on a new connection</returns>
		// Token: 0x0600046E RID: 1134 RVA: 0x0002A7E8 File Offset: 0x000289E8
		private bool _MayRetryWhenReceiveFailed()
		{
			if (!this.oResponse.bServerSocketReused || this.state == SessionStates.Aborted || this.oResponse.bLeakedHeaders)
			{
				return false;
			}
			if (this.isAnyFlagSet(SessionFlags.RequestBodyDropped))
			{
				return false;
			}
			RetryMode retryOnReceiveFailure = CONFIG.RetryOnReceiveFailure;
			return retryOnReceiveFailure != RetryMode.Never && (retryOnReceiveFailure != RetryMode.IdempotentOnly || Utilities.HTTPMethodIsIdempotent(this.RequestMethod));
		}

		/// <summary>
		/// If the response demands credentials and the Session is configured to have Fiddler provide those
		/// credentials, try to do so now.
		/// </summary>
		/// <returns>TRUE if Fiddler has generated a response to an Auth challenge; FALSE otherwise.</returns>
		// Token: 0x0600046F RID: 1135 RVA: 0x0002A84C File Offset: 0x00028A4C
		private bool _handledAsAutomaticAuth()
		{
			if (!this._isResponseAuthChallenge() || !this.oFlags.ContainsKey("x-AutoAuth") || this.oFlags.ContainsKey("x-AutoAuth-Failed"))
			{
				this.__WebRequestForAuth = null;
				return false;
			}
			bool result;
			try
			{
				result = this._PerformInnerAuth();
			}
			catch (TypeLoadException oTLE)
			{
				Logger log = FiddlerApplication.Log;
				string str = "!Warning: Automatic authentication failed. You should installl the latest .NET Framework 2.0/3.5 Service Pack from WindowsUpdate.\n";
				TypeLoadException ex = oTLE;
				log.LogFormat(str + ((ex != null) ? ex.ToString() : null), Array.Empty<object>());
				result = false;
			}
			return result;
		}

		/// <summary>
		/// This method will perform obtain authentication credentials from System.NET using a reflection trick to grab the internal value.
		/// It's needed to cope with Channel-Binding-Tokens (CBT).
		///
		/// This MUST live within its own non-inlined method such that when it's run on an outdated version of the .NET Framework, the outdated
		/// version of the target object triggers a TypeLoadException in such a way that the caller can catch it and warn the user without 
		/// killing Fiddler.exe.
		/// </summary>
		/// <returns>TRUE if we didn't hit any exceptions</returns>
		// Token: 0x06000470 RID: 1136 RVA: 0x0002A8D4 File Offset: 0x00028AD4
		[MethodImpl(MethodImplOptions.NoInlining)]
		private bool _PerformInnerAuth()
		{
			bool bIsProxyAuth = 407 == this.oResponse.headers.HTTPResponseCode;
			if (bIsProxyAuth && this.isHTTPS && FiddlerApplication.Prefs.GetBoolPref("fiddler.security.ForbidServer407", true))
			{
				return false;
			}
			bool result;
			try
			{
				string sUrl = this.oFlags["X-AutoAuth-URL"];
				if (string.IsNullOrEmpty(sUrl))
				{
					if (bIsProxyAuth)
					{
						sUrl = this.fullUrl;
					}
					else
					{
						sUrl = this.fullUrl;
					}
				}
				Uri oUrl = new Uri(sUrl);
				if (CONFIG.bDebugSpew)
				{
					FiddlerApplication.DebugSpew("Performing automatic authentication to {0} in response to {1}", new object[]
					{
						oUrl,
						this.oResponse.headers.HTTPResponseCode
					});
				}
				if (this.__WebRequestForAuth == null)
				{
					this.__WebRequestForAuth = WebRequest.Create(oUrl);
				}
				Type tWebReq = this.__WebRequestForAuth.GetType();
				tWebReq.InvokeMember("Async", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetProperty, null, this.__WebRequestForAuth, new object[] { false });
				object objServerAuthState = tWebReq.InvokeMember("ServerAuthenticationState", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty, null, this.__WebRequestForAuth, new object[0]);
				if (objServerAuthState == null)
				{
					throw new ApplicationException("Auth state is null");
				}
				Type tAuthState = objServerAuthState.GetType();
				tAuthState.InvokeMember("ChallengedUri", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField, null, objServerAuthState, new object[] { oUrl });
				string sSPN = this.oFlags["X-AutoAuth-SPN"];
				if (sSPN == null && !bIsProxyAuth)
				{
					sSPN = Session._GetSPNForUri(oUrl);
				}
				if (sSPN != null)
				{
					if (CONFIG.bDebugSpew)
					{
						FiddlerApplication.DebugSpew("Authenticating to '{0}' with ChallengedSpn='{1}'", new object[] { oUrl, sSPN });
					}
					bool bSetSPNUsingObject = false;
					if (Session.bTrySPNTokenObject)
					{
						try
						{
							Assembly asm = Assembly.GetAssembly(typeof(AuthenticationManager));
							Type tspntoken = asm.GetType("System.Net.SpnToken", true);
							Type type = tspntoken;
							BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;
							Binder binder = null;
							object[] args = new string[] { sSPN };
							object oSPNToken = Activator.CreateInstance(type, bindingAttr, binder, args, CultureInfo.InvariantCulture);
							tAuthState.InvokeMember("ChallengedSpn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField, null, objServerAuthState, new object[] { oSPNToken });
							bSetSPNUsingObject = true;
						}
						catch (Exception eX)
						{
							FiddlerApplication.DebugSpew(Utilities.DescribeException(eX));
							Session.bTrySPNTokenObject = false;
						}
					}
					if (!bSetSPNUsingObject)
					{
						tAuthState.InvokeMember("ChallengedSpn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField, null, objServerAuthState, new object[] { sSPN });
					}
				}
				try
				{
					if (this.oResponse.pipeServer != null && this.oResponse.pipeServer.bIsSecured)
					{
						TransportContext oTC = this.oResponse.pipeServer._GetTransportContext();
						if (oTC != null)
						{
							tAuthState.InvokeMember("_TransportContext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetField, null, objServerAuthState, new object[] { oTC });
						}
					}
				}
				catch (Exception eX2)
				{
					FiddlerApplication.Log.LogFormat("Cannot get TransportContext. You may need to upgrade to a later .NET Framework. {0}", new object[] { eX2.Message });
				}
				string sAuthString = (bIsProxyAuth ? this.oResponse["Proxy-Authenticate"] : this.oResponse["WWW-Authenticate"]);
				ICredentials oCreds;
				if (this.oFlags["x-AutoAuth"].Contains(":"))
				{
					string sUserName = Utilities.TrimAfter(this.oFlags["x-AutoAuth"], ':');
					if (sUserName.Contains("\\"))
					{
						string sDomain = Utilities.TrimAfter(sUserName, '\\');
						sUserName = Utilities.TrimBefore(sUserName, '\\');
						oCreds = new NetworkCredential(sUserName, Utilities.TrimBefore(this.oFlags["x-AutoAuth"], ':'), sDomain);
					}
					else
					{
						oCreds = new NetworkCredential(sUserName, Utilities.TrimBefore(this.oFlags["x-AutoAuth"], ':'));
					}
				}
				else
				{
					oCreds = CredentialCache.DefaultCredentials;
				}
				this.__WebRequestForAuth.Method = this.RequestMethod;
				Authorization auth = AuthenticationManager.Authenticate(sAuthString, this.__WebRequestForAuth, oCreds);
				if (auth == null)
				{
					throw new Exception("AuthenticationManager.Authenticate returned null.");
				}
				string sAuth = auth.Message;
				this.nextSession = new Session(this.oRequest.pipeClient, this.oResponse.pipeServer);
				this.nextSession.propagateProcessInfo(this);
				this.FireContinueTransaction(this, this.nextSession, ContinueTransactionReason.Authenticate);
				if (!auth.Complete)
				{
					this.nextSession.__WebRequestForAuth = this.__WebRequestForAuth;
				}
				this.__WebRequestForAuth = null;
				this.nextSession.requestBodyBytes = this.requestBodyBytes;
				this.nextSession.oRequest.headers = (HTTPRequestHeaders)this.oRequest.headers.Clone();
				this.nextSession.oRequest.headers[bIsProxyAuth ? "Proxy-Authorization" : "Authorization"] = sAuth;
				this.nextSession.SetBitFlag(SessionFlags.RequestGeneratedByFiddler, true);
				if (this.oFlags.ContainsKey("x-From-Builder"))
				{
					this.nextSession.oFlags["x-From-Builder"] = this.oFlags["x-From-Builder"] + " > +Auth";
				}
				int iRetries;
				if (int.TryParse(this.oFlags["x-AutoAuth-Retries"], out iRetries))
				{
					iRetries--;
					if (iRetries > 0)
					{
						this.nextSession.oFlags["x-AutoAuth"] = this.oFlags["x-AutoAuth"];
						this.nextSession.oFlags["x-AutoAuth-Retries"] = iRetries.ToString();
					}
					else
					{
						this.nextSession.oFlags["x-AutoAuth-Failed"] = "true";
					}
				}
				else
				{
					this.nextSession.oFlags["x-AutoAuth-Retries"] = "5";
					this.nextSession.oFlags["x-AutoAuth"] = this.oFlags["x-AutoAuth"];
				}
				if (this.oFlags.ContainsKey("x-Builder-Inspect"))
				{
					this.nextSession.oFlags["x-Builder-Inspect"] = this.oFlags["x-Builder-Inspect"];
				}
				if (this.oFlags.ContainsKey("x-Builder-MaxRedir"))
				{
					this.nextSession.oFlags["x-Builder-MaxRedir"] = this.oFlags["x-Builder-MaxRedir"];
				}
				this.state = SessionStates.Done;
				this.nextSession.state = SessionStates.AutoTamperRequestBefore;
				this.FinishUISession();
				result = true;
			}
			catch (Exception eX3)
			{
				FiddlerApplication.Log.LogFormat("Automatic authentication of Session #{0} was unsuccessful. {1}\n{2}", new object[]
				{
					this.id,
					Utilities.DescribeException(eX3),
					eX3.StackTrace
				});
				this.__WebRequestForAuth = null;
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Copies process-owner information from a source session to a destination session. Used during handling of AutoRedirects
		/// and auto-Authentications
		/// </summary>
		/// <param name="sessionFrom"></param>
		// Token: 0x06000471 RID: 1137 RVA: 0x0002AF84 File Offset: 0x00029184
		internal void propagateProcessInfo(Session sessionFrom)
		{
			if (this._LocalProcessID != 0)
			{
				return;
			}
			if (sessionFrom == null)
			{
				this._LocalProcessID = FiddlerApplication.iPID;
				this.oFlags["x-ProcessInfo"] = FiddlerApplication.sProcessInfo;
			}
			else
			{
				this._LocalProcessID = sessionFrom._LocalProcessID;
				if (sessionFrom.oFlags.ContainsKey("x-ProcessInfo"))
				{
					this.oFlags["x-ProcessInfo"] = sessionFrom.oFlags["x-ProcessInfo"];
				}
			}
			string sessionProcessInfo = this.oFlags["x-ProcessInfo"];
			this.SetDecryptFlagIfSessionIsFromInstrumentedBrowserProcess(sessionProcessInfo);
		}

		/// <summary>
		/// Returns a Kerberos-usable SPN for the target
		/// http://dev.chromium.org/developers/design-documents/http-authentication
		/// "HttpAuthHandlerNegotiate::CreateSPN"
		/// http://blog.michelbarneveld.nl/michel/archive/2009/11/14/the-reason-why-kb911149-and-kb908209-are-not-the-soluton.aspx
		/// </summary>
		/// <param name="uriTarget"></param>
		/// <returns></returns>
		// Token: 0x06000472 RID: 1138 RVA: 0x0002B018 File Offset: 0x00029218
		private static string _GetSPNForUri(Uri uriTarget)
		{
			int iSPNMode = FiddlerApplication.Prefs.GetInt32Pref("fiddler.auth.SPNMode", 3);
			string sSPN;
			switch (iSPNMode)
			{
			case 0:
				return null;
			case 1:
				sSPN = uriTarget.DnsSafeHost;
				goto IL_72;
			}
			sSPN = uriTarget.DnsSafeHost;
			if (iSPNMode == 3 || (uriTarget.HostNameType != UriHostNameType.IPv6 && uriTarget.HostNameType != UriHostNameType.IPv4 && sSPN.IndexOf('.') == -1))
			{
				string sCName = DNSResolver.GetCanonicalName(uriTarget.DnsSafeHost);
				if (!string.IsNullOrEmpty(sCName))
				{
					sSPN = sCName;
				}
			}
			IL_72:
			sSPN = "HTTP/" + sSPN;
			if (uriTarget.Port != 80 && uriTarget.Port != 443 && FiddlerApplication.Prefs.GetBoolPref("fiddler.auth.SPNIncludesPort", false))
			{
				sSPN = sSPN + ":" + uriTarget.Port.ToString();
			}
			return sSPN;
		}

		/// <summary>
		/// Returns the fully-qualified URL to which this Session's response points, or null.
		/// This method is needed because many servers (illegally) return a relative url in HTTP/3xx Location response headers.
		/// </summary>
		/// <returns>null, or Target URL. Note, you may want to call Utilities.TrimAfter(sTarget, '#'); on the response</returns>
		// Token: 0x06000473 RID: 1139 RVA: 0x0002B0E7 File Offset: 0x000292E7
		public string GetRedirectTargetURL()
		{
			if (!Utilities.IsRedirectStatus(this.responseCode) || !Utilities.HasHeaders(this.oResponse))
			{
				return null;
			}
			return Session.GetRedirectTargetURL(this.fullUrl, this.oResponse["Location"]);
		}

		/// <summary>
		/// Gets a redirect-target from a base URI and a Location header
		/// </summary>
		/// <param name="sBase"></param>
		/// <param name="sLocation"></param>
		/// <returns>null, or Target URL. Note, you may want to call Utilities.TrimAfter(sTarget, '#');</returns>
		// Token: 0x06000474 RID: 1140 RVA: 0x0002B120 File Offset: 0x00029320
		public static string GetRedirectTargetURL(string sBase, string sLocation)
		{
			int ixProtocolEnd = sLocation.IndexOf(":");
			if (ixProtocolEnd < 0 || sLocation.IndexOfAny(new char[] { '/', '?', '#' }) < ixProtocolEnd)
			{
				try
				{
					Uri uriBase = new Uri(sBase);
					Uri uriNew = new Uri(uriBase, sLocation);
					return uriNew.ToString();
				}
				catch (UriFormatException)
				{
					return null;
				}
			}
			return sLocation;
		}

		/// <summary>
		/// Fiddler can only auto-follow redirects to HTTP/HTTPS/FTP.
		/// </summary>
		/// <param name="sBase">The BASE URL to which a relative redirection should be applied</param>
		/// <param name="sLocation">Response "Location" header</param>
		/// <returns>TRUE if the auto-redirect target is allowed</returns>
		// Token: 0x06000475 RID: 1141 RVA: 0x0002B188 File Offset: 0x00029388
		private static bool isRedirectableURI(string sBase, string sLocation, out string sTarget)
		{
			sTarget = Session.GetRedirectTargetURL(sBase, sLocation);
			return sTarget != null && sTarget.OICStartsWithAny(new string[] { "http://", "https://", "ftp://" });
		}

		/// <summary>
		/// Handles a Response's Redirect if the Session is configured to do so.
		/// </summary>
		/// <returns>TRUE if a redirect was handled, FALSE otherwise</returns>
		// Token: 0x06000476 RID: 1142 RVA: 0x0002B1C0 File Offset: 0x000293C0
		private bool _handledAsAutomaticRedirect()
		{
			if (this.oResponse.headers.HTTPResponseCode < 300 || this.oResponse.headers.HTTPResponseCode > 308 || this.HTTPMethodIs("CONNECT") || !this.oFlags.ContainsKey("x-Builder-MaxRedir") || !this.oResponse.headers.Exists("Location"))
			{
				return false;
			}
			string sTarget;
			if (!Session.isRedirectableURI(this.fullUrl, this.oResponse["Location"], out sTarget))
			{
				return false;
			}
			this.nextSession = new Session(this.oRequest.pipeClient, null);
			this.nextSession.propagateProcessInfo(this);
			this.nextSession.oRequest.headers = (HTTPRequestHeaders)this.oRequest.headers.Clone();
			sTarget = Utilities.TrimAfter(sTarget, '#');
			try
			{
				this.nextSession.fullUrl = new Uri(sTarget).AbsoluteUri;
			}
			catch (UriFormatException exU)
			{
				FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("Redirect's Location header was malformed.\nLocation: {0}\n\n{1}", sTarget, exU));
				this.nextSession.fullUrl = sTarget;
			}
			if (this.oResponse.headers.HTTPResponseCode == 307 || this.oResponse.headers.HTTPResponseCode == 308)
			{
				this.nextSession.requestBodyBytes = Utilities.Dupe(this.requestBodyBytes);
			}
			else
			{
				if (!this.nextSession.HTTPMethodIs("HEAD"))
				{
					this.nextSession.RequestMethod = "GET";
				}
				this.nextSession.oRequest.headers.RemoveRange(new string[] { "Content-Type", "Content-Length", "Transfer-Encoding", "Content-Encoding", "Expect" });
				this.nextSession.requestBodyBytes = Utilities.emptyByteArray;
			}
			if (FiddlerApplication.Prefs.GetBoolPref("fiddler.reissue.UpdateHeadersOnAutoRedirectedRequest", true))
			{
				this.nextSession.oRequest.headers.RemoveRange(new string[]
				{
					"Accept", "Pragma", "Connection", "X-Download-Initiator", "Range", "If-Modified-Since", "If-Unmodified-Since", "Unless-Modified-Since", "If-Range", "If-Match",
					"If-None-Match"
				});
				this.nextSession.oRequest.headers.RemoveRange(new string[] { "Authorization", "Proxy-Authorization", "Cookie", "Cookie2" });
			}
			this.nextSession.SetBitFlag(SessionFlags.RequestGeneratedByFiddler, true);
			if (this.oFlags.ContainsKey("x-From-Builder"))
			{
				this.nextSession.oFlags["x-From-Builder"] = this.oFlags["x-From-Builder"] + " > +Redir";
			}
			if (this.oFlags.ContainsKey("x-AutoAuth"))
			{
				this.nextSession.oFlags["x-AutoAuth"] = this.oFlags["x-AutoAuth"];
			}
			if (this.oFlags.ContainsKey("x-Builder-Inspect"))
			{
				this.nextSession.oFlags["x-Builder-Inspect"] = this.oFlags["x-Builder-Inspect"];
			}
			int iRedir;
			if (int.TryParse(this.oFlags["x-Builder-MaxRedir"], out iRedir))
			{
				iRedir--;
				if (iRedir > 0)
				{
					this.nextSession.oFlags["x-Builder-MaxRedir"] = iRedir.ToString();
				}
			}
			this.FireContinueTransaction(this, this.nextSession, ContinueTransactionReason.Redirect);
			this.oResponse.releaseServerPipe();
			this.nextSession.state = SessionStates.AutoTamperRequestBefore;
			this.state = SessionStates.Done;
			this.FinishUISession();
			return true;
		}

		// Token: 0x06000477 RID: 1143 RVA: 0x0002B5A8 File Offset: 0x000297A8
		private void ExecuteHTTPLintOnRequest()
		{
			if (this.oRequest.headers == null)
			{
				return;
			}
			if (this.fullUrl.Length > 2083)
			{
				FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, false, false, string.Format("[HTTPLint #M001] Request URL was {0} characters. WinINET-based clients encounter problems when dealing with URLs longer than 2083 characters.", this.fullUrl.Length));
			}
			if (this.fullUrl.Contains("#"))
			{
				FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, false, false, "[HTTPLint #H002] Request URL contained '#'. URL Fragments should not be sent to the server.");
			}
			if (this.oRequest.headers.ByteCount() > 16000)
			{
				FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, false, false, string.Format("[HTTPLint #M003] Request headers were {0:N0} bytes long. Many servers will reject requests this large.", this.oRequest.headers.ByteCount()));
			}
			string sReferer = this.oRequest["Referer"];
			if (!string.IsNullOrEmpty(sReferer))
			{
				if (sReferer.Contains("#"))
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, false, false, "[HTTPLint #M004] Referer Header contained '#'. URL Fragments should not be sent to the server.");
				}
				if (!this.isHTTPS && !sReferer.StartsWith("http:"))
				{
					try
					{
						Uri uriReferer = new Uri(sReferer);
						if (uriReferer.AbsolutePath != "/" || uriReferer.Query != string.Empty)
						{
							FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, false, false, "[HTTPLint #H005] Referer Header leaked a private URL on an unsecure request: '" + sReferer + "' Referrer Policy may be in use.");
						}
					}
					catch
					{
					}
				}
			}
		}

		/// <summary>
		/// Check for common mistakes in HTTP Responses and notify the user if they are found. Called only if Linting is enabled.
		/// </summary>
		// Token: 0x06000478 RID: 1144 RVA: 0x0002B710 File Offset: 0x00029910
		private void ExecuteHTTPLintOnResponse()
		{
			if (this.responseBodyBytes == null || this.oResponse.headers == null)
			{
				return;
			}
			if (this.oResponse.headers.Exists("Content-Encoding"))
			{
				if (this.oResponse.headers.ExistsAndContains("Content-Encoding", ",") && !this.oResponse.headers.ExistsAndContains("Content-Encoding", "sdch"))
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint #M006] Response appears to specify multiple encodings: '{0}'. This will prevent decoding in Internet Explorer.", this.oResponse.headers["Content-Encoding"]));
				}
				if (this.oResponse.headers.ExistsAndContains("Content-Encoding", "gzip") && this.oRequest != null && this.oRequest.headers != null && !this.oRequest.headers.ExistsAndContains("Accept-Encoding", "gzip"))
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #H008] Illegal response. Response specified Content-Encoding: gzip, but request did not specify GZIP in Accept-Encoding.");
				}
				if (this.oResponse.headers.ExistsAndContains("Content-Encoding", "deflate"))
				{
					if (this.oRequest != null && this.oRequest.headers != null && !this.oRequest.headers.ExistsAndContains("Accept-Encoding", "deflate"))
					{
						FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #H008] Illegal response. Response specified Content-Encoding: Deflate, but request did not specify Deflate in Accept-Encoding.");
					}
					if (this.responseBodyBytes != null && this.responseBodyBytes.Length > 2 && (this.responseBodyBytes[0] & 15) == 8 && (this.responseBodyBytes[0] & 128) == 0 && (((int)this.responseBodyBytes[0] << 8) + (int)this.responseBodyBytes[1]) % 31 == 0)
					{
						FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #M028] Response specified Content-Encoding: Deflate, but content included RFC1950 header and footer bytes incompatible with many clients.");
					}
				}
				if (this.oResponse.headers.ExistsAndContains("Content-Encoding", "br") && this.oRequest != null && this.oRequest.headers != null && !this.oRequest.headers.ExistsAndContains("Accept-Encoding", "br"))
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #H008] Illegal response. Response specified Content-Encoding: br, but request did not specify br (Brotli) in Accept-Encoding.");
				}
				if (this.oResponse.headers.ExistsAndContains("Content-Encoding", "chunked"))
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #H009] Response specified Content-Encoding: chunked, but Chunked is a Transfer-Encoding.");
				}
			}
			if (this.oResponse.headers.ExistsAndContains("Transfer-Encoding", "chunked"))
			{
				if ((Utilities.HasHeaders(this.oRequest) && "HTTP/1.0".OICEquals(this.oRequest.headers.HTTPVersion)) || "HTTP/1.0".OICEquals(this.oResponse.headers.HTTPVersion))
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #H010] Invalid response. Responses to HTTP/1.0 clients MUST NOT specify a Transfer-Encoding.");
				}
				if (this.oResponse.headers.Exists("Content-Length"))
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #M011] Invalid response headers. Messages MUST NOT include both a Content-Length header field and a non-identity transfer-coding.");
				}
				if (!this.isAnyFlagSet(SessionFlags.ResponseBodyDropped))
				{
					long lZero = 0L;
					long lEnd = (long)this.responseBodyBytes.Length;
					if (!Utilities.IsChunkedBodyComplete(this, this.responseBodyBytes, 0L, (long)this.responseBodyBytes.Length, out lZero, out lEnd))
					{
						FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, true, true, "[HTTPLint #M012] The HTTP Chunked response body was incomplete; most likely lacking the final 0-size chunk.");
					}
				}
			}
			List<HTTPHeaderItem> listETags = this.oResponse.headers.FindAll("ETAG");
			if (listETags.Count > 1)
			{
				FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint #H013] Response contained {0} ETag headers", listETags.Count));
			}
			if (listETags.Count > 0)
			{
				string sETag = listETags[0].Value;
				if (!sETag.EndsWith("\"") || (!sETag.StartsWith("\"") && !sETag.StartsWith("W/\"")))
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint #L014] ETag values must be a quoted string. Response ETag: {0}", sETag));
				}
			}
			if (!this.oResponse.headers.Exists("Date") && this.responseCode > 199 && this.responseCode < 500 && !this.HTTPMethodIs("CONNECT"))
			{
				FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #L015] With rare exceptions, servers MUST include a DATE response header. RFC7231 Section 7.1.1.2");
			}
			if (this.responseCode > 299 && this.responseCode != 304 && this.responseCode < 399)
			{
				if (308 == this.responseCode)
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #M016] Server returned a HTTP/308 redirect. Most clients do not handle HTTP/308; instead use a HTTP/307 with a Cache-Control header.");
				}
				if (this.oResponse.headers.Exists("Location"))
				{
					if (this.oResponse["Location"].StartsWith("/"))
					{
						FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint #L017] HTTP Location header should specify a fully-qualified URL. Location: {0}", this.oResponse["Location"]));
					}
				}
				else
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #H018] HTTP/3xx redirect response headers lacked a Location header.");
				}
			}
			string sCT = this.oResponse.headers["Content-Type"];
			if (sCT.OICContains("utf8"))
			{
				FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #M019] Content-Type header specified CharSet=UTF8; for better compatibility, use CharSet=UTF-8 instead.");
			}
			if (sCT.OICContains("image/jpg"))
			{
				FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #L026] Content-Type header specified 'image/jpg'; correct type is 'image/jpeg'.");
			}
			string sCacheControl = this.oResponse.headers.AllValues("Cache-Control");
			if (sCacheControl.OICContains("pre-check") || sCacheControl.OICContains("post-check"))
			{
				if (sCacheControl.OICContains("no-cache"))
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #L024] The pre-check and post-check tokens are meaningless when Cache-Control: no-cache is specified.");
				}
				else
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #L025] Cache-Control header contained non-standard tokens. pre-check and post-check are poorly supported and almost never used properly.");
				}
			}
			if (206 != this.responseCode && !Utilities.IsNullOrEmpty(this.responseBodyBytes) && !this.oResponse.headers.Exists("Transfer-Encoding") && !this.oResponse.headers.Exists("Content-Encoding"))
			{
				if (this.oResponse.headers.ExistsAndContains("Content-Type", "image/png"))
				{
					if (!Utilities.HasMagicBytes(this.responseBodyBytes, new byte[] { 137, 80, 78, 71 }))
					{
						FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #M027] Declared 'Content-Type: image/png' does not match response body content.");
					}
				}
				else if (this.oResponse.headers.ExistsAndContains("Content-Type", "image/gif"))
				{
					if (!Utilities.HasMagicBytes(this.responseBodyBytes, new byte[] { 71, 73, 70, 56 }))
					{
						FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #M027] Declared 'Content-Type: image/gif' does not match response body content.");
					}
				}
				else if (this.oResponse.headers.ExistsAndContains("Content-Type", "image/jpeg") && !Utilities.HasMagicBytes(this.responseBodyBytes, new byte[] { 255, 216 }))
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #M027] Declared 'Content-Type: image/jpeg' does not match response body content.");
				}
			}
			List<HTTPHeaderItem> listSetCookies = this.oResponse.headers.FindAll("Set-Cookie");
			if (listSetCookies.Count > 0)
			{
				if (this.hostname.Contains("_"))
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, "[HTTPLint #M020] Response sets a cookie, and server's hostname contains '_'. Internet Explorer does not permit cookies to be set on hostnames containing underscores. See http://support.microsoft.com/kb/316112");
				}
				foreach (HTTPHeaderItem oHI in listSetCookies)
				{
					string sAttrs = Utilities.TrimBefore(oHI.Value, ";");
					string sDomainAttr = Utilities.GetCommaTokenValue(sAttrs, "domain");
					if (!Utilities.IsNullOrWhiteSpace(sDomainAttr))
					{
						sDomainAttr = sDomainAttr.Trim();
						if (sDomainAttr.StartsWith("."))
						{
							sDomainAttr = sDomainAttr.Substring(1);
						}
						if (!this.hostname.EndsWith(sDomainAttr))
						{
							FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint #H021] Illegal DOMAIN in Set-Cookie. Cookie from '{0}' specified 'domain={1}'", this.hostname, sDomainAttr));
						}
					}
					string sCookie = Utilities.TrimAfter(oHI.Value, ';');
					foreach (char c in sCookie)
					{
						if (c == ',')
						{
							FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint #L022] Illegal comma in cookie. Set-Cookie: {0}.", sCookie));
						}
						else if (c >= '\u0080')
						{
							FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInResponse, false, false, string.Format("[HTTPLint #M023] Non-ASCII character found in Set-Cookie: {0}. Some browsers (Safari) may corrupt this cookie.", sCookie));
						}
					}
				}
			}
		}

		/// <summary>
		/// Assign a Session ID. Called by ClientChatter when headers are available
		/// </summary>
		// Token: 0x06000479 RID: 1145 RVA: 0x0002BF7C File Offset: 0x0002A17C
		[DoNotObfuscate]
		internal void _AssignID()
		{
			this.m_requestID = Interlocked.Increment(ref Session.cRequests);
		}

		// Token: 0x0600047A RID: 1146 RVA: 0x0002BF8E File Offset: 0x0002A18E
		internal void EnsureID()
		{
			if (this.m_requestID == 0)
			{
				this.m_requestID = Interlocked.Increment(ref Session.cRequests);
			}
		}

		/// <summary>
		/// Called only by InnerExecute, this method reads a request from the client and performs tampering/manipulation on it.
		/// </summary>
		/// <returns>TRUE if there's a Request object and we should continue processing. FALSE if reading the request failed
		/// *OR* if script or an extension changed the session's State to DONE or ABORTED.
		/// </returns>
		// Token: 0x0600047B RID: 1147 RVA: 0x0002BFA8 File Offset: 0x0002A1A8
		private bool _executeObtainRequest()
		{
			if (this.state > SessionStates.ReadingRequest)
			{
				this.Timers.ClientBeginRequest = (this.Timers.FiddlerGotRequestHeaders = (this.Timers.ClientDoneRequest = DateTime.Now));
				this._AssignID();
			}
			else
			{
				this.state = SessionStates.ReadingRequest;
				if (!this.oRequest.ReadRequest())
				{
					this._HandleFailedReadRequest();
					return false;
				}
				this.Timers.ClientDoneRequest = DateTime.Now;
				if (CONFIG.bDebugSpew)
				{
					FiddlerApplication.DebugSpew("Request for Session #{0} for read from {1}.", new object[]
					{
						this.m_requestID,
						this.oRequest.pipeClient
					});
				}
				try
				{
					this.requestBodyBytes = this.oRequest.TakeEntity();
				}
				catch (Exception eX)
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, true, false, "Failed to obtain request body. " + Utilities.DescribeException(eX));
					this.CloseSessionPipes(true);
					this.state = SessionStates.Aborted;
					return false;
				}
			}
			this._replaceVirtualHostnames();
			if (this.isHTTPS)
			{
				this.SetBitFlag(SessionFlags.IsHTTPS, true);
				this.SetBitFlag(SessionFlags.IsFTP, false);
			}
			else if (this.isFTP)
			{
				this.SetBitFlag(SessionFlags.IsFTP, true);
				this.SetBitFlag(SessionFlags.IsHTTPS, false);
			}
			this._smValidateRequest();
			this.state = SessionStates.AutoTamperRequestBefore;
			FiddlerApplication.DoBeforeRequest(this);
			if (FiddlerApplication._AutoResponder.IsEnabled)
			{
				FiddlerApplication._AutoResponder.DoMatchBeforeRequestTampering(this);
			}
			if (this.m_state >= SessionStates.Done)
			{
				this.FinishUISession();
				return false;
			}
			return true;
		}

		// Token: 0x0600047C RID: 1148 RVA: 0x0002C120 File Offset: 0x0002A320
		private void _smCheckForAutoReply()
		{
			if (FiddlerApplication._AutoResponder.IsEnabled && this.m_state < SessionStates.AutoTamperResponseBefore)
			{
				FiddlerApplication._AutoResponder.DoMatchAfterRequestTampering(this);
			}
		}

		// Token: 0x0600047D RID: 1149 RVA: 0x0002C144 File Offset: 0x0002A344
		private void _smValidateRequest()
		{
			if (Utilities.IsNullOrEmpty(this.requestBodyBytes) && Utilities.HTTPMethodRequiresBody(this.RequestMethod) && !this.isAnyFlagSet(SessionFlags.RequestStreamed | SessionFlags.IsRPCTunnel))
			{
				FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, true, false, "This HTTP method requires a request body.");
			}
			string sOriginalHostHeader = this.oFlags["X-Original-Host"];
			if (sOriginalHostHeader != null)
			{
				if (sOriginalHostHeader.Length < 1)
				{
					FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, false, false, "HTTP/1.1 Request was missing the required HOST header.");
					return;
				}
				if (!FiddlerApplication.Prefs.GetBoolPref("fiddler.network.SetHostHeaderFromURL", true))
				{
					this.oFlags["X-OverrideHost"] = this.oFlags["X-URI-Host"];
				}
				FiddlerApplication.HandleHTTPError(this, SessionFlags.ProtocolViolationInRequest, false, false, string.Format("The Request's Host header did not match the URL's host component.\n\nURL Host:\t{0}\nHeader Host:\t{1}", this.oFlags["X-URI-Host"], this.oFlags["X-Original-Host"]));
			}
		}

		/// <summary>
		/// If the executeObtainRequest called failed, we perform cleanup
		/// </summary>
		// Token: 0x0600047E RID: 1150 RVA: 0x0002C228 File Offset: 0x0002A428
		private void _HandleFailedReadRequest()
		{
			if (this.oRequest.headers == null)
			{
				this.oFlags["ui-hide"] = "stealth-NewOrReusedClosedWithoutRequest";
			}
			try
			{
				this.requestBodyBytes = this.oRequest.TakeEntity();
			}
			catch (Exception eX)
			{
				this.oFlags["X-FailedToReadRequestBody"] = Utilities.DescribeException(eX);
			}
			if (this.oResponse != null)
			{
				this.oResponse._detachServerPipe();
			}
			this.CloseSessionPipes(true);
			this.state = SessionStates.Aborted;
		}

		/// <summary>
		/// Returns TRUE if response is a NTLM or NEGO challenge
		/// </summary>
		/// <returns>True for HTTP/401,407 with NEGO or NTLM demand</returns>
		// Token: 0x0600047F RID: 1151 RVA: 0x0002C2B8 File Offset: 0x0002A4B8
		private bool _isResponseMultiStageAuthChallenge()
		{
			return Utilities.HasHeaders(this.oResponse) && ((401 == this.oResponse.headers.HTTPResponseCode && this.oResponse.headers["WWW-Authenticate"].OICStartsWith("N")) || (407 == this.oResponse.headers.HTTPResponseCode && this.oResponse.headers["Proxy-Authenticate"].OICStartsWith("N")));
		}

		/// <summary>
		/// Returns TRUE if response is a Digest, NTLM, or Nego challenge
		/// </summary>
		/// <returns>True for HTTP/401,407 with Digest, NEGO, NTLM demand</returns>
		// Token: 0x06000480 RID: 1152 RVA: 0x0002C348 File Offset: 0x0002A548
		private bool _isResponseAuthChallenge()
		{
			if (401 == this.oResponse.headers.HTTPResponseCode)
			{
				return this.oResponse.headers.ExistsAndContains("WWW-Authenticate", "NTLM") || this.oResponse.headers.ExistsAndContains("WWW-Authenticate", "Negotiate") || this.oResponse.headers.ExistsAndContains("WWW-Authenticate", "Digest");
			}
			return 407 == this.oResponse.headers.HTTPResponseCode && (this.oResponse.headers.ExistsAndContains("Proxy-Authenticate", "NTLM") || this.oResponse.headers.ExistsAndContains("Proxy-Authenticate", "Negotiate") || this.oResponse.headers.ExistsAndContains("Proxy-Authenticate", "Digest"));
		}

		/// <summary>
		/// Replace the "ipv*.fiddler "fake" hostnames with the IP-literal equvalents.
		/// </summary>
		// Token: 0x06000481 RID: 1153 RVA: 0x0002C430 File Offset: 0x0002A630
		private void _replaceVirtualHostnames()
		{
			if (!this.hostname.OICEndsWith(".fiddler"))
			{
				return;
			}
			string sInboundHost = this.hostname.ToLowerInvariant();
			if (!(sInboundHost == "ipv4.fiddler"))
			{
				if (!(sInboundHost == "localhost.fiddler"))
				{
					if (!(sInboundHost == "ipv6.fiddler"))
					{
						return;
					}
					this.hostname = "[::1]";
				}
				else
				{
					this.hostname = "localhost";
				}
			}
			else
			{
				this.hostname = "127.0.0.1";
			}
			this.oFlags["x-UsedVirtualHost"] = sInboundHost;
			this.bypassGateway = true;
			if (this.HTTPMethodIs("CONNECT"))
			{
				this.oFlags["x-OverrideCertCN"] = Utilities.StripIPv6LiteralBrackets(sInboundHost);
			}
		}

		/// <summary>
		/// Determines if request host is pointing directly at Fiddler.
		/// </summary>
		/// <returns></returns>
		// Token: 0x06000482 RID: 1154 RVA: 0x0002C4E4 File Offset: 0x0002A6E4
		private bool _isDirectRequestToFiddler()
		{
			if (this.port != CONFIG.ListenPort)
			{
				return false;
			}
			if (this.host.OICEquals(CONFIG.sFiddlerListenHostPort))
			{
				return true;
			}
			string _hostname = this.hostname.ToLowerInvariant();
			if (_hostname == "localhost" || _hostname == "localhost." || _hostname == CONFIG.sAlternateHostname)
			{
				return true;
			}
			if (_hostname.StartsWith("[") && _hostname.EndsWith("]"))
			{
				_hostname = _hostname.Substring(1, _hostname.Length - 2);
			}
			IPAddress ipTarget = Utilities.IPFromString(_hostname);
			if (ipTarget != null)
			{
				try
				{
					if (IPAddress.IsLoopback(ipTarget))
					{
						return true;
					}
					NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
					foreach (NetworkInterface networkInterface in networkInterfaces)
					{
						if (networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
						{
							IPInterfaceProperties properties = networkInterface.GetIPProperties();
							foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
							{
								if (ipTarget.Equals(ip.Address))
								{
									return true;
								}
							}
						}
					}
				}
				catch (Exception eX)
				{
				}
				return false;
			}
			return _hostname.StartsWith(CONFIG.sMachineName) && (_hostname.Length == CONFIG.sMachineName.Length || _hostname == CONFIG.sMachineName + "." + CONFIG.sMachineDomain);
		}

		/// <summary>
		/// Echo the client's request back as a HTTP Response, encoding to prevent XSS.
		/// </summary>
		// Token: 0x06000483 RID: 1155 RVA: 0x0002C66C File Offset: 0x0002A86C
		private void _returnEchoServiceResponse()
		{
			if (!FiddlerApplication.Prefs.GetBoolPref("fiddler.echoservice.enabled", true))
			{
				if (this.oRequest != null && this.oRequest.pipeClient != null)
				{
					this.oRequest.pipeClient.EndWithRST();
				}
				this.state = SessionStates.Aborted;
				return;
			}
			if (this.HTTPMethodIs("CONNECT"))
			{
				this.oRequest.FailSession(405, "Method Not Allowed", "This endpoint does not support HTTP CONNECTs. Try GET or POST instead.");
				return;
			}
			int iResultCode = 200;
			Action<Session> oDel = null;
			if (this.PathAndQuery.Length == 4 && Regex.IsMatch(this.PathAndQuery, "/\\d{3}"))
			{
				iResultCode = int.Parse(this.PathAndQuery.Substring(1));
				if (Utilities.IsRedirectStatus(iResultCode))
				{
					oDel = delegate(Session s)
					{
						s.oResponse["Location"] = "/200";
					};
				}
			}
			StringBuilder sEcho = new StringBuilder();
			sEcho.AppendFormat("<!doctype html>\n<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"><title>", Array.Empty<object>());
			if (iResultCode != 200)
			{
				sEcho.AppendFormat("[{0}] - ", iResultCode);
			}
			sEcho.Append("Fiddler Echo Service</title></head><body style=\"font-family: arial,sans-serif;\"><h1>Fiddler Echo Service</h1><br /><pre>");
			sEcho.Append(Utilities.HtmlEncode(this.oRequest.headers.ToString(true, true)));
			if (this.requestBodyBytes != null && (long)this.requestBodyBytes.Length > 0L)
			{
				sEcho.Append(Utilities.HtmlEncode(Encoding.UTF8.GetString(this.requestBodyBytes)));
			}
			sEcho.Append("</pre>");
			sEcho.AppendFormat("This page returned a <b>HTTP/{0}</b> response <br />", iResultCode);
			if (this.oFlags.ContainsKey("X-ProcessInfo"))
			{
				sEcho.AppendFormat("Originating Process Information: <code>{0}</code><br />", this.oFlags["X-ProcessInfo"]);
			}
			sEcho.Append("<hr />");
			if (this.fullUrl.Contains("troubleshooter.cgi"))
			{
				sEcho.Append("<h3>Alternate hostname test</h3>\n");
				sEcho.Append("<iframe src='http://ipv4.fiddler:" + CONFIG.ListenPort.ToString() + "/' width=300></iframe>");
				sEcho.Append("<iframe src='http://ipv6.fiddler:" + CONFIG.ListenPort.ToString() + "/' width=300></iframe>");
				sEcho.Append("<iframe src='http://localhost.fiddler:" + CONFIG.ListenPort.ToString() + "/' width=300></iframe>");
				sEcho.Append("<img src='http://www.example.com/' width=0 height=0 />");
			}
			else
			{
				sEcho.Append("<ul><li>To configure Fiddler as a reverse proxy instead of seeing this page, see <a href='" + CONFIG.GetRedirUrl("REVERSEPROXY") + "'>Reverse Proxy Setup</a><li>You can download the <a href=\"FiddlerRoot.cer\">FiddlerRoot certificate</a></ul>");
			}
			sEcho.Append("</body></html>");
			this.oRequest.BuildAndReturnResponse(iResultCode, "Fiddler Generated", sEcho.ToString(), oDel);
			this.state = SessionStates.Aborted;
		}

		/// <summary>
		/// Send a Proxy Configuration script back to the client.
		/// </summary>
		// Token: 0x06000484 RID: 1156 RVA: 0x0002C90C File Offset: 0x0002AB0C
		private void _returnPACFileResponse()
		{
			this.utilCreateResponseAndBypassServer();
			this.oResponse.headers["Content-Type"] = "application/x-ns-proxy-autoconfig";
			this.oResponse.headers["Cache-Control"] = "max-age=60";
			this.oResponse.headers["Connection"] = "close";
			this.utilSetResponseBody(FiddlerApplication.oProxy._GetPACScriptText());
			this.state = SessionStates.Aborted;
			FiddlerApplication.DoResponseHeadersAvailable(this);
			this.ReturnResponse(false);
		}

		/// <summary>
		/// Send a Proxy Configuration script back to WinHTTP, so that Fiddler can use an upstream proxy specified
		/// by a script on a fileshare. (WinHTTP only allows HTTP/HTTPS-hosted script files)
		/// </summary>
		// Token: 0x06000485 RID: 1157 RVA: 0x0002C994 File Offset: 0x0002AB94
		private void _returnUpstreamPACFileResponse()
		{
			this.utilCreateResponseAndBypassServer();
			this.oResponse.headers["Content-Type"] = "application/x-ns-proxy-autoconfig";
			this.oResponse.headers["Connection"] = "close";
			this.oResponse.headers["Cache-Control"] = "max-age=300";
			string sBody = FiddlerApplication.oProxy._GetUpstreamPACScriptText();
			if (string.IsNullOrEmpty(sBody))
			{
				this.responseCode = 404;
			}
			this.utilSetResponseBody(sBody);
			this.state = SessionStates.Aborted;
			this.ReturnResponse(false);
		}

		/// <summary>
		/// Send the Fiddler Root certificate back to the client
		/// </summary>
		// Token: 0x06000486 RID: 1158 RVA: 0x0002CA2C File Offset: 0x0002AC2C
		private static void _returnRootCert(Session oS)
		{
			oS.utilCreateResponseAndBypassServer();
			oS.oResponse.headers["Connection"] = "close";
			oS.oResponse.headers["Cache-Control"] = "max-age=0";
			byte[] arrRootCert = CertMaker.getRootCertBytes();
			if (arrRootCert != null)
			{
				oS.oResponse.headers["Content-Type"] = "application/x-x509-ca-cert";
				oS.responseBodyBytes = arrRootCert;
				oS.oResponse.headers["Content-Length"] = oS.responseBodyBytes.Length.ToString();
			}
			else
			{
				oS.responseCode = 404;
				oS.oResponse.headers["Content-Type"] = "text/html; charset=UTF-8";
				oS.utilSetResponseBody("No root certificate was found. Have you enabled HTTPS traffic decryption in Fiddler yet?".PadRight(512, ' '));
			}
			FiddlerApplication.DoResponseHeadersAvailable(oS);
			oS.ReturnResponse(false);
		}

		/// <summary>
		/// This method indicates to the client that a secure tunnel was created,
		/// without actually talking to an upstream server.
		///
		/// If Fiddler's AutoResponder is enabled, and that autoresponder denies passthrough,
		/// then Fiddler itself will always indicate "200 Connection Established" and wait for
		/// another request from the client. That subsequent request can then potentially be 
		/// handled by the AutoResponder engine.
		///
		/// BUG BUG: This occurs even if Fiddler isn't configured for HTTPS Decryption
		///
		/// </summary>
		/// <param name="sHostname">The hostname to use in the Certificate returned to the client</param>
		// Token: 0x06000487 RID: 1159 RVA: 0x0002CB10 File Offset: 0x0002AD10
		private void _ReturnSelfGeneratedCONNECTTunnel(string sHostname)
		{
			this.SetBitFlag(SessionFlags.ResponseGeneratedByFiddler | SessionFlags.IsDecryptingTunnel, true);
			this.oResponse.headers = new HTTPResponseHeaders();
			this.oResponse.headers.SetStatus(200, "DecryptEndpoint Created");
			this.oResponse.headers.Add("Timestamp", DateTime.Now.ToString("HH:mm:ss.fff"));
			this.oResponse.headers.Add("FiddlerGateway", "AutoResponder");
			this.oResponse.headers.Add("Connection", "close");
			this.responseBodyBytes = Encoding.UTF8.GetBytes("This is a Fiddler-generated response to the client's request for a CONNECT tunnel.\n\n");
			this.oFlags["ui-backcolor"] = "Lavender";
			this.oFlags.Remove("x-no-decrypt");
			FiddlerApplication.DoBeforeResponse(this);
			this.state = SessionStates.Done;
			FiddlerApplication.DoAfterSessionComplete(this);
			if (CONFIG.bUseSNIForCN && !this.oFlags.ContainsKey("x-OverrideCertCN"))
			{
				string sSNI = this.oFlags["https-Client-SNIHostname"];
				if (!string.IsNullOrEmpty(sSNI) && sSNI != sHostname)
				{
					this.oFlags["x-OverrideCertCN"] = this.oFlags["https-Client-SNIHostname"];
				}
			}
			string sCertCN = this.oFlags["x-OverrideCertCN"] ?? Utilities.StripIPv6LiteralBrackets(sHostname);
			if (this.oRequest.pipeClient == null || !this.oRequest.pipeClient.SecureClientPipe(sCertCN, this.oResponse.headers))
			{
				this.CloseSessionPipes(false);
				return;
			}
			Session oFauxSecureSession = new Session(this.oRequest.pipeClient, null);
			this.oRequest.pipeClient = null;
			oFauxSecureSession.oFlags["x-serversocket"] = "AUTO-RESPONDER-GENERATED";
			oFauxSecureSession.Execute(null);
		}

		/// <summary>
		/// This method adds a Proxy-Support: Session-Based-Authentication header and indicates whether the response is Nego:Type2.
		/// </summary>
		/// <returns>Returns TRUE if server returned a credible Type2 NTLM Message</returns>
		// Token: 0x06000488 RID: 1160 RVA: 0x0002CCE4 File Offset: 0x0002AEE4
		private bool _isNTLMType2()
		{
			if (!this.oFlags.ContainsKey("x-SuppressProxySupportHeader"))
			{
				this.oResponse.headers["Proxy-Support"] = "Session-Based-Authentication";
			}
			if (407 == this.oResponse.headers.HTTPResponseCode)
			{
				if (this.oRequest.headers["Proxy-Authorization"].Length < 1)
				{
					return false;
				}
				if (!this.oResponse.headers.Exists("Proxy-Authenticate") || this.oResponse.headers["Proxy-Authenticate"].Length < 6)
				{
					return false;
				}
			}
			else
			{
				if (string.IsNullOrEmpty(this.oRequest.headers["Authorization"]))
				{
					return false;
				}
				if (!this.oResponse.headers.Exists("WWW-Authenticate") || this.oResponse.headers["WWW-Authenticate"].Length < 6)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// This helper evaluates the conditions for client socket reuse.
		/// </summary>
		/// <returns></returns>
		// Token: 0x06000489 RID: 1161 RVA: 0x0002CDE0 File Offset: 0x0002AFE0
		private bool _MayReuseMyClientPipe()
		{
			return CONFIG.ReuseClientSockets && this._bAllowClientPipeReuse && !this.oResponse.headers.ExistsAndEquals("Connection", "close", false) && !this.oRequest.headers.ExistsAndEquals("Connection", "close", false) && !this.oResponse.headers.ExistsAndEquals("Proxy-Connection", "close", false) && !this.oRequest.headers.ExistsAndEquals("Proxy-Connection", "close", false) && (this.oResponse.headers.HTTPVersion == "HTTP/1.1" || this.oResponse.headers.ExistsAndContains("Connection", "Keep-Alive"));
		}

		/// <summary>
		/// Sends the Response that Fiddler received from the server back to the client socket.
		/// </summary>
		/// <param name="bForceClientServerPipeAffinity">Should the client and server pipes be tightly-bound together?</param>
		/// <returns>True, if the response was successfully sent to the client</returns>
		// Token: 0x0600048A RID: 1162 RVA: 0x0002CEB4 File Offset: 0x0002B0B4
		internal bool ReturnResponse(bool bForceClientServerPipeAffinity)
		{
			this.state = SessionStates.SendingResponse;
			bool result = false;
			this.Timers.ClientBeginResponse = (this.Timers.ClientDoneResponse = DateTime.Now);
			try
			{
				if (this.oRequest.pipeClient != null)
				{
					if (this.oFlags.ContainsKey("response-trickle-delay"))
					{
						int iDelayPerK = int.Parse(this.oFlags["response-trickle-delay"]);
						this.oRequest.pipeClient.TransmitDelay = iDelayPerK;
					}
					this.oRequest.pipeClient.Send(this.oResponse.headers.ToByteArray(true, true));
					if (this.responseBodyBytes == Utilities.emptyByteArray && !string.IsNullOrEmpty(this.__sResponseFileToStream))
					{
						using (FileStream file = File.OpenRead(this.__sResponseFileToStream))
						{
							byte[] buffer = new byte[65536];
							int bytesRead;
							while ((bytesRead = file.Read(buffer, 0, buffer.Length)) > 0)
							{
								this.oRequest.pipeClient.Send(buffer, 0, bytesRead);
							}
							goto IL_115;
						}
					}
					this.oRequest.pipeClient.Send(this.responseBodyBytes);
					IL_115:
					this.Timers.ClientDoneResponse = DateTime.Now;
					if (this.responseCode == 101 && Utilities.HasHeaders(this.oRequest) && this.oRequest.headers.ExistsAndContains("Upgrade", "WebSocket") && Utilities.HasHeaders(this.oResponse) && this.oResponse.headers.ExistsAndContains("Upgrade", "WebSocket"))
					{
						FiddlerApplication.DebugSpew("Upgrading Session #{0} to Websocket", new object[] { this.id });
						WebSocket.CreateTunnel(this);
						this.state = SessionStates.Done;
						this.FinishUISession();
						return true;
					}
					if (this.responseCode == 200 && this.HTTPMethodIs("CONNECT") && this.oRequest.pipeClient != null)
					{
						bForceClientServerPipeAffinity = true;
						if (this.isAnyFlagSet(SessionFlags.Ignored) || (this.oFlags.ContainsKey("x-no-decrypt") && this.oFlags.ContainsKey("x-no-parse")))
						{
							this.oFlags["x-CONNECT-Peek"] = "Skipped";
							StringDictionary stringDictionary = this.oFlags;
							stringDictionary["x-no-decrypt"] = stringDictionary["x-no-decrypt"] + "Skipped";
							stringDictionary = this.oFlags;
							stringDictionary["x-no-parse"] = stringDictionary["x-no-parse"] + "Skipped";
							FiddlerApplication.DebugSpew("Session #{0} set to act like a blind tunnel", new object[] { this.id });
							CONNECTTunnel.CreateTunnel(this);
							result = true;
							goto IL_62C;
						}
						FiddlerApplication.DebugSpew("Returned Session #{0} CONNECT's 200 response to client; sniffing for client data in tunnel", new object[] { this.id });
						Socket sockClient = this.oRequest.pipeClient.GetRawSocket();
						if (sockClient != null)
						{
							byte[] arrTmp = new byte[1024];
							int iCNT = sockClient.Receive(arrTmp, SocketFlags.Peek);
							if (iCNT == 0)
							{
								this.oFlags["x-CONNECT-Peek"] = "After the client received notice of the established CONNECT, it failed to send any data.";
								this.requestBodyBytes = Encoding.UTF8.GetBytes("After the client received notice of the established CONNECT, it failed to send any data.\n");
								if (this.isFlagSet(SessionFlags.SentToGateway))
								{
									this.PoisonServerPipe();
								}
								this.PoisonClientPipe();
								this.oRequest.pipeClient.End();
								result = true;
								goto IL_62C;
							}
							if (CONFIG.bDebugSpew)
							{
								FiddlerApplication.DebugSpew("Peeking at the first bytes from CONNECT'd client session {0} yielded:\n{1}", new object[]
								{
									this.id,
									Utilities.ByteArrayToHexView(arrTmp, 32, iCNT)
								});
							}
							bool bSmellsLikeHTTPS = arrTmp[0] == 22 || arrTmp[0] == 128;
							if (bSmellsLikeHTTPS)
							{
								FiddlerApplication.DebugSpew("Session [{0}] looks like a HTTPS tunnel!", new object[] { this.id });
								try
								{
									HTTPSClientHello oHello = new HTTPSClientHello();
									if (oHello.LoadFromStream(new MemoryStream(arrTmp, 0, iCNT, false)))
									{
										this.requestBodyBytes = Encoding.UTF8.GetBytes(oHello.ToString() + "\n");
										this["https-Client-SessionID"] = oHello.SessionID;
										if (!string.IsNullOrEmpty(oHello.ServerNameIndicator))
										{
											this["https-Client-SNIHostname"] = oHello.ServerNameIndicator;
										}
									}
								}
								catch (Exception eX)
								{
								}
								CONNECTTunnel.CreateTunnel(this);
								result = true;
								goto IL_62C;
							}
							bool bSmellsLikeHTTPOverCONNECT = iCNT > 4 && ((arrTmp[0] == 71 && arrTmp[1] == 69 && arrTmp[2] == 84 && arrTmp[3] == 32) || (arrTmp[0] == 80 && arrTmp[1] == 79 && arrTmp[2] == 83 && arrTmp[3] == 84) || (arrTmp[0] == 80 && arrTmp[1] == 85 && arrTmp[2] == 84 && arrTmp[3] == 32) || (arrTmp[0] == 72 && arrTmp[1] == 69 && arrTmp[2] == 65 && arrTmp[3] == 68));
							if (!bSmellsLikeHTTPOverCONNECT)
							{
								FiddlerApplication.DebugSpew("Session [{0}] CONNECT Peek yielded unknown protocol!", new object[] { this.id });
								this.oFlags["x-CONNECT-Peek"] = BitConverter.ToString(arrTmp, 0, Math.Min(iCNT, 16));
								this.oFlags["x-no-decrypt"] = "PeekYieldedUnknownProtocol";
								CONNECTTunnel.CreateTunnel(this);
								result = true;
								goto IL_62C;
							}
							FiddlerApplication.DebugSpew("Session [{0}] looks like it's going to be an unencrypted WebSocket tunnel!", new object[] { this.id });
							this.SetBitFlag(SessionFlags.IsRPCTunnel, true);
						}
					}
					bool bMayReuseClientSocket = bForceClientServerPipeAffinity || this._MayReuseMyClientPipe();
					if (bMayReuseClientSocket)
					{
						FiddlerApplication.DebugSpew("Creating next session with pipes from {0}.", new object[] { this.id });
						this._createNextSession(bForceClientServerPipeAffinity);
						result = true;
					}
					else
					{
						if (CONFIG.bDebugSpew)
						{
							FiddlerApplication.DebugSpew("fiddler.network.clientpipereuse> Closing client socket since bReuseClientSocket was false after returning [{0}]", new object[] { this.url });
						}
						this.oRequest.pipeClient.End();
						result = true;
					}
				}
				else
				{
					result = true;
				}
			}
			catch (Exception eX2)
			{
				if (CONFIG.bDebugSpew)
				{
					FiddlerApplication.DebugSpew("Write to client failed for Session #{0}; exception was {1}", new object[]
					{
						this.id,
						eX2.ToString()
					});
				}
				this.state = SessionStates.Aborted;
				this.PoisonClientPipe();
			}
			IL_62C:
			this.oRequest.pipeClient = null;
			if (result)
			{
				this.state = SessionStates.Done;
				try
				{
					this.FinishUISession();
				}
				catch (Exception eX3)
				{
				}
			}
			FiddlerApplication.DoAfterSessionComplete(this);
			if (this.oFlags.ContainsKey("log-drop-response-body") && !Utilities.IsNullOrEmpty(this.responseBodyBytes))
			{
				this.oFlags["x-ResponseBodyFinalLength"] = ((long)this.responseBodyBytes.Length).ToString("N0");
				this.SetBitFlag(SessionFlags.ResponseBodyDropped, true);
				this.responseBodyBytes = Utilities.emptyByteArray;
			}
			if (this.oFlags.ContainsKey("log-drop-request-body") && !Utilities.IsNullOrEmpty(this.requestBodyBytes))
			{
				this.oFlags["x-RequestBodyFinalLength"] = ((long)this.requestBodyBytes.Length).ToString("N0");
				this.SetBitFlag(SessionFlags.RequestBodyDropped, true);
				this.requestBodyBytes = Utilities.emptyByteArray;
			}
			return result;
		}

		/// <summary>
		/// Sets up the next Session on these pipes, binding this Session's pipes to that new Session, as appropriate. When this method is called,
		/// the nextSession variable is populated with the new Session, and that object is executed at the appropriate time.
		/// </summary>
		/// <param name="bForceClientServerPipeAffinity">TRUE if both the client and server pipes should be bound regardless of the serverPipe's ReusePolicy</param>
		// Token: 0x0600048B RID: 1163 RVA: 0x0002D638 File Offset: 0x0002B838
		private void _createNextSession(bool bForceClientServerPipeAffinity)
		{
			if (this.oResponse != null && this.oResponse.pipeServer != null && (bForceClientServerPipeAffinity || this.oResponse.pipeServer.ReusePolicy == PipeReusePolicy.MarriedToClientPipe || this.oFlags.ContainsKey("X-ClientServerPipeAffinity")))
			{
				this.nextSession = new Session(this.oRequest.pipeClient, this.oResponse.pipeServer);
				this.oResponse.pipeServer = null;
				return;
			}
			this.nextSession = new Session(this.oRequest.pipeClient, null);
		}

		// Token: 0x0600048C RID: 1164 RVA: 0x0002D6C7 File Offset: 0x0002B8C7
		internal void FinishUISession()
		{
		}

		// Token: 0x0600048D RID: 1165 RVA: 0x0002D6CC File Offset: 0x0002B8CC
		private void RaiseSessionCreated()
		{
			EventHandler<Session> handler = Session.SessionCreated;
			if (handler != null)
			{
				handler(this, this);
			}
		}

		// Token: 0x0600048E RID: 1166 RVA: 0x0002D6EC File Offset: 0x0002B8EC
		internal void RaiseSessionFieldChanged()
		{
			EventHandler<Session> handler = Session.SessionFieldChanged;
			if (handler != null)
			{
				handler(this, this);
			}
		}

		// Token: 0x040001C3 RID: 451
		[DoNotObfuscate]
		internal bool isReceivedByFiddlerOrchestra;

		/// <summary>
		/// Should we try to use the SPNToken type?
		/// Cached for performance reasons.
		/// ISSUE: It's technically possible to use FiddlerCorev2/v3 on .NET/4.5 but we won't set this field if you do that.
		/// </summary>
		// Token: 0x040001C4 RID: 452
		private static bool bTrySPNTokenObject = true;

		/// <summary>
		/// Sorta hacky, we may use a .NET WebRequest object to generate a valid NTLM/Kerberos response if the server
		/// demands authentication and the Session is configured to automatically respond.
		/// </summary>
		// Token: 0x040001C5 RID: 453
		private WebRequest __WebRequestForAuth;

		/// <summary>
		/// Used if the Session is bound to a WebSocket or CONNECTTunnel
		/// </summary>
		// Token: 0x040001C6 RID: 454
		public ITunnel __oTunnel;

		/// <summary>
		/// File to stream if responseBodyBytes is null
		/// </summary>
		// Token: 0x040001C7 RID: 455
		private string __sResponseFileToStream;

		// Token: 0x040001C8 RID: 456
		private SessionFlags _bitFlags;

		// Token: 0x040001C9 RID: 457
		private static int cRequests;

		/// <summary>
		/// When a client socket is reused, this field holds the next Session until its execution begins
		/// </summary>
		// Token: 0x040001CA RID: 458
		private Session nextSession;

		/// <summary>
		/// Should response be buffered for tampering.
		/// </summary>
		/// <remarks>ARCH: This should have been a property instead of a field, so we could throw an InvalidStateException if code tries to manipulate this value after the response has begun</remarks>
		// Token: 0x040001CB RID: 459
		public bool bBufferResponse = FiddlerApplication.Prefs.GetBoolPref("fiddler.ui.rules.bufferresponses", false);

		/// <summary>
		/// Timers stored as this Session progresses
		/// </summary>
		// Token: 0x040001CC RID: 460
		public SessionTimers Timers = new SessionTimers();

		// Token: 0x040001CD RID: 461
		private SessionStates m_state;

		// Token: 0x040001CE RID: 462
		private bool _bypassGateway;

		// Token: 0x040001CF RID: 463
		private int m_requestID;

		// Token: 0x040001D0 RID: 464
		private int _LocalProcessID;

		// Token: 0x040001D1 RID: 465
		public object ViewItem;

		/// <summary>
		/// Field is set to False if socket is poisoned due to HTTP errors.
		/// </summary>
		// Token: 0x040001D2 RID: 466
		private bool _bAllowClientPipeReuse = true;

		/// <summary>
		/// Object representing the HTTP Response.
		/// </summary>
		// Token: 0x040001D4 RID: 468
		[CodeDescription("Object representing the HTTP Response.")]
		public ServerChatter oResponse;

		/// <summary>
		/// Object representing the HTTP Request.
		/// </summary>
		// Token: 0x040001D5 RID: 469
		[CodeDescription("Object representing the HTTP Request.")]
		public ClientChatter oRequest;

		/// <summary>
		/// Fiddler-internal flags set on the Session.
		/// </summary>
		/// <remarks>TODO: ARCH: This shouldn't be exposed directly; it should be wrapped by a ReaderWriterLockSlim to prevent
		/// exceptions while enumerating the flags for storage, etc</remarks>
		// Token: 0x040001D6 RID: 470
		[CodeDescription("Fiddler-internal flags set on the session.")]
		public readonly StringDictionary oFlags = new StringDictionary();

		/// <summary>
		/// Contains the bytes of the request body.
		/// </summary>
		// Token: 0x040001D8 RID: 472
		[CodeDescription("Contains the bytes of the request body.")]
		public byte[] requestBodyBytes;

		/// <summary>
		/// Contains the bytes of the response body.
		/// </summary>
		// Token: 0x040001D9 RID: 473
		[CodeDescription("Contains the bytes of the response body.")]
		public byte[] responseBodyBytes;

		/// <summary>
		/// IP Address of the client for this session.
		/// </summary>
		// Token: 0x040001DA RID: 474
		[CodeDescription("IP Address of the client for this session.")]
		public string m_clientIP;

		/// <summary>
		/// Client port attached to Fiddler.
		/// </summary>
		// Token: 0x040001DB RID: 475
		[CodeDescription("Client port attached to Fiddler.")]
		public int m_clientPort;

		/// <summary>
		/// IP Address of the server for this session.
		/// </summary>
		// Token: 0x040001DC RID: 476
		[CodeDescription("IP Address of the server for this session.")]
		public string m_hostIP;

		/// <summary>
		/// Event object used for pausing and resuming the thread servicing this session
		/// </summary>
		// Token: 0x040001DD RID: 477
		private AutoResetEvent oSyncEvent;

		// Token: 0x040001E1 RID: 481
		private static Dictionary<string, int> _sessionProcessInfoToPPID = new Dictionary<string, int>();

		// Token: 0x040001E2 RID: 482
		private static EventHandler beforeSessionCounterReset;

		/// <summary>
		/// Current step in the SessionProcessing State Machine
		/// </summary>
		// Token: 0x040001E3 RID: 483
		private ProcessingStates _pState;

		// Token: 0x040001E4 RID: 484
		private bool bLeakedResponseAlready;
	}
}
