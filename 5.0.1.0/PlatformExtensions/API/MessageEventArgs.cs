using System;

namespace FiddlerCore.PlatformExtensions.API
{
	/// <summary>
	/// This class is used to pass a simple string message to a event handler.
	/// </summary>
	// Token: 0x020000A9 RID: 169
	internal class MessageEventArgs : EventArgs
	{
		/// <summary>
		/// Creates and initializes new instance of the <see cref="T:FiddlerCore.PlatformExtensions.API.MessageEventArgs" />. 
		/// </summary>
		/// <param name="message">The message.</param>
		// Token: 0x06000686 RID: 1670 RVA: 0x00036025 File Offset: 0x00034225
		public MessageEventArgs(string message)
		{
			this.Message = message;
		}

		/// <summary>
		/// Gets the message.
		/// </summary>
		// Token: 0x17000101 RID: 257
		// (get) Token: 0x06000687 RID: 1671 RVA: 0x00036034 File Offset: 0x00034234
		// (set) Token: 0x06000688 RID: 1672 RVA: 0x0003603C File Offset: 0x0003423C
		public string Message { get; private set; }
	}
}
