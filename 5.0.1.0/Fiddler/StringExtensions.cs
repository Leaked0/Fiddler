using System;
using FiddlerCore.Utilities;

namespace Fiddler
{
	/// <summary>
	/// Common functions we'll want to use on Strings. Fiddler makes extensive use of strings which 
	/// should be interpreted in a case-insensitive manner.
	///
	/// WARNING: Methods assume that the calling object is not null, which is lame for reliability but arguably good for performance.
	/// </summary>
	// Token: 0x0200006C RID: 108
	public static class StringExtensions
	{
		// Token: 0x06000564 RID: 1380 RVA: 0x00031FDB File Offset: 0x000301DB
		public static bool OICContains(this string inStr, string toMatch)
		{
			return inStr.OICContains(toMatch);
		}

		// Token: 0x06000565 RID: 1381 RVA: 0x00031FE4 File Offset: 0x000301E4
		public static bool OICEquals(this string inStr, string toMatch)
		{
			return inStr.OICEquals(toMatch);
		}

		// Token: 0x06000566 RID: 1382 RVA: 0x00031FED File Offset: 0x000301ED
		public static bool OICStartsWith(this string inStr, string toMatch)
		{
			return inStr.OICStartsWith(toMatch);
		}

		// Token: 0x06000567 RID: 1383 RVA: 0x00031FF6 File Offset: 0x000301F6
		public static bool OICStartsWithAny(this string inStr, params string[] toMatch)
		{
			return inStr.OICStartsWithAny(toMatch);
		}

		// Token: 0x06000568 RID: 1384 RVA: 0x00031FFF File Offset: 0x000301FF
		public static bool OICEndsWithAny(this string inStr, params string[] toMatch)
		{
			return inStr.OICEndsWithAny(toMatch);
		}

		// Token: 0x06000569 RID: 1385 RVA: 0x00032008 File Offset: 0x00030208
		public static bool OICEndsWith(this string inStr, string toMatch)
		{
			return inStr.OICEndsWith(toMatch);
		}

		// Token: 0x0600056A RID: 1386 RVA: 0x00032011 File Offset: 0x00030211
		internal static int StrLen(this string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return 0;
			}
			return s.Length;
		}
	}
}
