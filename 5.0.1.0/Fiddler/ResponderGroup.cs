using System;

namespace Fiddler
{
	// Token: 0x02000008 RID: 8
	public class ResponderGroup
	{
		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000074 RID: 116 RVA: 0x00003F37 File Offset: 0x00002137
		// (set) Token: 0x06000075 RID: 117 RVA: 0x00003F3F File Offset: 0x0000213F
		public string Id { get; set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000076 RID: 118 RVA: 0x00003F48 File Offset: 0x00002148
		// (set) Token: 0x06000077 RID: 119 RVA: 0x00003F50 File Offset: 0x00002150
		public string Header { get; set; }

		// Token: 0x06000078 RID: 120 RVA: 0x00003F59 File Offset: 0x00002159
		public void AddRule(ResponderRule rule)
		{
			if (rule.Group != this)
			{
				if (rule.Group != null)
				{
					rule.Group.RemoveRule(rule);
				}
				rule.Group = this;
			}
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00003F7F File Offset: 0x0000217F
		internal void RemoveRule(ResponderRule rule)
		{
			rule.Group = null;
		}
	}
}
