using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FiddlerCore.PlatformExtensions.Windows
{
	// Token: 0x02000094 RID: 148
	internal static class PortProcessMapperForWindows
	{
		// Token: 0x06000619 RID: 1561 RVA: 0x00034604 File Offset: 0x00032804
		internal static bool TryMapLocalPortToProcessId(int iPort, bool bEnableIPv6, out int processId, out string errorMessage)
		{
			processId = PortProcessMapperForWindows.FindPIDForPort(iPort, bEnableIPv6, out errorMessage);
			return string.IsNullOrEmpty(errorMessage);
		}

		// Token: 0x0600061A RID: 1562 RVA: 0x0003461C File Offset: 0x0003281C
		internal static bool TryGetListeningProcess(int iPort, out string processName, out int processId, out string errorMessage)
		{
			bool result;
			try
			{
				int iPID = PortProcessMapperForWindows.FindPIDForConnection(iPort, 2U, PortProcessMapperForWindows.TcpTableType.OwnerPidListener, out errorMessage);
				if (iPID < 1)
				{
					iPID = PortProcessMapperForWindows.FindPIDForConnection(iPort, 23U, PortProcessMapperForWindows.TcpTableType.OwnerPidListener, out errorMessage);
				}
				processId = iPID;
				if (iPID < 1)
				{
					processName = string.Empty;
					if (string.IsNullOrEmpty(errorMessage))
					{
						result = true;
					}
					else
					{
						result = false;
					}
				}
				else
				{
					processName = Process.GetProcessById(iPID).ProcessName.ToLower();
					if (string.IsNullOrEmpty(processName))
					{
						processName = "unknown";
					}
					result = true;
				}
			}
			catch (Exception eX)
			{
				processName = string.Empty;
				processId = 0;
				errorMessage = "Unable to call IPHelperAPI function" + eX.Message;
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Given a local port number, uses GetExtendedTcpTable to find the originating process ID. 
		/// First checks the IPv4 connections, then looks at IPv6 connections.
		/// </summary>
		/// <param name="iTargetPort">Client applications' port</param>
		/// <returns>ProcessID, or 0 if not found</returns>
		// Token: 0x0600061B RID: 1563 RVA: 0x000346BC File Offset: 0x000328BC
		private static int FindPIDForPort(int iTargetPort, bool bEnableIPv6, out string errorMessage)
		{
			try
			{
				int iPID = PortProcessMapperForWindows.FindPIDForConnection(iTargetPort, 2U, PortProcessMapperForWindows.TcpTableType.OwnerPidConnections, out errorMessage);
				if (iPID > 0 || !bEnableIPv6)
				{
					return iPID;
				}
				return PortProcessMapperForWindows.FindPIDForConnection(iTargetPort, 23U, PortProcessMapperForWindows.TcpTableType.OwnerPidConnections, out errorMessage);
			}
			catch (Exception eX)
			{
				errorMessage = string.Format("Fiddler.Network.TCPTable> Unable to call IPHelperAPI function: {0}", eX.Message);
			}
			return 0;
		}

		/// <summary>
		/// Calls the GetExtendedTcpTable function to map a port to a process ID.
		/// This function is (over) optimized for performance.
		/// </summary>
		/// <param name="iTargetPort">Client port</param>
		/// <param name="iAddressType">AF_INET or AF_INET6</param>
		/// <returns>PID, if found, or 0</returns>
		// Token: 0x0600061C RID: 1564 RVA: 0x00034718 File Offset: 0x00032918
		private static int FindPIDForConnection(int iTargetPort, uint iAddressType, PortProcessMapperForWindows.TcpTableType whichTable, out string errorMessage)
		{
			IntPtr ptrTcpTable = IntPtr.Zero;
			uint cbBufferSize = 32768U;
			try
			{
				ptrTcpTable = Marshal.AllocHGlobal(32768);
				uint dwResult = PortProcessMapperForWindows.GetExtendedTcpTable(ptrTcpTable, ref cbBufferSize, false, iAddressType, whichTable, 0U);
				while (122U == dwResult)
				{
					Marshal.FreeHGlobal(ptrTcpTable);
					cbBufferSize += 2048U;
					ptrTcpTable = Marshal.AllocHGlobal((int)cbBufferSize);
					dwResult = PortProcessMapperForWindows.GetExtendedTcpTable(ptrTcpTable, ref cbBufferSize, false, iAddressType, whichTable, 0U);
				}
				if (dwResult != 0U)
				{
					errorMessage = string.Format("!GetExtendedTcpTable() returned error #0x{0:x} when looking for port {1}", dwResult, iTargetPort);
					return 0;
				}
				int iOffsetToFirstPort;
				int iOffsetToPIDInRow;
				int iTableRowSize;
				if (iAddressType == 2U)
				{
					iOffsetToFirstPort = 12;
					iOffsetToPIDInRow = 12;
					iTableRowSize = 24;
				}
				else
				{
					iOffsetToFirstPort = 24;
					iOffsetToPIDInRow = 32;
					iTableRowSize = 56;
				}
				int iTargetPortInNetOrder = ((iTargetPort & 255) << 8) + ((iTargetPort & 65280) >> 8);
				int iRowCount = Marshal.ReadInt32(ptrTcpTable);
				if (iRowCount == 0)
				{
					errorMessage = null;
					return 0;
				}
				IntPtr ptrRow = (IntPtr)((long)ptrTcpTable + (long)iOffsetToFirstPort);
				for (int i = 0; i < iRowCount; i++)
				{
					if (iTargetPortInNetOrder == Marshal.ReadInt32(ptrRow))
					{
						errorMessage = null;
						return Marshal.ReadInt32(ptrRow, iOffsetToPIDInRow);
					}
					ptrRow = (IntPtr)((long)ptrRow + (long)iTableRowSize);
				}
			}
			finally
			{
				Marshal.FreeHGlobal(ptrTcpTable);
			}
			errorMessage = null;
			return 0;
		}

		// Token: 0x0600061D RID: 1565
		[DllImport("iphlpapi.dll", ExactSpelling = true, SetLastError = true)]
		private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref uint dwTcpTableLength, [MarshalAs(UnmanagedType.Bool)] bool sort, uint ipVersion, PortProcessMapperForWindows.TcpTableType tcpTableType, uint reserved);

		// Token: 0x040002AE RID: 686
		private const int AF_INET = 2;

		// Token: 0x040002AF RID: 687
		private const int AF_INET6 = 23;

		// Token: 0x040002B0 RID: 688
		private const int ERROR_INSUFFICIENT_BUFFER = 122;

		// Token: 0x040002B1 RID: 689
		private const int NO_ERROR = 0;

		/// <summary>
		/// Enumeration of possible queries that can be issued using GetExtendedTcpTable
		/// http://msdn2.microsoft.com/en-us/library/aa366386.aspx
		/// </summary>
		// Token: 0x020000E0 RID: 224
		private enum TcpTableType
		{
			// Token: 0x0400039D RID: 925
			BasicListener,
			// Token: 0x0400039E RID: 926
			BasicConnections,
			// Token: 0x0400039F RID: 927
			BasicAll,
			/// <summary>
			/// Processes listening on Ports
			/// </summary>
			// Token: 0x040003A0 RID: 928
			OwnerPidListener,
			/// <summary>
			/// Processes with active TCP/IP connections
			/// </summary>
			// Token: 0x040003A1 RID: 929
			OwnerPidConnections,
			// Token: 0x040003A2 RID: 930
			OwnerPidAll,
			// Token: 0x040003A3 RID: 931
			OwnerModuleListener,
			// Token: 0x040003A4 RID: 932
			OwnerModuleConnections,
			// Token: 0x040003A5 RID: 933
			OwnerModuleAll
		}
	}
}
