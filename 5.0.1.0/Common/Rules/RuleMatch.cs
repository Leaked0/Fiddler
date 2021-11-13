using System;

namespace FiddlerCore.Common.Rules
{
	// Token: 0x020000AC RID: 172
	public class RuleMatch
	{
		/// <summary>
		/// The match condition type 
		/// </summary>
		// Token: 0x17000107 RID: 263
		// (get) Token: 0x0600069F RID: 1695 RVA: 0x00036E0C File Offset: 0x0003500C
		// (set) Token: 0x060006A0 RID: 1696 RVA: 0x00036E14 File Offset: 0x00035014
		public RuleMatchType Type { get; set; }

		/// <summary>
		/// Used to denote header/cookie name
		/// </summary>
		// Token: 0x17000108 RID: 264
		// (get) Token: 0x060006A1 RID: 1697 RVA: 0x00036E1D File Offset: 0x0003501D
		// (set) Token: 0x060006A2 RID: 1698 RVA: 0x00036E25 File Offset: 0x00035025
		public string Key { get; set; }

		/// <summary>
		/// Used for the compare condition (equal, contains, regular expression, etc.)
		/// </summary>
		// Token: 0x17000109 RID: 265
		// (get) Token: 0x060006A3 RID: 1699 RVA: 0x00036E2E File Offset: 0x0003502E
		// (set) Token: 0x060006A4 RID: 1700 RVA: 0x00036E36 File Offset: 0x00035036
		public string Condition { get; set; }

		/// <summary>
		/// The value to match (depending on the condition)
		/// </summary>
		// Token: 0x1700010A RID: 266
		// (get) Token: 0x060006A5 RID: 1701 RVA: 0x00036E3F File Offset: 0x0003503F
		// (set) Token: 0x060006A6 RID: 1702 RVA: 0x00036E47 File Offset: 0x00035047
		public string Value { get; set; }
	}
}
