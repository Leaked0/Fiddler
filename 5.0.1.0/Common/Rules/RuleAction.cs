using System;

namespace FiddlerCore.Common.Rules
{
	// Token: 0x020000AB RID: 171
	public class RuleAction
	{
		/// <summary>
		/// The action type 
		/// </summary>
		// Token: 0x17000102 RID: 258
		// (get) Token: 0x06000694 RID: 1684 RVA: 0x00036DAF File Offset: 0x00034FAF
		// (set) Token: 0x06000695 RID: 1685 RVA: 0x00036DB7 File Offset: 0x00034FB7
		public RuleActionType Type { get; set; }

		/// <summary>
		/// The the condition of the action (set, replace)
		/// </summary>
		// Token: 0x17000103 RID: 259
		// (get) Token: 0x06000696 RID: 1686 RVA: 0x00036DC0 File Offset: 0x00034FC0
		// (set) Token: 0x06000697 RID: 1687 RVA: 0x00036DC8 File Offset: 0x00034FC8
		public string Condition { get; set; }

		/// <summary>
		/// The key to match (depending on the action)
		/// </summary>
		// Token: 0x17000104 RID: 260
		// (get) Token: 0x06000698 RID: 1688 RVA: 0x00036DD1 File Offset: 0x00034FD1
		// (set) Token: 0x06000699 RID: 1689 RVA: 0x00036DD9 File Offset: 0x00034FD9
		public string Key { get; set; }

		/// <summary>
		/// The text to search for in the value that the action is updating.
		/// </summary>
		// Token: 0x17000105 RID: 261
		// (get) Token: 0x0600069A RID: 1690 RVA: 0x00036DE2 File Offset: 0x00034FE2
		// (set) Token: 0x0600069B RID: 1691 RVA: 0x00036DEA File Offset: 0x00034FEA
		public string Find { get; set; }

		/// <summary>
		/// The value to match (depending on the action)
		/// </summary>
		// Token: 0x17000106 RID: 262
		// (get) Token: 0x0600069C RID: 1692 RVA: 0x00036DF3 File Offset: 0x00034FF3
		// (set) Token: 0x0600069D RID: 1693 RVA: 0x00036DFB File Offset: 0x00034FFB
		public string Value { get; set; }
	}
}
