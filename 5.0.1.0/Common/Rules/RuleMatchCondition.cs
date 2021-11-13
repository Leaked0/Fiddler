using System;

namespace FiddlerCore.Common.Rules
{
	/// <summary>
	/// The operator applied to the rule match conditions.
	/// </summary>
	// Token: 0x020000AE RID: 174
	public enum RuleMatchCondition
	{
		/// <summary>
		/// Match all conditions in order to execute the rule actions.
		/// </summary>
		// Token: 0x040002E9 RID: 745
		MatchAll = 1,
		/// <summary>
		/// Match at least one of the conditions in order to execute the rule actions.
		/// </summary>
		// Token: 0x040002EA RID: 746
		MatchAny,
		/// <summary>
		/// Execute the rule actions only if none of the conditions match.
		/// </summary>
		// Token: 0x040002EB RID: 747
		MatchNone
	}
}
