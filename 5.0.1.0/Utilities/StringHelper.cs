using System;

namespace FiddlerCore.Utilities
{
	// Token: 0x02000078 RID: 120
	internal static class StringHelper
	{
		// Token: 0x060005B7 RID: 1463 RVA: 0x00033E14 File Offset: 0x00032014
		public static bool OICContains(this string inStr, string toMatch)
		{
			return inStr.IndexOf(toMatch, StringComparison.OrdinalIgnoreCase) > -1;
		}

		// Token: 0x060005B8 RID: 1464 RVA: 0x00033E21 File Offset: 0x00032021
		public static bool OICEquals(this string inStr, string toMatch)
		{
			return string.Equals(inStr, toMatch, StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060005B9 RID: 1465 RVA: 0x00033E2B File Offset: 0x0003202B
		public static bool OICStartsWith(this string inStr, string toMatch)
		{
			return inStr.StartsWith(toMatch, StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x060005BA RID: 1466 RVA: 0x00033E38 File Offset: 0x00032038
		public static bool OICStartsWithAny(this string inStr, params string[] toMatch)
		{
			for (int i = 0; i < toMatch.Length; i++)
			{
				if (inStr.StartsWith(toMatch[i], StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060005BB RID: 1467 RVA: 0x00033E64 File Offset: 0x00032064
		public static bool OICEndsWithAny(this string inStr, params string[] toMatch)
		{
			for (int i = 0; i < toMatch.Length; i++)
			{
				if (inStr.EndsWith(toMatch[i], StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060005BC RID: 1468 RVA: 0x00033E8E File Offset: 0x0003208E
		public static bool OICEndsWith(this string inStr, string toMatch)
		{
			return inStr.EndsWith(toMatch, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Returns the "Tail" of a string, after (but not including) the Last instance of specified delimiter.
		/// <seealso cref="!:TrimBefore(string, char)" />
		/// </summary>
		/// <param name="sString">The string to trim from.</param>
		/// <param name="chDelim">The delimiting character after which text should be returned.</param>
		/// <returns>Part of a string after (but not including) the final chDelim, or the full string if chDelim was not found.</returns>
		// Token: 0x060005BD RID: 1469 RVA: 0x00033E98 File Offset: 0x00032098
		public static string TrimBeforeLast(string sString, char chDelim)
		{
			if (sString == null)
			{
				return string.Empty;
			}
			int ixToken = sString.LastIndexOf(chDelim);
			if (ixToken < 0)
			{
				return sString;
			}
			return sString.Substring(ixToken + 1);
		}
	}
}
