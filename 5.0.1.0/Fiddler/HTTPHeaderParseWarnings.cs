using System;

namespace Fiddler
{
	/// <summary>
	/// Flags that indicate what problems, if any, were encountered in parsing HTTP headers
	/// </summary>
	// Token: 0x0200004A RID: 74
	[Flags]
	public enum HTTPHeaderParseWarnings
	{
		/// <summary>
		/// There were no problems parsing the HTTP headers
		/// </summary>
		// Token: 0x0400014B RID: 331
		None = 0,
		/// <summary>
		/// The HTTP headers ended incorrectly with \n\n
		/// </summary>
		// Token: 0x0400014C RID: 332
		EndedWithLFLF = 1,
		/// <summary>
		/// The HTTP headers ended incorrectly with \n\r\n
		/// </summary>
		// Token: 0x0400014D RID: 333
		EndedWithLFCRLF = 2,
		/// <summary>
		/// The HTTP headers were malformed.
		/// </summary>
		// Token: 0x0400014E RID: 334
		Malformed = 4
	}
}
