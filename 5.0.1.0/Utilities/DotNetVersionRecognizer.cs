using System;
using System.Reflection;
using System.Runtime;

namespace FiddlerCore.Utilities
{
	/// <summary>
	/// Provides methods which recognize the .NET Frameworks on the user machine.
	/// </summary>
	// Token: 0x0200007A RID: 122
	internal static class DotNetVersionRecognizer
	{
		/// <summary>
		/// This method tries to get the highest installed .NET Framework version for the current CLR. If it succeeds, it returns it.
		/// Otherwise it returns the version which the Environment.Version property returns.
		/// </summary>
		/// <returns>The highest .NET Framework installed for the current CLR if found. If any framework was found the method returns the environment version.</returns>
		// Token: 0x060005C4 RID: 1476 RVA: 0x00034100 File Offset: 0x00032300
		public static string GetHighestVersionInstalledForCurrentClr()
		{
			if (DotNetVersionRecognizer.recognizedDotNetVersion == null && !DotNetVersionRecognizer.TryGetHighestVersionInstalledForCurrentClr(out DotNetVersionRecognizer.recognizedDotNetVersion))
			{
				DotNetVersionRecognizer.recognizedDotNetVersion = Environment.Version.ToString();
			}
			return DotNetVersionRecognizer.recognizedDotNetVersion;
		}

		/// <summary>
		/// This method tries to detect which CLR is running the application and then finds the highest framework version installed for that CLR.
		/// If it succeeds it returns true and the version is assigned to the <paramref name="version" />.
		/// Otherwise the method returns false and assigns null to <paramref name="version" />.
		/// <para>If there are exceptions, they will be caught and reported to the Telerik.Analytics.</para>
		/// </summary>
		/// <param name="version">out: The version of the .NET Framework</param>
		/// <returns>Returns true if a .NET Framework version is assigned to <paramref name="version" />
		/// and false when the <paramref name="version" /> is assigned null.</returns>
		// Token: 0x060005C5 RID: 1477 RVA: 0x00034138 File Offset: 0x00032338
		private static bool TryGetHighestVersionInstalledForCurrentClr(out string version)
		{
			try
			{
				return DotNetVersionRecognizer.GetNetCoreVersion(out version);
			}
			catch (Exception ex)
			{
			}
			version = null;
			return false;
		}

		// Token: 0x060005C6 RID: 1478 RVA: 0x00034168 File Offset: 0x00032368
		private static bool GetNetCoreVersion(out string version)
		{
			Assembly assembly = typeof(GCSettings).GetTypeInfo().Assembly;
			string[] assemblyPath = assembly.CodeBase.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
			int netCoreAppIndex = Array.IndexOf<string>(assemblyPath, "Microsoft.NETCore.App");
			if (netCoreAppIndex > 0 && netCoreAppIndex < assemblyPath.Length - 2)
			{
				version = assemblyPath[netCoreAppIndex + 1];
				return true;
			}
			version = null;
			return false;
		}

		// Token: 0x040002A6 RID: 678
		private static string recognizedDotNetVersion;
	}
}
