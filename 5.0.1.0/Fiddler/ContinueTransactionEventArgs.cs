using System;

namespace Fiddler
{
	// Token: 0x0200005E RID: 94
	public class ContinueTransactionEventArgs : EventArgs
	{
		// Token: 0x170000CD RID: 205
		// (get) Token: 0x06000497 RID: 1175 RVA: 0x0002D976 File Offset: 0x0002BB76
		public ContinueTransactionReason reason
		{
			get
			{
				return this._reason;
			}
		}

		// Token: 0x170000CE RID: 206
		// (get) Token: 0x06000498 RID: 1176 RVA: 0x0002D97E File Offset: 0x0002BB7E
		public Session originalSession
		{
			get
			{
				return this._sessOriginal;
			}
		}

		// Token: 0x170000CF RID: 207
		// (get) Token: 0x06000499 RID: 1177 RVA: 0x0002D986 File Offset: 0x0002BB86
		public Session newSession
		{
			get
			{
				return this._sessNew;
			}
		}

		// Token: 0x0600049A RID: 1178 RVA: 0x0002D98E File Offset: 0x0002BB8E
		internal ContinueTransactionEventArgs(Session originalSession, Session newSession, ContinueTransactionReason reason)
		{
			this._sessOriginal = originalSession;
			this._sessNew = newSession;
			this._reason = reason;
		}

		// Token: 0x040001F1 RID: 497
		private Session _sessOriginal;

		// Token: 0x040001F2 RID: 498
		private Session _sessNew;

		// Token: 0x040001F3 RID: 499
		private ContinueTransactionReason _reason;
	}
}
