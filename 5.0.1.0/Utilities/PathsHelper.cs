using System;
using System.IO;
using System.Reflection;

namespace FiddlerCore.Utilities
{
	// Token: 0x02000077 RID: 119
	internal static class PathsHelper
	{
		// Token: 0x170000EE RID: 238
		// (get) Token: 0x060005B6 RID: 1462 RVA: 0x00033DCC File Offset: 0x00031FCC
		public static string RootDirectory
		{
			get
			{
				if (PathsHelper.rootDirectory == null)
				{
					Assembly assembly = Assembly.GetEntryAssembly();
					if (assembly == null)
					{
						assembly = typeof(PathsHelper).Assembly;
					}
					PathsHelper.rootDirectory = Path.GetDirectoryName(assembly.Location);
				}
				return PathsHelper.rootDirectory;
			}
		}

		// Token: 0x040002A5 RID: 677
		private static string rootDirectory;
	}
}
