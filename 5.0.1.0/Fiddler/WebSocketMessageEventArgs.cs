using System;

namespace Fiddler
{
	// Token: 0x0200003B RID: 59
	public class WebSocketMessageEventArgs : EventArgs
	{
		// Token: 0x17000071 RID: 113
		// (get) Token: 0x06000259 RID: 601 RVA: 0x00016495 File Offset: 0x00014695
		// (set) Token: 0x0600025A RID: 602 RVA: 0x0001649D File Offset: 0x0001469D
		public WebSocketMessage oWSM { get; private set; }

		// Token: 0x0600025B RID: 603 RVA: 0x000164A6 File Offset: 0x000146A6
		public WebSocketMessageEventArgs(WebSocketMessage _inMsg)
		{
			this.oWSM = _inMsg;
		}
	}
}
