using System;

namespace Fiddler
{
	/// <summary>
	/// This enumeration provides the values for the WebSocketMessage object's BitFlags field
	/// </summary>
	// Token: 0x0200006F RID: 111
	[Flags]
	public enum WSMFlags
	{
		/// <summary>
		/// No flags are set
		/// </summary>
		// Token: 0x0400026C RID: 620
		None = 0,
		/// <summary>
		/// Message was eaten ("dropped") by Fiddler
		/// </summary>
		// Token: 0x0400026D RID: 621
		Aborted = 1,
		/// <summary>
		/// Message was generated ("injected") by Fiddler itself
		/// </summary>
		// Token: 0x0400026E RID: 622
		GeneratedByFiddler = 2,
		/// <summary>
		/// Fragmented Message was reassembled by Fiddler
		/// </summary>
		// Token: 0x0400026F RID: 623
		Assembled = 4,
		/// <summary>
		/// Breakpointed
		/// </summary>
		// Token: 0x04000270 RID: 624
		Breakpointed = 8
	}
}
