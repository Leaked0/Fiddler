using System;

namespace Fiddler
{
	// Token: 0x02000069 RID: 105
	public interface ISAZReader2 : ISAZReader
	{
		// Token: 0x170000DC RID: 220
		// (get) Token: 0x060004C4 RID: 1220
		// (set) Token: 0x060004C5 RID: 1221
		GetPasswordDelegate PasswordCallback { get; set; }
	}
}
