using System;

namespace Fiddler
{
	/// <summary>
	/// Interface for the WebSocket and CONNECT Tunnel classes
	/// </summary>
	// Token: 0x0200001B RID: 27
	public interface ITunnel
	{
		// Token: 0x1700004A RID: 74
		// (get) Token: 0x06000155 RID: 341
		long IngressByteCount { get; }

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000156 RID: 342
		long EgressByteCount { get; }

		// Token: 0x06000157 RID: 343
		void CloseTunnel();

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000158 RID: 344
		bool IsOpen { get; }
	}
}
