using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FiddlerCore.Utilities
{
	// Token: 0x02000079 RID: 121
	internal static class Utilities
	{
		/// <summary>
		/// Format an Exception message, including InnerException message if present.
		/// </summary>
		/// <param name="eX"></param>
		/// <returns></returns>
		// Token: 0x060005BE RID: 1470 RVA: 0x00033EC8 File Offset: 0x000320C8
		public static string DescribeException(Exception eX)
		{
			StringBuilder oSB = new StringBuilder(512);
			oSB.AppendFormat("{0} {1}", eX.GetType(), eX.Message);
			if (eX.InnerException != null)
			{
				oSB.AppendFormat(" < {0}", eX.InnerException.Message);
			}
			return oSB.ToString();
		}

		// Token: 0x060005BF RID: 1471 RVA: 0x00033F20 File Offset: 0x00032120
		public static bool RunExecutableAndWait(string sExecute, string sParams, out string errorMessage)
		{
			errorMessage = null;
			bool result;
			try
			{
				Process oProc = new Process();
				oProc.StartInfo.FileName = sExecute;
				oProc.StartInfo.Arguments = sParams;
				oProc.Start();
				oProc.WaitForExit();
				bool isSuccessful = true;
				if (oProc.ExitCode != 0)
				{
					isSuccessful = false;
				}
				oProc.Dispose();
				result = isSuccessful;
			}
			catch (Exception eX)
			{
				if (!(eX is Win32Exception) || 1223 != (eX as Win32Exception).NativeErrorCode)
				{
					errorMessage = "Fiddler Exception thrown: " + eX.ToString() + "\r\n" + eX.StackTrace.ToString();
				}
				result = false;
			}
			return result;
		}

		// Token: 0x060005C0 RID: 1472 RVA: 0x00033FC4 File Offset: 0x000321C4
		public static bool CheckIfFileHasBOM(string filename)
		{
			UTF8Encoding encoding = new UTF8Encoding(true);
			byte[] preamble = encoding.GetPreamble();
			int preambleLenght = preamble.Length;
			byte[] buffer = new byte[preambleLenght];
			using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read))
			{
				fileStream.Read(buffer, 0, buffer.Length);
				fileStream.Close();
			}
			for (int i = 0; i < preambleLenght; i++)
			{
				if (preamble[i] != buffer[i])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060005C1 RID: 1473 RVA: 0x00034044 File Offset: 0x00032244
		public static bool IsVaraibleIsInFile(string pattern, string path)
		{
			bool isVariableIsInFile = false;
			using (StreamReader file = new StreamReader(path))
			{
				string line = string.Empty;
				while ((line = file.ReadLine()) != null)
				{
					Regex regex = new Regex(pattern);
					if (regex.Match(line).Success)
					{
						isVariableIsInFile = true;
						break;
					}
				}
			}
			return isVariableIsInFile;
		}

		// Token: 0x060005C2 RID: 1474 RVA: 0x000340A4 File Offset: 0x000322A4
		public static string[] SplitHostAndPort(string hostAndPort)
		{
			string[] result = null;
			if (!string.IsNullOrEmpty(hostAndPort))
			{
				result = hostAndPort.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
			}
			if (result != null && result.Length != 2)
			{
				result = Utilities.CreateArrayWithTwoZeros();
			}
			if (result == null)
			{
				result = Utilities.CreateArrayWithTwoZeros();
			}
			return result;
		}

		// Token: 0x060005C3 RID: 1475 RVA: 0x000340E7 File Offset: 0x000322E7
		private static string[] CreateArrayWithTwoZeros()
		{
			return new string[] { "0", "0" };
		}
	}
}
