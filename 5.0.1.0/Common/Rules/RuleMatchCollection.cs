using System;

namespace FiddlerCore.Common.Rules
{
	/// <summary>
	/// The match object that comes from the Fiddler client (if using the new rule format with multiple matches/actions)
	/// </summary>
	// Token: 0x020000AD RID: 173
	public class RuleMatchCollection
	{
		/// <summary>
		/// The operator (all,any,none) for the rule match conditions
		/// </summary>
		// Token: 0x1700010B RID: 267
		// (get) Token: 0x060006A8 RID: 1704 RVA: 0x00036E58 File Offset: 0x00035058
		// (set) Token: 0x060006A9 RID: 1705 RVA: 0x00036E60 File Offset: 0x00035060
		public RuleMatchCondition MatchCondition { get; set; }

		/// <summary>
		/// A list of match conditions
		/// </summary>
		// Token: 0x1700010C RID: 268
		// (get) Token: 0x060006AA RID: 1706 RVA: 0x00036E69 File Offset: 0x00035069
		// (set) Token: 0x060006AB RID: 1707 RVA: 0x00036E71 File Offset: 0x00035071
		public string[] Matches { get; set; }
	}
}
