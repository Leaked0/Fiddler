using System;

namespace Fiddler
{
	/// <summary>
	/// Event arguments constructed for the OnStateChanged event raised when a Session's state property changed
	/// </summary>
	// Token: 0x0200005F RID: 95
	public class StateChangeEventArgs : EventArgs
	{
		/// <summary>
		/// Constructor for the change in state
		/// </summary>
		/// <param name="ssOld">The old state</param>
		/// <param name="ssNew">The new state</param>
		// Token: 0x0600049B RID: 1179 RVA: 0x0002D9AB File Offset: 0x0002BBAB
		internal StateChangeEventArgs(SessionStates ssOld, SessionStates ssNew)
		{
			this.oldState = ssOld;
			this.newState = ssNew;
		}

		/// <summary>
		/// The prior state of this session
		/// </summary>
		// Token: 0x040001F4 RID: 500
		public readonly SessionStates oldState;

		/// <summary>
		/// The new state of this session
		/// </summary>
		// Token: 0x040001F5 RID: 501
		public readonly SessionStates newState;
	}
}
