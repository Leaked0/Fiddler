using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using FiddlerCore.Common.Rules;

namespace Fiddler
{
	/// <summary>
	/// The AutoResponder object manages automatic responses to requests.
	/// </summary>
	// Token: 0x02000009 RID: 9
	public class AutoResponder
	{
		/// <summary>
		/// Describes the contents of the AutoResponder list
		/// </summary>
		/// <returns>Multi-line string containing rules.</returns>
		// Token: 0x0600007B RID: 123 RVA: 0x00003F90 File Offset: 0x00002190
		public override string ToString()
		{
			StringBuilder slResult = new StringBuilder();
			try
			{
				this.GetReaderLock();
				slResult.AppendFormat("The AutoResponder list contains {0} rules.\r\n", this.Rules.Count);
				foreach (ResponderRule oItem in this.Rules)
				{
					slResult.AppendFormat("\t{0}\t->\t{1}\r\n", oItem.sMatch, oItem.sAction);
				}
			}
			finally
			{
				this.FreeReaderLock();
			}
			return slResult.ToString();
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00004038 File Offset: 0x00002238
		private void GetReaderLock()
		{
			this._RWLockRules.EnterReadLock();
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00004045 File Offset: 0x00002245
		private void FreeReaderLock()
		{
			this._RWLockRules.ExitReadLock();
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00004052 File Offset: 0x00002252
		private void GetWriterLock()
		{
			this._RWLockRules.EnterWriteLock();
		}

		// Token: 0x0600007F RID: 127 RVA: 0x0000405F File Offset: 0x0000225F
		private void FreeWriterLock()
		{
			this._RWLockRules.ExitWriteLock();
		}

		/// <summary>
		/// True if the AutoResponder feature is Enabled; false otherwise.
		/// </summary>
		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000080 RID: 128 RVA: 0x0000406C File Offset: 0x0000226C
		// (set) Token: 0x06000081 RID: 129 RVA: 0x00004074 File Offset: 0x00002274
		public bool IsEnabled
		{
			get
			{
				return this.isEnabled;
			}
			set
			{
				if (this.isEnabled == value)
				{
					return;
				}
				this.isEnabled = value;
				this._bRuleListIsDirty = true;
				this.enabledSubject.OnNext(this.isEnabled);
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000082 RID: 130 RVA: 0x0000409F File Offset: 0x0000229F
		public IObservable<bool> Enabled { get; }

		/// <summary>
		/// TRUE if requests that match no AutoResponder rule are permitted to proceed to the network
		/// </summary>
		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000083 RID: 131 RVA: 0x000040A7 File Offset: 0x000022A7
		// (set) Token: 0x06000084 RID: 132 RVA: 0x000040AF File Offset: 0x000022AF
		public bool PermitFallthrough
		{
			get
			{
				return this.permitFallthrough;
			}
			set
			{
				if (this.permitFallthrough == value)
				{
					return;
				}
				this.permitFallthrough = value;
				this._bRuleListIsDirty = true;
				this.unmatchedRequestsPassthroughSubject.OnNext(this.permitFallthrough);
			}
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000085 RID: 133 RVA: 0x000040DA File Offset: 0x000022DA
		public IObservable<bool> UnmatchedRequestsPassthrough { get; }

		/// <summary>
		/// TRUE if AutoResponder should respond to CONNECTs with 200
		/// </summary>
		// Token: 0x1700001F RID: 31
		// (get) Token: 0x06000086 RID: 134 RVA: 0x000040E2 File Offset: 0x000022E2
		// (set) Token: 0x06000087 RID: 135 RVA: 0x000040EA File Offset: 0x000022EA
		public bool AcceptAllConnects
		{
			get
			{
				return this.acceptAllConnects;
			}
			set
			{
				if (this.acceptAllConnects == value)
				{
					return;
				}
				this.acceptAllConnects = value;
				this._bRuleListIsDirty = true;
				this.acceptAllConnectsSubject.OnNext(this.acceptAllConnects);
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000088 RID: 136 RVA: 0x00004115 File Offset: 0x00002315
		public IObservable<bool> AcceptAllConnectsObservable { get; }

		/// <summary>
		/// Should per-rule latency values be used?
		/// </summary>
		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000089 RID: 137 RVA: 0x0000411D File Offset: 0x0000231D
		// (set) Token: 0x0600008A RID: 138 RVA: 0x00004125 File Offset: 0x00002325
		public bool UseLatency
		{
			get
			{
				return this._bUseLatency;
			}
			set
			{
				if (value == this._bUseLatency)
				{
					return;
				}
				this._bUseLatency = value;
				this._bRuleListIsDirty = true;
			}
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00004140 File Offset: 0x00002340
		internal AutoResponder()
		{
			this.enabledSubject = new BehaviorSubject<bool>(this.IsEnabled);
			this.Enabled = this.enabledSubject.AsObservable<bool>();
			this.unmatchedRequestsPassthroughSubject = new BehaviorSubject<bool>(this.PermitFallthrough);
			this.UnmatchedRequestsPassthrough = this.unmatchedRequestsPassthroughSubject.AsObservable<bool>();
			this.acceptAllConnectsSubject = new BehaviorSubject<bool>(this.AcceptAllConnects);
			this.AcceptAllConnectsObservable = this.acceptAllConnectsSubject.AsObservable<bool>();
			try
			{
				AutoResponder.sColorAutoResponded = FiddlerApplication.Prefs.GetStringPref("fiddler.ui.Colors.AutoResponded", "Lavender");
			}
			catch
			{
			}
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00004210 File Offset: 0x00002410
		internal void CreateRulesForFolder(string sFolderName)
		{
			DirectoryInfo oDir = new DirectoryInfo(sFolderName);
			FileInfo[] oFiles = oDir.GetFiles("*", SearchOption.AllDirectories);
			foreach (FileInfo oF in oFiles)
			{
				this.CreateRuleForFile(oF.FullName, oDir.Parent.FullName);
			}
		}

		/// <summary>
		/// Creates a new rule for a single file
		/// </summary>
		/// <param name="sFilename">The file to generate the rule for</param>
		/// <returns></returns>
		// Token: 0x0600008D RID: 141 RVA: 0x00004260 File Offset: 0x00002460
		internal bool CreateRuleForFile(string sFilename, string sRelativeTo)
		{
			if (sFilename == null || !File.Exists(sFilename))
			{
				return false;
			}
			bool result;
			try
			{
				string sLeafFilename;
				if (string.IsNullOrEmpty(sRelativeTo))
				{
					sLeafFilename = Path.GetFileName(sFilename);
				}
				else
				{
					sLeafFilename = sFilename.Substring(sRelativeTo.Length).Replace('\\', '/');
				}
				sLeafFilename = Utilities.UrlPathEncode(sLeafFilename);
				bool bIsCGI = sLeafFilename.OICEndsWithAny(new string[] { ".htm", ".html", ".php", ".cgi", ".asp", ".cfm", ".aspx" });
				string sMatch = "REGEX:(?inx).*" + Utilities.RegExEscape(sLeafFilename, false, !bIsCGI);
				if (bIsCGI)
				{
					sMatch += "(\\?.*)?$";
				}
				this.AddRule(sMatch, sFilename, "Rule from file " + Regex.Replace(sLeafFilename, "[^a-zA-Z0-9_.-]", "-"), true);
				result = true;
			}
			catch (Exception eX)
			{
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Imports the sessions from a Session Archive Zip file, creating one responder rule for each request
		/// </summary>
		/// <param name="sFilename">The filename of the SAZ File</param>
		/// <returns>FALSE if there was an error in loading</returns>
		// Token: 0x0600008E RID: 142 RVA: 0x0000435C File Offset: 0x0000255C
		public bool ImportSAZ(string sFilename)
		{
			return this.ImportSAZ(sFilename, false);
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00004368 File Offset: 0x00002568
		public bool ImportSAZ(string sFilename, bool bUsePlaybackHeuristics)
		{
			Session[] oNewRules = Utilities.ReadSessionArchive(sFilename);
			if (oNewRules == null)
			{
				return false;
			}
			if (sFilename.OICStartsWith(CONFIG.GetPath("Captures")))
			{
				sFilename = sFilename.Substring(CONFIG.GetPath("Captures").Length);
			}
			return this.ImportSessions(oNewRules, sFilename, bUsePlaybackHeuristics);
		}

		/// <summary>
		/// Create rules based on a set of existing sessions
		/// </summary>
		/// <param name="oSessions"></param>
		/// <returns></returns>
		// Token: 0x06000090 RID: 144 RVA: 0x000043B3 File Offset: 0x000025B3
		public bool ImportSessions(Session[] oSessions)
		{
			return this.ImportSessions(oSessions, null, false);
		}

		/// <summary>
		/// Imports sessions for replay
		/// </summary>
		/// <param name="oSessions">The set of Sessions</param>
		/// <param name="sAnnotation">The annotation to add to the UI display for the Session</param>
		/// <param name="bUsePlaybackHeuristics">Should 401s be filtered out?</param>
		/// <returns>TRUE if import succeeded.</returns>
		// Token: 0x06000091 RID: 145 RVA: 0x000043C0 File Offset: 0x000025C0
		private bool ImportSessions(Session[] oSessions, string sAnnotation, bool bUsePlaybackHeuristics)
		{
			if (oSessions == null || oSessions.Length < 1)
			{
				return false;
			}
			Dictionary<string, List<HTTPHeaderItem>> dictSavedSetCookies = null;
			foreach (Session oSessionRule in oSessions)
			{
				if (oSessionRule.HTTPMethodIs("CONNECT") && 200 == oSessionRule.responseCode)
				{
					if (bUsePlaybackHeuristics)
					{
						string sRule = "METHOD:CONNECT " + oSessionRule.fullUrl;
						if (this.Rules.Find((ResponderRule oCandidateRule) => oCandidateRule.sMatch == sRule) == null)
						{
							this.AddRule(sRule, "*ReplyWithTunnel", "Rule from CONNECT Tunnel", true);
						}
					}
				}
				else if (oSessionRule.bHasResponse && oSessionRule.oResponse != null)
				{
					if (bUsePlaybackHeuristics && 401 == oSessionRule.responseCode && oSessionRule.oResponse.headers.Exists("WWW-Authenticate"))
					{
						if (oSessionRule.oResponse.headers.Exists("Set-Cookie"))
						{
							if (dictSavedSetCookies == null)
							{
								dictSavedSetCookies = new Dictionary<string, List<HTTPHeaderItem>>();
							}
							dictSavedSetCookies[oSessionRule.fullUrl] = oSessionRule.oResponse.headers.FindAll("Set-Cookie");
						}
					}
					else
					{
						string sMatch;
						if (bUsePlaybackHeuristics && oSessionRule.HTTPMethodIs("POST") && Utilities.HasHeaders(oSessionRule.oRequest) && oSessionRule.oRequest.headers.Exists("SOAPAction"))
						{
							sMatch = "Header:SOAPAction=" + oSessionRule.oRequest.headers["SOAPAction"];
						}
						else if (!oSessionRule.HTTPMethodIs("GET"))
						{
							sMatch = string.Format("METHOD:{0} EXACT:{1}", oSessionRule.RequestMethod, oSessionRule.fullUrl);
						}
						else
						{
							sMatch = "EXACT:" + oSessionRule.fullUrl;
						}
						string sDescription = string.Format("*{0}-{1}", oSessionRule.responseCode, (sAnnotation == null) ? ("SESSION_" + oSessionRule.id.ToString()) : (sAnnotation + "#" + oSessionRule.oFlags["x-LoadedFrom"]));
						int iLatency = 0;
						if (oSessionRule.Timers != null)
						{
							iLatency = (int)(oSessionRule.Timers.ServerBeginResponse - oSessionRule.Timers.ClientDoneRequest).TotalMilliseconds;
						}
						byte[] arrNewBody = Utilities.Dupe(oSessionRule.responseBodyBytes);
						HTTPResponseHeaders oNewHeaders = (HTTPResponseHeaders)oSessionRule.oResponse.headers.Clone();
						List<HTTPHeaderItem> listSavedSetCookies;
						if (dictSavedSetCookies != null && dictSavedSetCookies.TryGetValue(oSessionRule.fullUrl, out listSavedSetCookies))
						{
							dictSavedSetCookies.Remove(oSessionRule.fullUrl);
							foreach (HTTPHeaderItem oHI in listSavedSetCookies)
							{
								oNewHeaders.Add(oHI.Name, oHI.Value);
							}
						}
						if (bUsePlaybackHeuristics)
						{
							bool bCleanupEarlierGETRules = !oSessionRule.HTTPMethodIs("GET");
							foreach (ResponderRule priorRule in this.Rules)
							{
								if (priorRule.sMatch == sMatch)
								{
									priorRule.bDisableOnMatch = true;
								}
								else if (bCleanupEarlierGETRules && priorRule.sMatch == "EXACT:" + oSessionRule.fullUrl)
								{
									priorRule.sMatch = "METHOD:GET " + priorRule.sMatch;
								}
							}
						}
						string comment = (string.IsNullOrEmpty(oSessionRule.fullUrl) ? "" : Regex.Replace(oSessionRule.fullUrl, "[^a-zA-Z0-9_.-]", "-"));
						ResponderRule oRule = this.AddRule(sMatch, oNewHeaders, arrNewBody, sDescription, "Rule from Url " + comment, iLatency, true);
					}
				}
			}
			this._bRuleListIsDirty = true;
			return true;
		}

		/// <summary>
		/// Clear all rules from the current AutoResponder list
		/// </summary>
		// Token: 0x06000092 RID: 146 RVA: 0x00004798 File Offset: 0x00002998
		public void ClearRules()
		{
			try
			{
				this.GetWriterLock();
				this.Rules.Clear();
				this.groups.Clear();
			}
			finally
			{
				this.FreeWriterLock();
			}
			this._bRuleListIsDirty = true;
		}

		/// <summary>
		/// Load options and rules from default XML file
		/// </summary>
		/// <param name="maxEnabled">How many rules can be enabled at the same time with 0 meaning no limit.</param>
		// Token: 0x06000093 RID: 147 RVA: 0x000047E4 File Offset: 0x000029E4
		internal void LoadRules(long maxEnabled = 0L)
		{
			this.LoadRules(CONFIG.GetPath("AutoResponderDefaultRules"), true, maxEnabled);
		}

		/// <summary>
		/// Import rules from a Fiddler AutoResponder Rules XML file
		/// </summary>
		/// <param name="sFilename">The name of the file</param>
		/// <param name="maxEnabled">How many rules can be enabled at the same time with 0 meaning no limit.</param>
		/// <returns>TRUE if the load was successful</returns>
		// Token: 0x06000094 RID: 148 RVA: 0x000047F9 File Offset: 0x000029F9
		internal bool ImportFARX(string sFilename, long maxEnabled = 0L)
		{
			return this.LoadRules(sFilename, false, maxEnabled);
		}

		/// <summary>
		/// Load options and Rules from an XML File
		/// </summary>
		/// <param name="sFilename">The name of the file</param>
		/// <param name="maxEnabled">How many rules can be enabled at the same time with 0 meaning no limit.</param>
		/// <returns>TRUE if the load was successful</returns>
		// Token: 0x06000095 RID: 149 RVA: 0x00004804 File Offset: 0x00002A04
		public bool LoadRules(string sFilename, long maxEnabled)
		{
			return this.LoadRules(sFilename, true, maxEnabled);
		}

		/// <summary>
		/// Load a set of rules from an XML File
		/// </summary>
		/// <param name="sFilename">The name of the file</param>
		/// <param name="bIsDefaultRuleFile">TRUE if the OPTIONS should be respected</param>
		/// <param name="maxEnabled">How many rules can be enabled at the same time with 0 meaning no limit.</param>
		/// <returns>TRUE if the load was successful</returns>
		// Token: 0x06000096 RID: 150 RVA: 0x0000480F File Offset: 0x00002A0F
		public bool LoadRules(string sFilename, bool bIsDefaultRuleFile, long maxEnabled = 0L)
		{
			return this.LoadRules(sFilename, bIsDefaultRuleFile, bIsDefaultRuleFile, maxEnabled);
		}

		/// <summary>
		/// Load a set of rules from an XML File
		/// </summary>
		/// <param name="sFilename">The name of the file</param>
		/// <param name="resetRules">TRUE if the rules should be cleaned before importing</param>
		/// <param name="respectOptions">TRUE if the OPTIONS should be respected from the provided XML file</param>
		/// <param name="maxEnabled">How many rules can be enabled at the same time with 0 meaning no limit.</param>
		/// <returns>TRUE if the load was successful</returns>
		// Token: 0x06000097 RID: 151 RVA: 0x0000481C File Offset: 0x00002A1C
		public bool LoadRules(string sFilename, bool resetRules, bool respectOptions, long maxEnabled = 0L)
		{
			if (resetRules)
			{
				this.ClearRules();
			}
			bool result;
			try
			{
				if (!File.Exists(sFilename) || new FileInfo(sFilename).Length < 143L)
				{
					result = false;
				}
				else
				{
					FileStream strmRules;
					try
					{
						strmRules = new FileStream(sFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
					}
					catch (Exception eIOX)
					{
						FiddlerApplication.Log.LogFormat("{0}: {1}\n{2}", new object[]
						{
							"AutoResponder Rules Unreadable",
							sFilename,
							eIOX.ToString()
						});
						if (resetRules)
						{
							this.IsEnabled = false;
						}
						return false;
					}
					using (strmRules)
					{
						XmlTextReader oXML = new XmlTextReader(strmRules);
						oXML.WhitespaceHandling = WhitespaceHandling.None;
						while (oXML.Read())
						{
							XmlNodeType nodeType = oXML.NodeType;
							if (nodeType == XmlNodeType.Element)
							{
								string name = oXML.Name;
								if (!(name == "State"))
								{
									if (!(name == "ResponseGroup"))
									{
										if (!(name == "ResponseRule"))
										{
											continue;
										}
									}
									else
									{
										try
										{
											string id = oXML.GetAttribute("Id");
											string header = oXML.GetAttribute("Header");
											this.AddGroup(id, header);
											continue;
										}
										catch
										{
											continue;
										}
									}
									try
									{
										string groupId = oXML.GetAttribute("GroupId");
										string sMatch = oXML.GetAttribute("Match");
										string sAction = oXML.GetAttribute("Action");
										int iLatency = 0;
										string sTemp = oXML.GetAttribute("Latency");
										if (sTemp != null)
										{
											iLatency = XmlConvert.ToInt32(sTemp);
										}
										string sComment = oXML.GetAttribute("Comment");
										if (!string.IsNullOrEmpty(sComment))
										{
										}
										bool bDisableAfterMatch = false;
										sTemp = oXML.GetAttribute("DisableAfterMatch");
										if ("true" == sTemp)
										{
											bDisableAfterMatch = true;
										}
										bool bRuleEnabled = "false" != oXML.GetAttribute("Enabled");
										if (bRuleEnabled && maxEnabled > 0L)
										{
											if (maxEnabled <= (long)this.Rules.FindAll((ResponderRule rule) => rule.IsEnabled).Count)
											{
												bRuleEnabled = false;
											}
										}
										string sHeaders = oXML.GetAttribute("Headers");
										ResponderRule oRR;
										if (string.IsNullOrEmpty(sHeaders))
										{
											oRR = this.AddRule(sMatch, null, null, sAction, sComment, iLatency, bRuleEnabled);
											oRR.bDisableOnMatch = bDisableAfterMatch;
										}
										else
										{
											HTTPResponseHeaders oRH = new HTTPResponseHeaders();
											sHeaders = Encoding.UTF8.GetString(Convert.FromBase64String(sHeaders));
											oRH.AssignFromString(sHeaders);
											string sBody = oXML.GetAttribute("DeflatedBody");
											byte[] arrRBB;
											if (!string.IsNullOrEmpty(sBody))
											{
												arrRBB = Utilities.DeflaterExpand(Convert.FromBase64String(sBody));
											}
											else
											{
												sBody = oXML.GetAttribute("Body");
												if (!string.IsNullOrEmpty(sBody))
												{
													arrRBB = Convert.FromBase64String(sBody);
												}
												else
												{
													arrRBB = Utilities.emptyByteArray;
												}
											}
											oRR = this.AddRule(sMatch, oRH, arrRBB, sAction, sComment, iLatency, bRuleEnabled);
											oRR.bDisableOnMatch = bDisableAfterMatch;
										}
										ResponderGroup group;
										if (!string.IsNullOrEmpty(groupId) && this.groups.TryGetValue(groupId, out group))
										{
											group.AddRule(oRR);
										}
									}
									catch
									{
									}
								}
								else if (respectOptions)
								{
									this.IsEnabled = "true" == oXML.GetAttribute("Enabled");
									this.AcceptAllConnects = "true" == oXML.GetAttribute("AcceptAllConnects");
									this.PermitFallthrough = !("false" == oXML.GetAttribute("Fallthrough"));
									this.UseLatency = "true" == oXML.GetAttribute("UseLatency");
								}
							}
						}
					}
					if (resetRules && this.Rules.Count < 1)
					{
						this.IsEnabled = false;
					}
					if (resetRules)
					{
						this._bRuleListIsDirty = false;
					}
					result = true;
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("{0}: {1}\n{2}", new object[]
				{
					"AutoResponder Rules Unreadable",
					sFilename,
					eX.ToString()
				});
				if (resetRules)
				{
					this.IsEnabled = false;
				}
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Save rules to the default XML file, if anything has changed.
		/// </summary>
		// Token: 0x06000098 RID: 152 RVA: 0x00004C84 File Offset: 0x00002E84
		internal void SaveDefaultRules()
		{
			if (this._bRuleListIsDirty)
			{
				this.SaveRules(CONFIG.GetPath("AutoResponderDefaultRules"));
				this._bRuleListIsDirty = false;
			}
		}

		/// <summary>
		/// Export the current ruleset as a Fiddler AutoResponder Rule XML file
		/// </summary>
		/// <param name="sFilename"></param>
		// Token: 0x06000099 RID: 153 RVA: 0x00004CA6 File Offset: 0x00002EA6
		internal bool ExportFARX(string sFilename)
		{
			return this.SaveRules(sFilename);
		}

		/// <summary>
		/// Save the rules to the specified file
		/// </summary>
		/// <param name="sFilename">The name of the file</param>
		/// <returns>False if the file cannot be saved (an exception was caught)</returns>
		// Token: 0x0600009A RID: 154 RVA: 0x00004CB0 File Offset: 0x00002EB0
		public bool SaveRules(string sFilename)
		{
			bool result;
			try
			{
				Utilities.EnsureOverwritable(sFilename);
				using (XmlTextWriter oXML = new XmlTextWriter(sFilename, Encoding.UTF8))
				{
					oXML.Formatting = Formatting.Indented;
					oXML.WriteStartDocument();
					oXML.WriteStartElement("AutoResponder");
					oXML.WriteAttributeString("LastSave", XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.RoundtripKind));
					oXML.WriteAttributeString("FiddlerVersion", CONFIG.FiddlerVersionInfo.ToString());
					oXML.WriteStartElement("State");
					oXML.WriteAttributeString("Enabled", XmlConvert.ToString(this.IsEnabled));
					oXML.WriteAttributeString("AcceptAllConnects", XmlConvert.ToString(this.AcceptAllConnects));
					oXML.WriteAttributeString("Fallthrough", XmlConvert.ToString(this.PermitFallthrough));
					oXML.WriteAttributeString("UseLatency", XmlConvert.ToString(this._bUseLatency));
					try
					{
						this.GetReaderLock();
						using (Dictionary<string, ResponderGroup>.ValueCollection.Enumerator enumerator = this.groups.Values.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								ResponderGroup group = enumerator.Current;
								if (this.Rules.Find((ResponderRule r) => r.Group != null && r.Group.Id == group.Id) != null)
								{
									oXML.WriteStartElement("ResponseGroup");
									oXML.WriteAttributeString("Id", group.Id);
									oXML.WriteAttributeString("Header", group.Header);
									oXML.WriteEndElement();
								}
							}
						}
						foreach (ResponderRule oRule in this.Rules)
						{
							oXML.WriteStartElement("ResponseRule");
							if (oRule.Group != null)
							{
								oXML.WriteAttributeString("GroupId", oRule.Group.Id);
							}
							oXML.WriteAttributeString("Match", oRule.sMatch);
							oXML.WriteAttributeString("Action", oRule.sAction);
							if (oRule.bDisableOnMatch)
							{
								oXML.WriteAttributeString("DisableAfterMatch", XmlConvert.ToString(oRule.bDisableOnMatch));
							}
							if (oRule.iLatency > 0)
							{
								oXML.WriteAttributeString("Latency", oRule.iLatency.ToString());
							}
							if (!string.IsNullOrEmpty(oRule.sComment))
							{
								oXML.WriteAttributeString("Comment", oRule.sComment);
							}
							oXML.WriteAttributeString("Enabled", XmlConvert.ToString(oRule.IsEnabled));
							if (oRule.HasImportedResponse)
							{
								byte[] arrHeaders = oRule._oResponseHeaders.ToByteArray(true, true);
								oXML.WriteStartAttribute("Headers");
								oXML.WriteBase64(arrHeaders, 0, arrHeaders.Length);
								oXML.WriteEndAttribute();
								byte[] arrBody = oRule._arrResponseBodyBytes;
								if (arrBody != null && arrBody.Length != 0)
								{
									if (arrBody.Length > 2048)
									{
										byte[] arrCompressedBody = Utilities.DeflaterCompress(arrBody);
										if ((double)arrCompressedBody.Length < 0.9 * (double)arrBody.Length)
										{
											oXML.WriteStartAttribute("DeflatedBody");
											oXML.WriteBase64(arrCompressedBody, 0, arrCompressedBody.Length);
											oXML.WriteEndAttribute();
											arrBody = null;
										}
									}
									if (arrBody != null)
									{
										oXML.WriteStartAttribute("Body");
										oXML.WriteBase64(arrBody, 0, arrBody.Length);
										oXML.WriteEndAttribute();
									}
								}
							}
							oXML.WriteEndElement();
						}
					}
					finally
					{
						this.FreeReaderLock();
					}
					oXML.WriteEndElement();
					oXML.WriteEndElement();
					oXML.WriteEndDocument();
				}
				result = true;
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("{0}: {1}\n{2}", new object[]
				{
					"Failed to save AutoResponder Rules",
					sFilename,
					eX.ToString()
				});
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Create a new autoresponse rule and add it to the listview
		/// </summary>
		/// <param name="sRule">The string to match</param>
		/// <param name="sAction">The response file or action</param>
		/// <param name="bIsEnabled">True to enable the rule</param>
		/// <returns>The Rule created, or null</returns>
		// Token: 0x0600009B RID: 155 RVA: 0x000050AC File Offset: 0x000032AC
		[Obsolete("Please use another constructor!")]
		public ResponderRule AddRule(string sRule, string sAction, bool bIsEnabled)
		{
			return this.AddRule(sRule, sAction, "New Rule", bIsEnabled);
		}

		/// <summary>
		/// Create a new autoresponse rule and add it to the listview
		/// </summary>
		/// <param name="sRule">The string to match</param>
		/// <param name="oImportedSession">The sdession to use for rule response</param>
		/// <param name="sDescription">A description of that reply</param>
		/// <param name="bEnabled">True to enable the rule</param>
		/// <returns>The Rule created, or null</returns>
		// Token: 0x0600009C RID: 156 RVA: 0x000050BC File Offset: 0x000032BC
		[Obsolete("Please use another constructor!")]
		public ResponderRule AddRule(string sRule, Session oImportedSession, string sDescription, bool bEnabled)
		{
			return this.AddRule(sRule, oImportedSession, sDescription, 0, bEnabled);
		}

		/// <summary>
		/// Create a new autoresponse rule and add it to the listview
		/// </summary>
		/// <param name="sRule">The string to match</param>
		/// <param name="oImportedSession">The sdession to use for rule response</param>
		/// <param name="sDescription">A description of that reply</param>
		/// <param name="bEnabled">True to enable the rule</param>
		/// <returns>The Rule created, or null</returns>
		// Token: 0x0600009D RID: 157 RVA: 0x000050CA File Offset: 0x000032CA
		[Obsolete("Please use another constructor!")]
		public ResponderRule AddRule(string sRule, Session oImportedSession, string sDescription, int iLatencyMS, bool bEnabled)
		{
			if (oImportedSession != null)
			{
				return this.AddRule(sRule, oImportedSession.oResponse.headers, oImportedSession.responseBodyBytes, sDescription, "New Rule", iLatencyMS, bEnabled);
			}
			return this.AddRule(sRule, null, null, sDescription, "New Rule", iLatencyMS, bEnabled);
		}

		/// <summary>
		/// Create a new autoresponse rule and add it to the listview
		/// </summary>
		/// <param name="sRule">The string to match</param>
		/// <param name="oRH">The list or response headers to send (not required)</param>
		/// <param name="arrResponseBody">The response body to send (not required)</param>
		/// <param name="sDescription">A description of that reply</param>
		/// <param name="iLatencyMS">Milliseconds of latency (0 if not needed)</param>
		/// <param name="bEnabled">True to enable the rule</param>
		/// <returns>The Rule created, or null</returns>
		// Token: 0x0600009E RID: 158 RVA: 0x00005105 File Offset: 0x00003305
		public ResponderRule AddRule(string sRule, HTTPResponseHeaders oRH, byte[] arrResponseBody, string sDescription, int iLatencyMS, bool bEnabled)
		{
			return this.AddRule(sRule, oRH, arrResponseBody, sDescription, "New Rule", iLatencyMS, bEnabled);
		}

		/// <summary>
		/// Create a new autoresponse rule and add it to the listview
		/// </summary>
		/// <param name="sRule">The string to match</param>
		/// <param name="sAction">The response file or action</param>
		/// <param name="sComment">The name(comment) of the rule</param>
		/// <param name="bIsEnabled">True to enable the rule</param>
		/// <returns>The Rule created, or null</returns>
		// Token: 0x0600009F RID: 159 RVA: 0x0000511B File Offset: 0x0000331B
		public ResponderRule AddRule(string sRule, string sAction, string sComment, bool bIsEnabled)
		{
			return this.AddRule(sRule, null, null, sAction, sComment, 0, bIsEnabled);
		}

		/// <summary>
		/// Create a new autoresponse rule and add it to the listview
		/// </summary>
		/// <param name="sRule">The string to match</param>
		/// <param name="oRH">The list or response headers to send (not required)</param>
		/// <param name="arrResponseBody">The response body to send (not required)</param>
		/// <param name="sDescription">A description of that reply</param>
		/// <param name="sComment">The name/comment for the rule</param>
		/// <param name="iLatencyMS">Milliseconds of latency (0 if not needed)</param>
		/// <param name="bEnabled">True to enable the rule</param>
		/// <returns>The Rule created, or null</returns>
		// Token: 0x060000A0 RID: 160 RVA: 0x0000512C File Offset: 0x0000332C
		public ResponderRule AddRule(string sRule, HTTPResponseHeaders oRH, byte[] arrResponseBody, string sDescription, string sComment, int iLatencyMS, bool bEnabled)
		{
			ResponderRule result;
			try
			{
				ResponderRule oNew = new ResponderRule(sRule, oRH, arrResponseBody, sDescription, sComment, iLatencyMS, bEnabled);
				try
				{
					this.GetWriterLock();
					this.Rules.Add(oNew);
				}
				finally
				{
					this.FreeWriterLock();
				}
				this._bRuleListIsDirty = true;
				result = oNew;
			}
			catch (Exception eX)
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00005194 File Offset: 0x00003394
		public ResponderGroup AddGroup(string id, string header)
		{
			ResponderGroup result;
			try
			{
				ResponderGroup group = new ResponderGroup
				{
					Id = id,
					Header = header
				};
				try
				{
					this.GetWriterLock();
					this.groups.Add(group.Id, group);
				}
				finally
				{
					this.FreeWriterLock();
				}
				this._bRuleListIsDirty = true;
				result = group;
			}
			catch (Exception eX)
			{
				result = null;
			}
			return result;
		}

		/// <summary>
		/// Moves a rule to earlier in the list
		/// </summary>
		/// <param name="oRule"></param>
		/// <returns></returns>
		// Token: 0x060000A2 RID: 162 RVA: 0x00005204 File Offset: 0x00003404
		internal bool PromoteRule(ResponderRule oRule)
		{
			bool result;
			try
			{
				this.GetWriterLock();
				int ixItem = this.Rules.IndexOf(oRule);
				if (ixItem > 0)
				{
					this.Rules.Reverse(ixItem - 1, 2);
					this._bRuleListIsDirty = true;
					result = true;
				}
				else
				{
					result = false;
				}
			}
			finally
			{
				this.FreeWriterLock();
			}
			return result;
		}

		/// <summary>
		/// Moves a rule to later in the list
		/// </summary>
		/// <param name="oRule"></param>
		/// <returns></returns>
		// Token: 0x060000A3 RID: 163 RVA: 0x00005260 File Offset: 0x00003460
		internal bool DemoteRule(ResponderRule oRule)
		{
			bool result;
			try
			{
				this.GetWriterLock();
				int ixItem = this.Rules.IndexOf(oRule);
				if (ixItem > -1 && ixItem < this.Rules.Count - 1)
				{
					this.Rules.Reverse(ixItem, 2);
					this._bRuleListIsDirty = true;
					result = true;
				}
				else
				{
					result = false;
				}
			}
			finally
			{
				this.FreeWriterLock();
			}
			return result;
		}

		/// <summary>
		/// Remove a rule from the list of rules
		/// </summary>
		/// <param name="oRule"></param>
		/// <returns></returns>
		// Token: 0x060000A4 RID: 164 RVA: 0x000052C8 File Offset: 0x000034C8
		public bool RemoveRule(ResponderRule oRule)
		{
			bool result;
			try
			{
				try
				{
					this.GetWriterLock();
					this.Rules.Remove(oRule);
				}
				finally
				{
					this.FreeWriterLock();
				}
				this._bRuleListIsDirty = true;
				result = true;
			}
			catch (Exception eX)
			{
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Remove a rule from the list of rules
		/// </summary>
		/// <param name="oRule"></param>
		/// <returns></returns>
		// Token: 0x060000A5 RID: 165 RVA: 0x00005320 File Offset: 0x00003520
		public bool RemoveGroup(ResponderGroup group)
		{
			bool result;
			try
			{
				try
				{
					this.GetWriterLock();
					this.groups.Remove(group.Id);
				}
				finally
				{
					this.FreeWriterLock();
				}
				this._bRuleListIsDirty = true;
				result = true;
			}
			catch (Exception eX)
			{
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Returns the list of responder groups
		/// </summary>
		// Token: 0x060000A6 RID: 166 RVA: 0x0000537C File Offset: 0x0000357C
		public Dictionary<string, ResponderGroup> GetGroups()
		{
			return this.groups;
		}

		/// <summary>
		/// Update flags (backcolor and x-Autoresponder) on a matched Session for user's awareness
		/// </summary>
		// Token: 0x060000A7 RID: 167 RVA: 0x00005384 File Offset: 0x00003584
		private static void _MarkMatch(Session oS, string name, string action)
		{
			oS.oFlags["x-AutoResponder"] = "Matched: " + name + ", sent: " + action;
			oS.oFlags["ui-backcolor"] = AutoResponder.sColorAutoResponded;
			oS.SetBitFlag(SessionFlags.IsModifiedByRule, true);
		}

		/// <summary>
		/// If the AutoResponder rules find a match, this function handles taking the appropriate action after the response is received. 
		/// </summary>
		/// <param name="oSession">The session to modify</param>
		/// <param name="oMatch">The matching rule</param>
		/// <param name="action">The rule action</param>
		/// <returns>Bool if this is the "FINAL" action</returns>
		// Token: 0x060000A8 RID: 168 RVA: 0x000053D4 File Offset: 0x000035D4
		private static bool HandleResponseMatch(Session oSession, ResponderRule oMatch, RuleAction action)
		{
			if (oSession == null || oSession.state < SessionStates.AutoTamperResponseBefore)
			{
				return false;
			}
			RuleActionType type = action.Type;
			if (type == RuleActionType.UpdateResponseHeader)
			{
				if (KeyValueRules.HeadersValueRegex(oSession.ResponseHeaders, action.Condition, action.Key, action.Find, action.Value))
				{
					AutoResponder._MarkMatch(oSession, oMatch.sComment, action.Type.ToString());
				}
				return false;
			}
			if (type == RuleActionType.UpdateResponseBody)
			{
				KeyValueRules.BodyRegex(oSession, false, action.Condition, action.Find, action.Value);
				return false;
			}
			if (type != RuleActionType.UpdateResponseCookies)
			{
				return false;
			}
			KeyValueRules.CookieRegex(oSession.ResponseHeaders, action.Condition, action.Key, action.Find, action.Value);
			return false;
		}

		/// <summary>
		/// If the AutoResponder rules find a match, this function handles taking the appropriate action. For CONNECT or HEAD requests, no body will be returned. 
		/// NOTE: ui-backcolor only set on FINAL actions
		/// </summary>
		/// <param name="oSession">The session to modify</param>
		/// <param name="oMatch">The matching rule</param>
		/// <param name="action">The rule action</param>
		/// <returns>true if this is the "FINAL" action</returns>
		// Token: 0x060000A9 RID: 169 RVA: 0x00005494 File Offset: 0x00003694
		private static bool HandleMatch(Session oSession, ResponderRule oMatch, RuleAction action)
		{
			if (oSession == null)
			{
				return false;
			}
			bool hasResponse = oSession.state >= SessionStates.AutoTamperResponseBefore;
			switch (action.Type)
			{
			case RuleActionType.MarkSession:
			{
				bool markResult = false;
				if (!string.IsNullOrEmpty(action.Value))
				{
					markResult = AutoResponder.HandleMatch(oSession, oMatch, "*foregroundColor:" + action.Value);
				}
				if (!markResult && !string.IsNullOrEmpty(action.Key))
				{
					markResult = AutoResponder.HandleMatch(oSession, oMatch, "*backgroundColor:" + action.Key);
				}
				return markResult;
			}
			case RuleActionType.UpdateRequestHeader:
				if (KeyValueRules.HeadersValueRegex(oSession.RequestHeaders, action.Condition, action.Key, action.Find, action.Value))
				{
					AutoResponder._MarkMatch(oSession, oMatch.sComment, action.Type.ToString());
				}
				return false;
			case RuleActionType.UpdateResponseHeader:
			case RuleActionType.UpdateResponseBody:
			case RuleActionType.UpdateResponseCookies:
				AutoResponder.ResponseAvailable(oSession);
				return false;
			case RuleActionType.UpdateRequestBody:
				if (KeyValueRules.BodyRegex(oSession, true, action.Condition, action.Find, action.Value))
				{
					AutoResponder._MarkMatch(oSession, oMatch.sComment, action.Type.ToString());
				}
				return false;
			case RuleActionType.UpdateUrl:
				if (!oSession.isTunnel && KeyValueRules.UrlValueRegex(oSession, action.Condition, action.Find, action.Value))
				{
					AutoResponder._MarkMatch(oSession, oMatch.sComment, action.Type.ToString());
				}
				return false;
			case RuleActionType.UpdateQueryParams:
				if (KeyValueRules.UrlQueryValueRegex(oSession, action.Condition, action.Key, action.Find, action.Value))
				{
					AutoResponder._MarkMatch(oSession, oMatch.sComment, action.Type.ToString());
				}
				return false;
			case RuleActionType.UpdateRequestCookies:
				if (KeyValueRules.CookieRegex(oSession.RequestHeaders, action.Condition, action.Key, action.Find, action.Value))
				{
					AutoResponder._MarkMatch(oSession, oMatch.sComment, action.Type.ToString());
				}
				return false;
			case RuleActionType.PredefinedResponse:
			case RuleActionType.ResponseFile:
			case RuleActionType.MagicString:
				return !hasResponse && AutoResponder.HandleMatch(oSession, oMatch, action.Value);
			case RuleActionType.ManualResponse:
				return !hasResponse && oMatch.HasImportedResponse && AutoResponder.HandleMatch(oSession, oMatch, "");
			case RuleActionType.DoNotCapture:
				oSession.bypassGateway = true;
				oSession.oFlags["ui-hide"] = "AutoResponder";
				oSession.RaiseSessionFieldChanged();
				return true;
			case RuleActionType.DelayRequest:
			{
				int delay;
				if (!hasResponse && int.TryParse(action.Value, out delay))
				{
					AutoResponder.DoDelay(delay, true);
					AutoResponder._MarkMatch(oSession, oMatch.sComment, action.Type.ToString());
				}
				return false;
			}
			case RuleActionType.GracefulClose:
				return !hasResponse && AutoResponder.HandleMatch(oSession, oMatch, "*drop");
			case RuleActionType.NonGracefulClose:
				return !hasResponse && AutoResponder.HandleMatch(oSession, oMatch, "*reset");
			default:
				FiddlerApplication.Log.LogFormat("Unknown match type: {0}", new object[] { action.Type });
				return false;
			}
		}

		/// <summary>
		/// If the AutoResponder rules find a match, this function handles taking the appropriate action. For CONNECT or HEAD requests, no body will be returned. 
		/// NOTE: ui-backcolor only set on FINAL actions
		/// </summary>
		/// <param name="oSession">The session to modify</param>
		/// <param name="oMatch">The matching rule</param>
		/// <param name="action">The action magic string</param>
		/// <returns>Bool if this is the "FINAL" action</returns>
		// Token: 0x060000AA RID: 170 RVA: 0x00005790 File Offset: 0x00003990
		private static bool HandleMatch(Session oSession, ResponderRule oMatch, string action)
		{
			bool bIsConnect = oSession.HTTPMethodIs("CONNECT");
			if (action.StartsWith("*"))
			{
				if (action.OICEquals("*drop"))
				{
					AutoResponder.DoDelay(oMatch.iLatency, false);
					if (oSession.oRequest != null && oSession.oRequest.pipeClient != null)
					{
						oSession.oRequest.pipeClient.End();
					}
					oSession.utilCreateResponseAndBypassServer();
					oSession.oResponse.headers.SetStatus(0, "Client Connection Dropped");
					AutoResponder._MarkMatch(oSession, oMatch.sComment, action);
					oSession.state = SessionStates.Aborted;
					return true;
				}
				if (action.OICEquals("*reset"))
				{
					AutoResponder.DoDelay(oMatch.iLatency, false);
					if (oSession.oRequest != null && oSession.oRequest.pipeClient != null)
					{
						oSession.oRequest.pipeClient.EndWithRST();
					}
					oSession.utilCreateResponseAndBypassServer();
					oSession.oResponse.headers.SetStatus(0, "Client Connection Reset");
					AutoResponder._MarkMatch(oSession, oMatch.sComment, action);
					oSession.state = SessionStates.Aborted;
					return true;
				}
				if (action.OICStartsWith("*ReplyWithTunnel"))
				{
					if (!bIsConnect)
					{
						return false;
					}
					AutoResponder.DoDelay(oMatch.iLatency, false);
					oSession.oFlags["X-ReplyWithTunnel"] = "*Reply rule";
					AutoResponder._MarkMatch(oSession, oMatch.sComment, action);
					return true;
				}
				else
				{
					if (action.OICStartsWith("*CORSPreflightAllow"))
					{
						AutoResponder.DoDelay(oMatch.iLatency, false);
						oSession.utilCreateResponseAndBypassServer();
						oSession.oResponse.headers.SetStatus(200, "Fiddler CORSPreflightAllow");
						string sOrigin = oSession.oRequest["Origin"];
						if (string.IsNullOrEmpty(sOrigin))
						{
							sOrigin = "*";
						}
						oSession.oResponse["Access-Control-Allow-Origin"] = sOrigin;
						string sMethods = oSession.oRequest["Access-Control-Request-Method"];
						if (!string.IsNullOrEmpty(sMethods))
						{
							oSession.oResponse["Access-Control-Allow-Methods"] = sMethods;
						}
						string sHeaders = oSession.oRequest["Access-Control-Request-Headers"];
						if (!string.IsNullOrEmpty(sHeaders))
						{
							oSession.oResponse["Access-Control-Allow-Headers"] = sHeaders;
						}
						oSession.oResponse["Access-Control-Max-Age"] = "1";
						oSession.oResponse["Access-Control-Allow-Credentials"] = "true";
						AutoResponder._MarkMatch(oSession, oMatch.sComment, action);
						oSession.state = SessionStates.Aborted;
						return true;
					}
					if (action.OICStartsWith("*delay:"))
					{
						int iMS = 0;
						if (int.TryParse(Utilities.TrimBefore(action, ':'), out iMS))
						{
							oSession.oFlags["x-AutoResponder-Delay"] = iMS.ToString();
							Thread.Sleep(iMS);
						}
						return false;
					}
					if (action.OICStartsWith("*flag:"))
					{
						string sFlagName = Utilities.TrimAfter(action.Substring(6), "=");
						string sFlagValue = Utilities.TrimBefore(action, "=");
						if (sFlagValue.Length > 0)
						{
							oSession.oFlags[sFlagName] = sFlagValue;
						}
						else
						{
							oSession.oFlags.Remove(sFlagName);
						}
						return false;
					}
					if (action.OICStartsWith("*foregroundColor:"))
					{
						string colorValue = Utilities.TrimBefore(action, ":");
						if (colorValue.Length > 0)
						{
							oSession.oFlags["ui-foregroundColor"] = colorValue;
						}
						else
						{
							oSession.oFlags.Remove("ui-foregroundColor");
						}
						return false;
					}
					if (action.OICStartsWith("*backgroundColor:"))
					{
						string colorValue2 = Utilities.TrimBefore(action, ":");
						if (colorValue2.Length > 0)
						{
							oSession.oFlags["ui-backgroundColor"] = colorValue2;
						}
						else
						{
							oSession.oFlags.Remove("ui-backgroundColor");
						}
						return false;
					}
					if (action.OICStartsWith("*bold:"))
					{
						string colorValue3 = Utilities.TrimBefore(action, ":");
						if (((colorValue3 != null) ? colorValue3.ToLowerInvariant() : null) == "true")
						{
							oSession.oFlags["ui-bold"] = "rule-marked";
						}
						else
						{
							oSession.oFlags.Remove("ui-bold");
						}
						return false;
					}
					if (action.OICStartsWith("*header:"))
					{
						string sHeaderName = Utilities.TrimAfter(action.Substring(8), "=");
						string sHeaderValue = Utilities.TrimBefore(action, "=");
						if (sHeaderValue.Length > 0)
						{
							oSession.oRequest[sHeaderName] = sHeaderValue;
						}
						else
						{
							oSession.oRequest.headers.Remove(sHeaderName);
						}
						return false;
					}
					if (action.OICEquals("*bpafter"))
					{
						oSession.oFlags["x-breakresponse"] = "AutoResponder";
						oSession.bBufferResponse = true;
						return false;
					}
					if (action.OICStartsWith("*redir:") && !bIsConnect)
					{
						AutoResponder.DoDelay(oMatch.iLatency, false);
						oSession.utilCreateResponseAndBypassServer();
						oSession.oResponse.headers.SetStatus(307, "AutoRedir");
						if (!AutoResponder.IfRuleIsRegExCallReplacementFunction(oMatch, action, oSession.fullUrl, delegate(string sReplaceWith)
						{
							oSession.oResponse.headers["Location"] = sReplaceWith.Substring(7);
						}))
						{
							oSession.oResponse.headers["Location"] = action.Substring(7);
						}
						oSession.oResponse.headers["Cache-Control"] = "max-age=0, must-revalidate";
						AutoResponder._MarkMatch(oSession, oMatch.sComment, action);
						return true;
					}
					if (action.OICEquals("*exit"))
					{
						AutoResponder.DoDelay(oMatch.iLatency, false);
						return true;
					}
					action.OICStartsWith("*script:");
				}
			}
			if (oMatch.HasImportedResponse)
			{
				if (bIsConnect && oMatch._oResponseHeaders.HTTPResponseCode == 200)
				{
					return false;
				}
				if (oSession.state >= SessionStates.SendingRequest)
				{
					FiddlerApplication.Log.LogFormat("fiddler.autoresponder.error> AutoResponder will not respond to a request which is already in-flight; Session #{0} is at state: {1}", new object[] { oSession.id, oSession.state });
					return true;
				}
				oSession.utilCreateResponseAndBypassServer();
				AutoResponder._MarkMatch(oSession, oMatch.sComment, action);
				if (oMatch._arrResponseBodyBytes == null || oMatch._oResponseHeaders == null)
				{
					FiddlerApplication.Log.LogString("fiddler.autoresponder.error> Response data from imported session is missing.");
					return true;
				}
				AutoResponder.DoDelay(oMatch.iLatency, false);
				if (oSession.HTTPMethodIs("HEAD"))
				{
					oSession.responseBodyBytes = Utilities.emptyByteArray;
				}
				else
				{
					oSession.responseBodyBytes = oMatch._arrResponseBodyBytes;
				}
				oSession.oResponse.headers = (HTTPResponseHeaders)oMatch._oResponseHeaders.Clone();
				oSession.state = SessionStates.AutoTamperResponseBefore;
				return true;
			}
			else
			{
				if (!bIsConnect && action.OICStartsWithAny(new string[] { "http://", "https://", "ftp://" }))
				{
					AutoResponder.DoDelay(oMatch.iLatency, false);
					AutoResponder._MarkMatch(oSession, oMatch.sComment, action);
					oSession.oFlags["X-OriginalURL"] = oSession.fullUrl;
					if (!AutoResponder.IfRuleIsRegExCallReplacementFunction(oMatch, action, oSession.fullUrl, delegate(string sReplaceWith)
					{
						oSession.fullUrl = sReplaceWith;
					}))
					{
						oSession.fullUrl = action;
					}
					return true;
				}
				AutoResponder.DoDelay(oMatch.iLatency, false);
				if (!AutoResponder.IfRuleIsRegExCallReplacementFunction(oMatch, action, oSession.fullUrl, delegate(string sReplaceWith)
				{
					if (sReplaceWith.OICStartsWithAny(new string[] { "http://", "https://", "ftp://" }))
					{
						oSession.oFlags["X-OriginalURL"] = oSession.fullUrl;
						oSession.fullUrl = sReplaceWith;
						return;
					}
					oSession.oFlags["x-replywithfile"] = (('\\' == Path.DirectorySeparatorChar) ? sReplaceWith.Replace('/', '\\') : sReplaceWith);
				}))
				{
					oSession.oFlags["x-replywithfile"] = action;
				}
				AutoResponder._MarkMatch(oSession, oMatch.sComment, action);
				return true;
			}
		}

		/// <summary>
		/// This function determines if oMatch's rule is a RegEx. If so, it runs RegEx.Replace passing sInURI and calls the replacement function oDel
		/// </summary>
		/// <param name="oMatch"></param>
		/// <param name="action"></param>
		/// <param name="sInURI"></param>
		/// <param name="oDel"></param>
		/// <returns></returns>
		// Token: 0x060000AB RID: 171 RVA: 0x00005FC8 File Offset: 0x000041C8
		private static bool IfRuleIsRegExCallReplacementFunction(ResponderRule oMatch, string action, string sInURI, Action<string> oDel)
		{
			List<RuleMatch> matches = new List<RuleMatch>
			{
				new RuleMatch
				{
					Type = RuleMatchType.MagicString,
					Value = oMatch.sMatch
				}
			};
			if (!string.IsNullOrEmpty(oMatch.sMatch) && oMatch.sMatch.StartsWith("{"))
			{
				try
				{
					JsonSerializerOptions serializeOptions = new JsonSerializerOptions
					{
						PropertyNamingPolicy = JsonNamingPolicy.CamelCase
					};
					RuleMatchCollection ruleMatches = JsonSerializer.Deserialize<RuleMatchCollection>(oMatch.sMatch, serializeOptions);
					matches = new List<RuleMatch>(AutoResponder.DecodeMatches(ruleMatches.Matches));
				}
				catch (Exception e)
				{
					FiddlerApplication.Log.LogFormat("Error deserializing rule matches: {0}", new object[] { e.ToString() });
				}
			}
			string sRegEx = null;
			for (int i = 0; i < matches.Count; i++)
			{
				if (matches[i].Type == RuleMatchType.MagicString)
				{
					string sRule = matches[i].Value ?? "";
					if (sRule.OICStartsWith("METHOD:"))
					{
						sRule = Utilities.TrimBefore(sRule.Substring(7), ' ');
					}
					if (sRule.Length >= 7)
					{
						if (sRule.OICStartsWith("REGEX:"))
						{
							sRegEx = sRule.Substring(6);
						}
						else if (sRule.OICStartsWith("URLWithBody:REGEX:"))
						{
							sRegEx = Utilities.TrimAfter(sRule.Substring(18), ' ');
						}
					}
				}
			}
			if (string.IsNullOrEmpty(sRegEx))
			{
				return false;
			}
			if (sRegEx == ".*")
			{
				sRegEx = "^.+$";
			}
			try
			{
				Regex r = new Regex(sRegEx);
				string sReplaced = r.Replace(sInURI, action);
				oDel(sReplaced);
			}
			catch
			{
				return false;
			}
			return true;
		}

		// Token: 0x060000AC RID: 172 RVA: 0x00006178 File Offset: 0x00004378
		private static IEnumerable<RuleMatch> DecodeMatches(string[] matches)
		{
			List<RuleMatch> decoded = new List<RuleMatch>();
			if (matches == null || matches.Length == 0)
			{
				return decoded;
			}
			for (int i = 0; i < matches.Length; i++)
			{
				string match = (string.IsNullOrEmpty(matches[i]) ? "" : matches[i].Trim());
				if (match.StartsWith("^^"))
				{
					string[] parts = match.Substring(2).Split('*', StringSplitOptions.None);
					if (parts.Length == 0)
					{
						FiddlerApplication.Log.LogString("Error decoding rule action! Invalid format");
					}
					else
					{
						for (int p = 0; p < parts.Length; p++)
						{
							try
							{
								parts[p] = Encoding.UTF8.GetString(Convert.FromBase64String(parts[p]));
							}
							catch (Exception e)
							{
								FiddlerApplication.Log.LogFormat("Error decoding rule match - {0}", new object[] { e.Message });
							}
						}
						int len = parts.Length;
						if (len == 1)
						{
							decoded.Add(new RuleMatch
							{
								Type = RuleMatchType.MagicString,
								Value = parts[0]
							});
						}
						else
						{
							RuleMatch rule = new RuleMatch
							{
								Type = (RuleMatchType)Enum.Parse(typeof(RuleMatchType), parts[0])
							};
							if (len > 3)
							{
								rule.Key = parts[len - 3];
							}
							if (len > 2)
							{
								rule.Condition = parts[len - 2];
							}
							if (len > 1)
							{
								rule.Value = parts[len - 1];
							}
							decoded.Add(rule);
						}
					}
				}
				else
				{
					decoded.Add(new RuleMatch
					{
						Type = RuleMatchType.MagicString,
						Value = match
					});
				}
			}
			return decoded;
		}

		// Token: 0x060000AD RID: 173 RVA: 0x00006308 File Offset: 0x00004508
		internal static IEnumerable<RuleAction> DecodeActions(string[] actions)
		{
			List<RuleAction> decoded = new List<RuleAction>();
			if (actions == null || actions.Length == 0)
			{
				return decoded;
			}
			for (int i = 0; i < actions.Length; i++)
			{
				string action = (string.IsNullOrEmpty(actions[i]) ? "" : actions[i].Trim());
				if (action.StartsWith("^^"))
				{
					string[] parts = action.Substring(2).Split('*', StringSplitOptions.None);
					if (parts.Length == 0)
					{
						FiddlerApplication.Log.LogString("Error decoding rule action! Invalid format");
					}
					else
					{
						for (int p = 0; p < parts.Length; p++)
						{
							try
							{
								parts[p] = Encoding.UTF8.GetString(Convert.FromBase64String(parts[p]));
							}
							catch (Exception e)
							{
								FiddlerApplication.Log.LogFormat("Error decoding rule action - {0}", new object[] { e.Message });
							}
						}
						int len = parts.Length;
						if (len == 1)
						{
							decoded.Add(new RuleAction
							{
								Type = RuleActionType.MagicString,
								Value = parts[0]
							});
						}
						else
						{
							RuleAction rule = new RuleAction
							{
								Type = (RuleActionType)Enum.Parse(typeof(RuleActionType), parts[0])
							};
							if (len > 4)
							{
								rule.Key = parts[len - 4];
							}
							if (len > 3)
							{
								rule.Condition = parts[len - 3];
							}
							if (len > 2)
							{
								rule.Find = parts[len - 2];
							}
							if (len > 1)
							{
								rule.Value = parts[len - 1];
							}
							decoded.Add(rule);
						}
					}
				}
				else
				{
					decoded.Add(new RuleAction
					{
						Type = RuleActionType.MagicString,
						Value = action
					});
				}
			}
			return decoded;
		}

		// Token: 0x060000AE RID: 174 RVA: 0x000064AC File Offset: 0x000046AC
		internal static IEnumerable<string> EncodeActions(RuleAction[] actions)
		{
			List<string> encoded = new List<string>();
			if (actions == null || actions.Length == 0)
			{
				return encoded;
			}
			foreach (RuleAction action in actions)
			{
				if (action != null)
				{
					string strAction = "^^";
					strAction += string.Join("*", new string[]
					{
						Convert.ToBase64String(Encoding.UTF8.GetBytes(((byte)action.Type).ToString())),
						Convert.ToBase64String(Encoding.UTF8.GetBytes(action.Key ?? string.Empty)),
						Convert.ToBase64String(Encoding.UTF8.GetBytes(action.Condition ?? string.Empty)),
						Convert.ToBase64String(Encoding.UTF8.GetBytes(action.Find ?? string.Empty)),
						Convert.ToBase64String(Encoding.UTF8.GetBytes(action.Value ?? string.Empty))
					});
					encoded.Add(strAction);
				}
			}
			return encoded;
		}

		/// <summary>
		/// Sleeps the current thread for the duration specified by the ResponderRule, if and only if
		/// the Use Latency checkbox is enabled.
		/// </summary>
		/// <param name="latency">The number of ms to delay the request.</param>
		/// <param name="force">Delay even if the global UseLatency option is disabled.</param>
		// Token: 0x060000AF RID: 175 RVA: 0x000065B1 File Offset: 0x000047B1
		private static void DoDelay(int latency, bool force = false)
		{
			if ((force || FiddlerApplication.oAutoResponder.UseLatency) && latency > 0)
			{
				Thread.Sleep(latency);
			}
		}

		/// <summary>
		/// Attempt to match a Session to a Filter for the purposes of Request Breakpointing only.
		/// If the Action is anything except BPU, it will be handled after the Request Tampering phase.
		/// </summary>
		/// <param name="oSession">The Session being processed</param>
		// Token: 0x060000B0 RID: 176 RVA: 0x000065CC File Offset: 0x000047CC
		internal void DoMatchBeforeRequestTampering(Session oSession)
		{
			if (oSession.isFlagSet(SessionFlags.Ignored))
			{
				return;
			}
			try
			{
				this.GetReaderLock();
				foreach (ResponderRule oCandidate in this.Rules)
				{
					if (oCandidate.sAction.OICEquals("*bpu") && AutoResponder.CheckMatch(oSession, oCandidate, true, true))
					{
						if (oCandidate.bDisableOnMatch)
						{
							oCandidate.IsEnabled = false;
						}
						oSession.oFlags["x-breakrequest"] = "AutoResponder";
						break;
					}
				}
			}
			finally
			{
				this.FreeReaderLock();
			}
		}

		/// <summary>
		/// Attempt to match a Session to a Filter for the purposes of Response update only.
		/// Only actions that modify the response headers/body are handled here.
		/// </summary>
		/// <param name="oSession">The Session being processed</param>
		// Token: 0x060000B1 RID: 177 RVA: 0x00006680 File Offset: 0x00004880
		internal void DoMatchAfterResponse(Session oSession)
		{
			if (oSession.isFlagSet(SessionFlags.Ignored))
			{
				return;
			}
			List<ResponderRule> listRules;
			try
			{
				this.GetReaderLock();
				listRules = new List<ResponderRule>(this.Rules);
			}
			finally
			{
				this.FreeReaderLock();
			}
			foreach (ResponderRule oCandidate in listRules)
			{
				if (!string.IsNullOrEmpty(oCandidate.sAction))
				{
					bool shouldDisable = false;
					if (oCandidate.bDisableOnMatch && oSession.oFlags["x-breakresponse"] == "AutoResponder")
					{
						oCandidate.IsEnabled = true;
						shouldDisable = true;
					}
					bool finalBehavior;
					AutoResponder.CheckRule(oSession, oCandidate, out finalBehavior, true);
					if (shouldDisable)
					{
						oCandidate.IsEnabled = false;
					}
					if (finalBehavior)
					{
						break;
					}
				}
			}
		}

		/// <summary>
		/// This method attempts to match a Session to a Filter.
		/// </summary>
		/// <param name="oSession">The Session being processed.</param>
		// Token: 0x060000B2 RID: 178 RVA: 0x00006750 File Offset: 0x00004950
		internal void DoMatchAfterRequestTampering(Session oSession)
		{
			if (oSession.isFlagSet(SessionFlags.Ignored))
			{
				return;
			}
			List<ResponderRule> listRules;
			try
			{
				this.GetReaderLock();
				listRules = new List<ResponderRule>(this.Rules);
			}
			finally
			{
				this.FreeReaderLock();
			}
			foreach (ResponderRule oCandidate in listRules)
			{
				if (!string.IsNullOrEmpty(oCandidate.sAction) && !oCandidate.sAction.OICEquals("*bpu"))
				{
					bool finalBehavior;
					AutoResponder.CheckRule(oSession, oCandidate, out finalBehavior, true);
					if (finalBehavior)
					{
						return;
					}
				}
			}
			if (this.AcceptAllConnects && oSession.HTTPMethodIs("CONNECT"))
			{
				oSession.oFlags["X-ReplyWithTunnel"] = "AutoResponderWithAcceptAllConnects";
				oSession.oFlags["ui-backcolor"] = AutoResponder.sColorAutoResponded;
				oSession.SetBitFlag(SessionFlags.ResponseGeneratedByFiddler, true);
				oSession.SetBitFlag(SessionFlags.IsModifiedByRule, true);
				return;
			}
			if (!this.PermitFallthrough)
			{
				oSession.oFlags["ui-backcolor"] = AutoResponder.sColorAutoResponded;
				oSession.SetBitFlag(SessionFlags.ResponseGeneratedByFiddler, true);
				oSession.SetBitFlag(SessionFlags.IsModifiedByRule, true);
				if (oSession.HTTPMethodIs("CONNECT"))
				{
					oSession.oFlags["x-replywithtunnel"] = "AutoResponderWithNoFallthrough";
					return;
				}
				if (oSession.state < SessionStates.SendingRequest)
				{
					oSession.utilCreateResponseAndBypassServer();
				}
				else
				{
					oSession.oResponse.headers = new HTTPResponseHeaders();
					oSession.responseBodyBytes = Utilities.emptyByteArray;
					oSession.bBufferResponse = true;
				}
				oSession.state = SessionStates.ReadingResponse;
				oSession.oResponse["Date"] = DateTime.UtcNow.ToString("r");
				if (oSession.oRequest.headers.Exists("If-Modified-Since") || oSession.oRequest.headers.Exists("If-None-Match"))
				{
					oSession.responseCode = 304;
					return;
				}
				oSession.responseCode = 404;
				oSession.oResponse["Cache-Control"] = "max-age=0, must-revalidate";
				if (!oSession.HTTPMethodIs("HEAD"))
				{
					oSession.utilSetResponseBody("The Fiddler Rules are enabled, but this request did not match any of the enabled rules. Because the \"Unmatched requests passthrough\" option is not enabled, this HTTP/404 response has been generated. To allow unmatched requests, go to Settings > Rules > Enable \"Unmatched requests passthrough\".".PadRight(512, ' '));
				}
			}
		}

		/// <summary>
		/// Execute a rule against an existing session.
		/// </summary>
		/// <param name="oSession">The session to check</param>
		/// <param name="oCandidate">The rule to use</param>
		/// <param name="finalBehavior">Whether the rule actions indicated a final behavior(stop checking other rules).</param>
		/// <param name="isLive">Is this an existing session (False) or a new session in progress (True).</param>
		/// <returns>True if rule matched, False if not</returns>
		// Token: 0x060000B3 RID: 179 RVA: 0x0000698C File Offset: 0x00004B8C
		internal static bool CheckRule(Session oSession, ResponderRule oCandidate, out bool finalBehavior, bool isLive = true)
		{
			finalBehavior = false;
			bool hasResponse = oSession.state >= SessionStates.AutoTamperResponseBefore;
			bool waitingForResponse = oSession.oFlags.ContainsKey("x-AutoResponder-Wait");
			if (!hasResponse && waitingForResponse)
			{
				return false;
			}
			bool hasMatched = AutoResponder.CheckMatch(oSession, oCandidate, false, isLive);
			if (hasMatched)
			{
				if (oCandidate.bDisableOnMatch && isLive)
				{
					oCandidate.IsEnabled = false;
				}
				List<RuleAction> actions = new List<RuleAction>
				{
					new RuleAction
					{
						Type = RuleActionType.MagicString,
						Value = oCandidate.sAction
					}
				};
				if (!string.IsNullOrEmpty(oCandidate.sAction) && oCandidate.sAction.StartsWith("["))
				{
					try
					{
						string[] actionsString = JsonSerializer.Deserialize<string[]>(oCandidate.sAction, null);
						actions = new List<RuleAction>(AutoResponder.DecodeActions(actionsString));
					}
					catch (Exception e)
					{
						FiddlerApplication.Log.LogFormat("Error deserializing rule actions: {0}", new object[] { e.ToString() });
					}
				}
				if (actions != null)
				{
					foreach (RuleAction action in actions)
					{
						if (!hasResponse || waitingForResponse || !isLive)
						{
							finalBehavior |= AutoResponder.HandleMatch(oSession, oCandidate, action);
						}
						if (hasResponse)
						{
							finalBehavior |= AutoResponder.HandleResponseMatch(oSession, oCandidate, action);
						}
					}
				}
			}
			return hasMatched;
		}

		/// <summary>
		/// Evaluate a Session (in progress or finished) to see if a given rule matches
		/// </summary>
		/// <param name="oS">The session to check</param>
		/// <param name="oCandidate">The rule to check</param>
		/// <param name="skipPercentCheck">skip the % match chance check (if present in the match condiditions).</param>
		/// <param name="isLive">Are we checking live traffic or an existing session</param>
		/// <returns>True if rule matches the session, False if not.</returns>
		// Token: 0x060000B4 RID: 180 RVA: 0x00006AE0 File Offset: 0x00004CE0
		internal static bool CheckMatch(Session oS, ResponderRule oCandidate, bool skipPercentCheck, bool isLive)
		{
			if (isLive && !oCandidate.IsEnabled)
			{
				return false;
			}
			List<RuleMatch> matches = new List<RuleMatch>
			{
				new RuleMatch
				{
					Type = RuleMatchType.MagicString,
					Value = oCandidate.sMatch
				}
			};
			RuleMatchCondition matchCondition = RuleMatchCondition.MatchAll;
			if (oCandidate.sMatch.StartsWith("{"))
			{
				try
				{
					JsonSerializerOptions serializeOptions = new JsonSerializerOptions
					{
						PropertyNamingPolicy = JsonNamingPolicy.CamelCase
					};
					RuleMatchCollection matchCollection = JsonSerializer.Deserialize<RuleMatchCollection>(oCandidate.sMatch, serializeOptions);
					matches = new List<RuleMatch>(AutoResponder.DecodeMatches(matchCollection.Matches));
					matchCondition = matchCollection.MatchCondition;
				}
				catch (Exception e)
				{
					FiddlerApplication.Log.LogFormat("Error deserializing rule matches: {0}", new object[] { e.ToString() });
				}
			}
			List<RuleMatchType> responseMatches = new List<RuleMatchType>
			{
				RuleMatchType.BodySize,
				RuleMatchType.Cookie,
				RuleMatchType.Duration,
				RuleMatchType.ResponseBody,
				RuleMatchType.ResponseHeader,
				RuleMatchType.Status
			};
			bool hasResponseMatch = matches.Exists((RuleMatch m) => responseMatches.Contains(m.Type));
			if (hasResponseMatch && !AutoResponder.ResponseAvailable(oS))
			{
				oS.oFlags["x-AutoResponder-Wait"] = "Response";
				return false;
			}
			foreach (RuleMatch match in matches)
			{
				if (match.Type == RuleMatchType.MagicString)
				{
					string sLookFor = match.Value;
					int ixPercent = sLookFor.IndexOf('%');
					if (ixPercent > 0 && ixPercent < 4)
					{
						int iMatchPercent;
						if (!skipPercentCheck && int.TryParse(sLookFor.Substring(0, ixPercent), out iMatchPercent) && AutoResponder._GetRandValue() > iMatchPercent)
						{
							return false;
						}
						match.Value = sLookFor.Substring(ixPercent + 1);
					}
				}
			}
			bool? hasMatched = null;
			foreach (RuleMatch match2 in matches)
			{
				bool checkResult = AutoResponder.CheckRuleMatch(oS.fullUrl, oS, match2);
				if (matchCondition == RuleMatchCondition.MatchNone)
				{
					checkResult = !checkResult;
				}
				if (hasMatched == null)
				{
					hasMatched = new bool?(checkResult);
				}
				if (matchCondition == RuleMatchCondition.MatchAny)
				{
					if (checkResult)
					{
						hasMatched = new bool?(true);
						break;
					}
				}
				else
				{
					hasMatched = hasMatched && checkResult;
				}
			}
			bool? flag = hasMatched;
			bool flag2 = true;
			return (flag.GetValueOrDefault() == flag2) & (flag != null);
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x00006D64 File Offset: 0x00004F64
		internal static bool CheckRuleMatch(string sURI, Session oSession, RuleMatch match)
		{
			if (oSession == null)
			{
				return false;
			}
			switch (match.Type)
			{
			case RuleMatchType.Protocol:
				if (!string.IsNullOrEmpty(match.Value))
				{
					HTTPRequestHeaders requestHeaders = oSession.RequestHeaders;
					return ((requestHeaders != null) ? requestHeaders.UriScheme : null) == match.Value.ToLowerInvariant();
				}
				return false;
			case RuleMatchType.Host:
				return ValueRules.SearchString(match.Condition, oSession.host, match.Value);
			case RuleMatchType.Path:
				return ValueRules.SearchString(match.Condition, oSession.PathAndQuery, match.Value);
			case RuleMatchType.Url:
				return ValueRules.SearchString(match.Condition, oSession.fullUrl, match.Value);
			case RuleMatchType.Status:
				return ValueRules.SearchString(match.Condition, oSession.responseCode.ToString(), match.Value);
			case RuleMatchType.Method:
				return ValueRules.SearchString(match.Condition, oSession.RequestMethod, match.Value);
			case RuleMatchType.Process:
				return ValueRules.SearchString(match.Condition, oSession.LocalProcess, match.Value);
			case RuleMatchType.ClientIP:
				return ValueRules.SearchString(match.Condition, oSession.clientIP, match.Value);
			case RuleMatchType.RemoteIP:
				return ValueRules.SearchString(match.Condition, oSession.m_hostIP, match.Value);
			case RuleMatchType.BodySize:
				if (AutoResponder.ResponseAvailable(oSession))
				{
					string size = oSession.ResponseHeaders["Content-Length"] ?? ((long)oSession.ResponseBody.Length).ToString();
					return ValueRules.SearchString(match.Condition, size, match.Value);
				}
				return false;
			case RuleMatchType.Duration:
				if (!AutoResponder.ResponseAvailable(oSession))
				{
					return false;
				}
				if (oSession.Timers.Duration != -1L)
				{
					return ValueRules.SearchString(match.Condition, oSession.Timers.Duration.ToString(), match.Value);
				}
				if (oSession.Timers.ClientBeginRequest < oSession.Timers.ServerDoneResponse)
				{
					int duration = Convert.ToInt32((oSession.Timers.ServerDoneResponse - oSession.Timers.ClientBeginRequest).TotalMilliseconds);
					return ValueRules.SearchString(match.Condition, duration.ToString(), match.Value);
				}
				return ValueRules.SearchString(match.Condition, oSession.Timers.Duration.ToString(), match.Value);
			case RuleMatchType.Comment:
				return ValueRules.SearchString(match.Condition, oSession.oFlags["ui-comments"] ?? string.Empty, match.Value);
			case RuleMatchType.RequestBody:
				return ValueRules.SearchString(match.Condition, oSession.GetRequestBodyAsString(), match.Value);
			case RuleMatchType.RequestHeader:
				return KeyValueRules.SearchString(match.Condition, match.Key, match.Value, oSession.RequestHeaders.ToArray());
			case RuleMatchType.ResponseBody:
				return AutoResponder.ResponseAvailable(oSession) && ValueRules.SearchString(match.Condition, oSession.GetResponseBodyAsString(), match.Value);
			case RuleMatchType.ResponseHeader:
				return AutoResponder.ResponseAvailable(oSession) && KeyValueRules.SearchString(match.Condition, match.Key, match.Value, oSession.ResponseHeaders.ToArray());
			case RuleMatchType.Timer:
				return ValueRules.SearchString(match.Condition, oSession.Timers.ClientBeginRequest.ToString("o"), match.Value);
			case RuleMatchType.Cookie:
				if (AutoResponder.ResponseAvailable(oSession))
				{
					IEnumerable<HTTPHeaderItem> cookies = KeyValueRules.GetCookies(oSession);
					return KeyValueRules.SearchString(match.Condition, match.Key, match.Value, cookies);
				}
				return false;
			case RuleMatchType.MagicString:
				return AutoResponder.CheckMatchMagicString(sURI, oSession, match.Value);
			default:
				FiddlerApplication.Log.LogFormat("Unknown match type: {0}", new object[] { match.Type });
				return false;
			}
		}

		/// <summary>
		/// Check a Session to see if a given rule matches.
		///
		/// TODO: We probably should build a full tokenizer here, so that we can support
		/// keywords like ONCE so on...
		/// </summary>
		/// <param name="sURI">URI String</param>
		/// <param name="oSession">(Optional) Session object (used for URLWithBody: and Method: rules)</param>
		/// <param name="sLookFor">String containing the match rule's text</param>
		/// <returns>TRUE if the session matches</returns>
		// Token: 0x060000B6 RID: 182 RVA: 0x00007114 File Offset: 0x00005314
		internal static bool CheckMatchMagicString(string sURI, Session oSession, string sLookFor)
		{
			string sBodyToMatch = null;
			if (sLookFor.OICStartsWith("METHOD:"))
			{
				if (oSession == null || !Utilities.HasHeaders(oSession.oRequest))
				{
					return false;
				}
				sLookFor = sLookFor.Substring(7);
				bool bNotOperator = false;
				if (sLookFor.OICStartsWith("NOT:"))
				{
					bNotOperator = true;
					sLookFor = sLookFor.Substring(4);
				}
				string sMethodToMatch = Utilities.TrimAfter(sLookFor, ' ');
				bool bMethodMatched = oSession.HTTPMethodIs(sMethodToMatch);
				if (bNotOperator)
				{
					bMethodMatched = !bMethodMatched;
				}
				if (!bMethodMatched)
				{
					return false;
				}
				sLookFor = (sLookFor.Contains(" ") ? Utilities.TrimBefore(sLookFor, ' ') : "*");
			}
			if (sLookFor.OICStartsWith("URLWithBody:"))
			{
				sLookFor = sLookFor.Substring(12);
				sBodyToMatch = Utilities.TrimBefore(sLookFor, ' ');
				sLookFor = Utilities.TrimAfter(sLookFor, ' ');
			}
			if (sLookFor.OICStartsWith("HEADER:"))
			{
				if (oSession == null || !Utilities.HasHeaders(oSession.oRequest))
				{
					return false;
				}
				sLookFor = sLookFor.Substring(7);
				bool bNotOperator2 = false;
				if (sLookFor.OICStartsWith("NOT:"))
				{
					bNotOperator2 = true;
					sLookFor = sLookFor.Substring(4);
				}
				bool bHeaderMatched;
				if (sLookFor.Contains("="))
				{
					string sHeaderName = Utilities.TrimAfter(sLookFor, "=");
					string sHeaderValue = Utilities.TrimBefore(sLookFor, "=");
					bHeaderMatched = oSession.oRequest.headers.ExistsAndContains(sHeaderName, sHeaderValue);
				}
				else
				{
					bHeaderMatched = oSession.oRequest.headers.Exists(sLookFor);
				}
				if (bNotOperator2)
				{
					bHeaderMatched = !bHeaderMatched;
				}
				return bHeaderMatched;
			}
			else if (sLookFor.OICStartsWith("FLAG:"))
			{
				if (oSession == null)
				{
					return false;
				}
				bool bNotOperator3 = false;
				sLookFor = sLookFor.Substring(5);
				if (sLookFor.OICStartsWith("NOT:"))
				{
					bNotOperator3 = true;
					sLookFor = sLookFor.Substring(4);
				}
				bool bFlagMatched;
				if (sLookFor.Contains("="))
				{
					string sFlagName = Utilities.TrimAfter(sLookFor, "=");
					string sFlagValue = Utilities.TrimBefore(sLookFor, "=");
					string sVal = oSession.oFlags[sFlagName];
					if (sVal == null)
					{
						return false;
					}
					bFlagMatched = sVal.OICContains(sFlagValue);
				}
				else
				{
					bFlagMatched = oSession.oFlags.ContainsKey(sLookFor);
				}
				if (bNotOperator3)
				{
					bFlagMatched = !bFlagMatched;
				}
				return bFlagMatched;
			}
			else
			{
				if (sLookFor.Length > 6 && sLookFor.OICStartsWith("REGEX:"))
				{
					string sRegEx = sLookFor.Substring(6);
					try
					{
						Regex r = new Regex(sRegEx);
						Match i = r.Match(sURI);
						if (i.Success)
						{
							if (!AutoResponder.IsBodyMatch(oSession, sBodyToMatch))
							{
								return false;
							}
							return true;
						}
					}
					catch
					{
					}
					return false;
				}
				if (sLookFor.Length > 6 && sLookFor.OICStartsWith("EXACT:"))
				{
					string sMatch = sLookFor.Substring(6);
					return sMatch.Equals(sURI, StringComparison.Ordinal) && AutoResponder.IsBodyMatch(oSession, sBodyToMatch);
				}
				if (sLookFor.Length > 4 && sLookFor.OICStartsWith("NOT:"))
				{
					string sMatch2 = sLookFor.Substring(4);
					return !sURI.OICContains(sMatch2) && AutoResponder.IsBodyMatch(oSession, sBodyToMatch);
				}
				return ("*" == sLookFor || sURI.OICContains(sLookFor)) && AutoResponder.IsBodyMatch(oSession, sBodyToMatch);
			}
		}

		/// <summary>
		/// Check if response is available on a session. If not, mark the session for further processing.
		/// </summary>
		/// <param name="oSession">The session object to check and mark</param>
		/// <returns>True if response is available</returns>
		// Token: 0x060000B7 RID: 183 RVA: 0x00007418 File Offset: 0x00005618
		private static bool ResponseAvailable(Session oSession)
		{
			if (oSession.state < SessionStates.AutoTamperResponseBefore)
			{
				oSession.oFlags["x-breakresponse"] = "AutoResponder";
				oSession.bBufferResponse = true;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Get a random number from 1 to 100. Math.Random is not thread-safe, and creating a new one for each thread results in unwanted
		/// reuse of the same seed due to use of tickcount as the initializer.
		/// </summary>
		/// <returns></returns>
		// Token: 0x060000B8 RID: 184 RVA: 0x00007444 File Offset: 0x00005644
		private static int _GetRandValue()
		{
			Random obj = AutoResponder.myRand;
			int result;
			lock (obj)
			{
				result = AutoResponder.myRand.Next(1, 101);
			}
			return result;
		}

		/// <summary>
		/// Checks the Request Body for sBodyToMatch
		/// </summary>
		/// <param name="oSession">The Session whose requestBodyBytes will be evaluated as a decoded string</param>
		/// <param name="sBodyToMatch">Text that must be in the body.</param>
		/// <returns>TRUE if the Request body contains sBodyToMatch or if sBodyToMatch is null/empty</returns>
		// Token: 0x060000B9 RID: 185 RVA: 0x0000748C File Offset: 0x0000568C
		private static bool IsBodyMatch(Session oSession, string sBodyToMatch)
		{
			if (oSession == null || string.IsNullOrEmpty(sBodyToMatch))
			{
				return true;
			}
			try
			{
				string sBodyData = oSession.GetRequestBodyAsString();
				if (string.IsNullOrEmpty(sBodyData))
				{
					return false;
				}
				if (sBodyToMatch.Length > 6 && sBodyToMatch.OICStartsWith("REGEX:"))
				{
					string sRegEx = sBodyToMatch.Substring(6);
					try
					{
						Regex r = new Regex(sRegEx);
						Match i = r.Match(sBodyData);
						if (i.Success)
						{
							return true;
						}
					}
					catch
					{
					}
					return false;
				}
				if (sBodyToMatch.Length > 6 && sBodyToMatch.OICStartsWith("EXACT:"))
				{
					string sMatch = sBodyToMatch.Substring(6);
					if (sMatch.Equals(sBodyData, StringComparison.Ordinal))
					{
						return true;
					}
					return false;
				}
				else if (sBodyToMatch.Length > 4 && sBodyToMatch.OICStartsWith("NOT:"))
				{
					string sMatch2 = sBodyToMatch.Substring(4);
					if (!sBodyData.OICContains(sMatch2))
					{
						return true;
					}
					return false;
				}
				else if (sBodyData.OICContains(sBodyToMatch))
				{
					return true;
				}
			}
			catch
			{
				return false;
			}
			return false;
		}

		/// <summary>
		/// Gets or Sets a flag indicating whether the rule-list has been modified.
		/// This member points out an architectural flaw, since direct modification of the rule
		/// should set the dirty bit for the list automatically. Ah well, live and learn.
		/// </summary>
		// Token: 0x17000022 RID: 34
		// (get) Token: 0x060000BA RID: 186 RVA: 0x0000759C File Offset: 0x0000579C
		// (set) Token: 0x060000BB RID: 187 RVA: 0x000075A4 File Offset: 0x000057A4
		public bool IsRuleListDirty
		{
			get
			{
				return this._bRuleListIsDirty;
			}
			set
			{
				this._bRuleListIsDirty = value;
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x060000BC RID: 188 RVA: 0x000075AD File Offset: 0x000057AD
		// (set) Token: 0x060000BD RID: 189 RVA: 0x000075B5 File Offset: 0x000057B5
		public List<ResponderRule> Rules { get; private set; } = new List<ResponderRule>();

		/// <summary>
		/// Notify all AcceptAllConnects, Enabled and UnmatchedRequestsPassthrough property 
		/// subscribers with the current property values.
		/// </summary>
		// Token: 0x060000BE RID: 190 RVA: 0x000075BE File Offset: 0x000057BE
		public void EmitSettings()
		{
			this.enabledSubject.OnNext(this.IsEnabled);
			this.unmatchedRequestsPassthroughSubject.OnNext(this.PermitFallthrough);
			this.acceptAllConnectsSubject.OnNext(this.AcceptAllConnects);
		}

		/// <summary>
		/// Magic String shown in the UI to trigger the File Picker dialog
		/// </summary>
		// Token: 0x04000026 RID: 38
		internal static readonly string STR_FIND_FILE = "Find a file...";

		/// <summary>
		/// Magic String shown in the UI to trigger editing of a blank response
		/// </summary>
		// Token: 0x04000027 RID: 39
		internal static readonly string STR_CREATE_NEW = "Create New Response...";

		// Token: 0x04000028 RID: 40
		private bool _bUseLatency;

		// Token: 0x04000029 RID: 41
		private static string sColorAutoResponded = "Lavender";

		// Token: 0x0400002A RID: 42
		private ReaderWriterLockSlim _RWLockRules = new ReaderWriterLockSlim();

		// Token: 0x0400002B RID: 43
		private Dictionary<string, ResponderGroup> groups = new Dictionary<string, ResponderGroup>();

		// Token: 0x0400002C RID: 44
		private bool _bRuleListIsDirty;

		// Token: 0x0400002D RID: 45
		private bool isEnabled;

		// Token: 0x0400002F RID: 47
		private readonly ISubject<bool> enabledSubject;

		// Token: 0x04000030 RID: 48
		private bool permitFallthrough = true;

		// Token: 0x04000032 RID: 50
		private readonly ISubject<bool> unmatchedRequestsPassthroughSubject;

		// Token: 0x04000033 RID: 51
		private bool acceptAllConnects;

		// Token: 0x04000035 RID: 53
		private readonly ISubject<bool> acceptAllConnectsSubject;

		/// <summary>
		/// Random instance used for "%" rules
		/// </summary>
		// Token: 0x04000036 RID: 54
		private static Random myRand = new Random();
	}
}
