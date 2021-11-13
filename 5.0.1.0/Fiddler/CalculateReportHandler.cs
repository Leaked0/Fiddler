using System;

namespace Fiddler
{
	/// <summary>
	/// An event handling delegate which is called during report calculation with the set of sessions being evaluated.
	/// </summary>
	/// <param name="_arrSessions">The sessions in this report.</param>
	// Token: 0x02000033 RID: 51
	// (Invoke) Token: 0x060001F2 RID: 498
	public delegate void CalculateReportHandler(Session[] _arrSessions);
}
