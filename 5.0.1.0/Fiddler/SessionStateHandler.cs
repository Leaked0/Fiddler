using System;

namespace Fiddler
{
	/// <summary>
	/// An event handling delegate which is called as a part of the HTTP pipeline at various stages.
	/// </summary>
	/// <param name="oSession">The Web Session in the pipeline.</param>
	// Token: 0x02000034 RID: 52
	// (Invoke) Token: 0x060001F6 RID: 502
	public delegate void SessionStateHandler(Session oSession);
}
