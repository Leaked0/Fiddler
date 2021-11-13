using System;
using System.Reflection;
using Telerik.NetworkConnections;

namespace FiddlerCore.Analytics
{
	// Token: 0x020000B5 RID: 181
	internal static class AnalyticsExtensions
	{
		// Token: 0x060006CE RID: 1742 RVA: 0x00037691 File Offset: 0x00035891
		public static void TrackMachineInformation(this IAnalytics analytics, string osVersion, string dotNetVersion, string processor)
		{
			if (analytics == null)
			{
				return;
			}
			analytics.TrackFeature("MachineInfo", "OS", osVersion);
			analytics.TrackFeature("MachineInfo", "_NET", dotNetVersion);
			analytics.TrackFeature("MachineInfo", "CPU", processor);
		}

		// Token: 0x060006CF RID: 1743 RVA: 0x000376CC File Offset: 0x000358CC
		public static void TrackApplicationInformation(this IAnalytics analytics)
		{
			analytics.TrackFeature("TargetFramework", "NETSTANDARD2_1", null);
			Assembly entryAssembly = Assembly.GetEntryAssembly();
			string applicationName;
			if (entryAssembly == null)
			{
				applicationName = "Unmanaged";
			}
			else
			{
				string entryAssemblyFullName = entryAssembly.FullName;
				AssemblyName entryAssemblyName = new AssemblyName(entryAssemblyFullName);
				applicationName = entryAssemblyName.Name;
			}
			analytics.TrackFeature("ApplicationName", applicationName, null);
		}

		// Token: 0x060006D0 RID: 1744 RVA: 0x00037724 File Offset: 0x00035924
		public static void TrackSystemProxyInfo(this IAnalytics analytics, ProxySettings systemProxySettings)
		{
			if (analytics == null || systemProxySettings == null)
			{
				return;
			}
			if (systemProxySettings.UseWebProxyAutoDiscovery)
			{
				analytics.TrackFeature("SystemProxyInfo", "AutoDetect", null);
			}
			if (systemProxySettings.ProxyAutoConfigEnabled && systemProxySettings.ProxyAutoConfigUrl != null)
			{
				analytics.TrackFeature("SystemProxyInfo", "UseConfigScript", null);
			}
			if (systemProxySettings.HttpProxyEnabled && systemProxySettings.HttpsProxyEnabled && systemProxySettings.FtpProxyEnabled && systemProxySettings.SocksProxyEnabled)
			{
				analytics.TrackFeature("SystemProxyInfo", "AllValidProtocolsEnabled", null);
				return;
			}
			if (systemProxySettings.HttpProxyEnabled)
			{
				analytics.TrackFeature("SystemProxyInfo", "HttpEnabled", null);
			}
			if (systemProxySettings.HttpsProxyEnabled)
			{
				analytics.TrackFeature("SystemProxyInfo", "HttpsEnabled", null);
			}
			if (systemProxySettings.FtpProxyEnabled)
			{
				analytics.TrackFeature("SystemProxyInfo", "FtpEnabled", null);
			}
			if (systemProxySettings.SocksProxyEnabled)
			{
				analytics.TrackFeature("SystemProxyInfo", "SocksEnabled", null);
			}
		}
	}
}
