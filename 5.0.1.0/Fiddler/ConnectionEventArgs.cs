using System;
using System.Net.Sockets;

namespace Fiddler
{
	// Token: 0x02000037 RID: 55
	public class ConnectionEventArgs : EventArgs
	{
		/// <summary>
		/// The Socket which was just Connected or Accepted
		/// </summary>
		// Token: 0x17000066 RID: 102
		// (get) Token: 0x06000248 RID: 584 RVA: 0x000163C4 File Offset: 0x000145C4
		public Socket Connection
		{
			get
			{
				return this._oSocket;
			}
		}

		/// <summary>
		/// The Session which owns the this new connection
		/// </summary>
		// Token: 0x17000067 RID: 103
		// (get) Token: 0x06000249 RID: 585 RVA: 0x000163CC File Offset: 0x000145CC
		public Session OwnerSession
		{
			get
			{
				return this._oSession;
			}
		}

		// Token: 0x0600024A RID: 586 RVA: 0x000163D4 File Offset: 0x000145D4
		internal ConnectionEventArgs(Session oSession, Socket oSocket)
		{
			this._oSession = oSession;
			this._oSocket = oSocket;
		}

		// Token: 0x040000FD RID: 253
		private readonly Socket _oSocket;

		// Token: 0x040000FE RID: 254
		private readonly Session _oSession;
	}
}
