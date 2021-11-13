using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Fiddler
{
	/// <summary>
	/// Fiddler Transcoders allow import and export of Sessions from Fiddler
	/// </summary>
	// Token: 0x0200002C RID: 44
	public class FiddlerTranscoders : IDisposable
	{
		/// <summary>
		/// Create the FiddlerTranscoders object
		/// </summary>
		// Token: 0x060001AE RID: 430 RVA: 0x00014135 File Offset: 0x00012335
		internal FiddlerTranscoders()
		{
		}

		/// <summary>
		/// True if one or more classes implementing ISessionImporter are available.
		/// </summary>
		// Token: 0x17000058 RID: 88
		// (get) Token: 0x060001AF RID: 431 RVA: 0x00014153 File Offset: 0x00012353
		internal bool hasImporters
		{
			get
			{
				return this.m_Importers != null && this.m_Importers.Count > 0;
			}
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x00014170 File Offset: 0x00012370
		internal string[] getImportFormats()
		{
			this.EnsureTranscoders();
			if (!this.hasImporters)
			{
				return new string[0];
			}
			string[] arrResult = new string[this.m_Importers.Count];
			this.m_Importers.Keys.CopyTo(arrResult, 0);
			return arrResult;
		}

		// Token: 0x060001B1 RID: 433 RVA: 0x000141B8 File Offset: 0x000123B8
		internal string[] getExportFormats()
		{
			this.EnsureTranscoders();
			if (!this.hasExporters)
			{
				return new string[0];
			}
			string[] arrResult = new string[this.m_Exporters.Count];
			this.m_Exporters.Keys.CopyTo(arrResult, 0);
			return arrResult;
		}

		/// <summary>
		/// List all of the Transcoder objects that are loaded
		/// </summary>
		/// <returns></returns>
		// Token: 0x060001B2 RID: 434 RVA: 0x00014200 File Offset: 0x00012400
		public override string ToString()
		{
			StringBuilder sbFormats = new StringBuilder();
			sbFormats.AppendLine("IMPORT FORMATS");
			foreach (string s in this.getImportFormats())
			{
				sbFormats.AppendFormat("\t{0}\n", s);
			}
			sbFormats.AppendLine("\nEXPORT FORMATS");
			foreach (string s2 in this.getExportFormats())
			{
				sbFormats.AppendFormat("\t{0}\n", s2);
			}
			return sbFormats.ToString();
		}

		/// <summary>
		/// True if one or more classes implementing ISessionImporter are available.
		/// </summary>
		// Token: 0x17000059 RID: 89
		// (get) Token: 0x060001B3 RID: 435 RVA: 0x00014287 File Offset: 0x00012487
		internal bool hasExporters
		{
			get
			{
				return this.m_Exporters != null && this.m_Exporters.Count > 0;
			}
		}

		/// <summary>
		/// Add Import/Export encoders to FiddlerApplication.oTranscoders
		/// </summary>
		/// <param name="sAssemblyPath">Assembly to import exporters and importers</param>
		/// <returns>FALSE on obvious errors</returns>
		// Token: 0x060001B4 RID: 436 RVA: 0x000142A4 File Offset: 0x000124A4
		public bool ImportTranscoders(string sAssemblyPath)
		{
			try
			{
				if (!File.Exists(sAssemblyPath))
				{
					return false;
				}
				if (!CONFIG.bRunningOnCLRv4)
				{
					throw new Exception("Not reachable.");
				}
				Assembly a = Assembly.UnsafeLoadFrom(sAssemblyPath);
				if (!this.ScanAssemblyForTranscoders(a))
				{
					return false;
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("Failed to load Transcoders from {0}; exception {1}", new object[] { sAssemblyPath, eX.Message });
				return false;
			}
			return true;
		}

		/// <summary>
		/// Add Import/Export encoders to FiddlerApplication.oTranscoders
		/// </summary>
		/// <param name="assemblyInput">Assembly to scan for transcoders</param>
		/// <returns>FALSE on obvious errors</returns>
		// Token: 0x060001B5 RID: 437 RVA: 0x00014324 File Offset: 0x00012524
		public bool ImportTranscoders(Assembly assemblyInput)
		{
			try
			{
				if (!this.ScanAssemblyForTranscoders(assemblyInput))
				{
					return false;
				}
			}
			catch (Exception eX)
			{
				FiddlerApplication.Log.LogFormat("Failed to load Transcoders from {0}; exception {1}", new object[] { assemblyInput.Location, eX.Message });
				return false;
			}
			return true;
		}

		// Token: 0x060001B6 RID: 438 RVA: 0x00014380 File Offset: 0x00012580
		private void ScanPathForTranscoders(string sPath)
		{
			this.ScanPathForTranscoders(sPath, false);
		}

		/// <summary>
		/// Loads any assembly in the specified path that ends with .dll and does not start with "_", checks that a compatible version requirement was specified, 
		/// and adds the importer and exporters within to the collection.
		/// </summary>
		/// <param name="sPath">The path to scan for extensions</param>
		// Token: 0x060001B7 RID: 439 RVA: 0x0001438C File Offset: 0x0001258C
		private void ScanPathForTranscoders(string sPath, bool bIsSubfolder)
		{
			try
			{
				if (Directory.Exists(sPath))
				{
					bool bNoisyLogging = FiddlerApplication.Prefs.GetBoolPref("fiddler.debug.extensions.verbose", false);
					if (bNoisyLogging)
					{
						FiddlerApplication.Log.LogFormat("Searching for Transcoders under {0}", new object[] { sPath });
					}
					if (!bIsSubfolder)
					{
						DirectoryInfo[] oDirectories = new DirectoryInfo(sPath).GetDirectories("*.ext");
						foreach (DirectoryInfo oDir in oDirectories)
						{
							this.ScanPathForTranscoders(oDir.FullName, true);
						}
					}
					FileInfo[] oFiles = new DirectoryInfo(sPath).GetFiles(bIsSubfolder ? "Fiddler*.dll" : "*.dll");
					foreach (FileInfo oFile in oFiles)
					{
						if (bIsSubfolder || !Utilities.IsNotExtension(oFile.Name))
						{
							if (bNoisyLogging)
							{
								FiddlerApplication.Log.LogFormat("Looking for Transcoders inside {0}", new object[] { oFile.FullName.ToString() });
							}
							Assembly a;
							try
							{
								if (!CONFIG.bRunningOnCLRv4)
								{
									throw new Exception("Not reachable");
								}
								a = Assembly.UnsafeLoadFrom(oFile.FullName);
							}
							catch (Exception eX)
							{
								FiddlerApplication.LogAddonException(eX, "Failed to load " + oFile.FullName);
								goto IL_125;
							}
							this.ScanAssemblyForTranscoders(a);
						}
						IL_125:;
					}
				}
			}
			catch (Exception eX2)
			{
				string title = "Transcoders Load Error";
				string message = string.Format("[Fiddler] Failure loading Transcoders: {0}", eX2.Message);
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[] { title, message });
			}
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x00014544 File Offset: 0x00012744
		private bool ScanAssemblyForTranscoders(Assembly assemblyInput)
		{
			bool bFoundTranscoders = false;
			bool bNoisyLogging = FiddlerApplication.Prefs.GetBoolPref("fiddler.debug.extensions.verbose", false);
			try
			{
				if (!Utilities.FiddlerMeetsVersionRequirement(assemblyInput, "Importers and Exporters"))
				{
					FiddlerApplication.Log.LogFormat("Assembly {0} did not specify a RequiredVersionAttribute. Aborting load of transcoders.", new object[] { assemblyInput.CodeBase });
					return false;
				}
				foreach (Type t in assemblyInput.GetExportedTypes())
				{
					if (!t.IsAbstract && t.IsPublic && t.IsClass)
					{
						if (typeof(ISessionImporter).IsAssignableFrom(t))
						{
							try
							{
								if (!FiddlerTranscoders.AddToImportOrExportCollection(this.m_Importers, t))
								{
									FiddlerApplication.Log.LogFormat("WARNING: SessionImporter {0} from {1} failed to specify any ImportExportFormat attributes.", new object[] { t.Name, assemblyInput.CodeBase });
								}
								else
								{
									bFoundTranscoders = true;
									if (bNoisyLogging)
									{
										FiddlerApplication.Log.LogFormat("    Added SessionImporter {0}", new object[] { t.FullName });
									}
								}
							}
							catch (Exception eX)
							{
								string title = "Extension Load Error";
								string message = string.Format("[Fiddler] Failure loading {0} SessionImporter from {1}: {2}\n\n{3}\n\n{4}", new object[] { t.Name, assemblyInput.CodeBase, eX.Message, eX.StackTrace, eX.InnerException });
								FiddlerApplication.Log.LogFormat("{0}: {1}", new object[] { title, message });
							}
						}
						if (typeof(ISessionExporter).IsAssignableFrom(t))
						{
							try
							{
								if (!FiddlerTranscoders.AddToImportOrExportCollection(this.m_Exporters, t))
								{
									FiddlerApplication.Log.LogFormat("WARNING: SessionExporter {0} from {1} failed to specify any ImportExportFormat attributes.", new object[] { t.Name, assemblyInput.CodeBase });
								}
								else
								{
									bFoundTranscoders = true;
									if (bNoisyLogging)
									{
										FiddlerApplication.Log.LogFormat("    Added SessionExporter {0}", new object[] { t.FullName });
									}
								}
							}
							catch (Exception eX2)
							{
								string title2 = "Extension Load Error";
								string message2 = string.Format("[Fiddler] Failure loading {0} SessionExporter from {1}: {2}\n\n{3}\n\n{4}", new object[] { t.Name, assemblyInput.CodeBase, eX2.Message, eX2.StackTrace, eX2.InnerException });
								FiddlerApplication.Log.LogFormat("{0}: {1}", new object[] { title2, message2 });
							}
						}
					}
				}
			}
			catch (Exception eX3)
			{
				string title3 = "Extension Load Error";
				string message3 = string.Format("[Fiddler] Failure loading Importer/Exporter from {0}: {1}", assemblyInput.CodeBase, eX3.Message);
				FiddlerApplication.Log.LogFormat("{0}: {1}", new object[] { title3, message3 });
				return false;
			}
			return bFoundTranscoders;
		}

		/// <summary>
		/// Ensures that Import/Export Transcoders have been loaded
		/// </summary>
		// Token: 0x060001B9 RID: 441 RVA: 0x0001483C File Offset: 0x00012A3C
		private void EnsureTranscoders()
		{
		}

		/// <summary>
		/// Returns a TranscoderTuple willing to handle the specified format
		/// </summary>
		/// <param name="sExportFormat">The Format</param>
		/// <returns>TranscoderTuple, or null</returns>
		// Token: 0x060001BA RID: 442 RVA: 0x00014840 File Offset: 0x00012A40
		public TranscoderTuple GetExporter(string sExportFormat)
		{
			this.EnsureTranscoders();
			if (this.m_Exporters == null)
			{
				return null;
			}
			TranscoderTuple ttVal;
			if (!this.m_Exporters.TryGetValue(sExportFormat, out ttVal))
			{
				return null;
			}
			return ttVal;
		}

		/// <summary>
		/// Returns a TranscoderTuple willing to handle the specified format
		/// </summary>
		/// <param name="sImportFormat">The Format</param>
		/// <returns>TranscoderTuple, or null</returns>
		// Token: 0x060001BB RID: 443 RVA: 0x00014870 File Offset: 0x00012A70
		public TranscoderTuple GetImporter(string sImportFormat)
		{
			this.EnsureTranscoders();
			if (this.m_Importers == null)
			{
				return null;
			}
			TranscoderTuple ttVal;
			if (!this.m_Importers.TryGetValue(sImportFormat, out ttVal))
			{
				return null;
			}
			return ttVal;
		}

		// Token: 0x060001BC RID: 444 RVA: 0x000148A0 File Offset: 0x00012AA0
		internal TranscoderTuple GetImporterForExtension(string sExt)
		{
			this.EnsureTranscoders();
			if (!this.hasImporters)
			{
				return null;
			}
			foreach (TranscoderTuple tt in this.m_Importers.Values)
			{
				if (tt.HandlesExtension(sExt))
				{
					return tt;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the format list of the specified type and adds that type to the collection.
		/// </summary>
		/// <param name="oCollection"></param>
		/// <param name="t"></param>
		/// <returns>TRUE if any formats were found; FALSE otherwise</returns>
		// Token: 0x060001BD RID: 445 RVA: 0x00014914 File Offset: 0x00012B14
		private static bool AddToImportOrExportCollection(Dictionary<string, TranscoderTuple> oCollection, Type t)
		{
			bool bHasFormatSpecifier = false;
			ProfferFormatAttribute[] oValues = (ProfferFormatAttribute[])Attribute.GetCustomAttributes(t, typeof(ProfferFormatAttribute));
			if (oValues != null && oValues.Length != 0)
			{
				bHasFormatSpecifier = true;
				foreach (ProfferFormatAttribute iFA in oValues)
				{
					if (!oCollection.ContainsKey(iFA.FormatName))
					{
						oCollection.Add(iFA.FormatName, new TranscoderTuple(iFA, t));
					}
				}
			}
			return bHasFormatSpecifier;
		}

		/// <summary>
		/// Clear Importer and Exporter collections
		/// </summary>
		// Token: 0x060001BE RID: 446 RVA: 0x0001497C File Offset: 0x00012B7C
		public void Dispose()
		{
			if (this.m_Exporters != null)
			{
				this.m_Exporters.Clear();
			}
			if (this.m_Importers != null)
			{
				this.m_Importers.Clear();
			}
			this.m_Importers = (this.m_Exporters = null);
		}

		// Token: 0x040000CD RID: 205
		internal Dictionary<string, TranscoderTuple> m_Importers = new Dictionary<string, TranscoderTuple>();

		// Token: 0x040000CE RID: 206
		internal Dictionary<string, TranscoderTuple> m_Exporters = new Dictionary<string, TranscoderTuple>();
	}
}
