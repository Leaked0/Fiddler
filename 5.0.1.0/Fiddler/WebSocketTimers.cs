using System;
using System.Text;

namespace Fiddler
{
	/// <summary>
	/// Timers
	/// </summary>
	// Token: 0x02000071 RID: 113
	public class WebSocketTimers
	{
		/// <summary>
		/// Return the timers formatted to be placed in pseudo-headers used in saving the WebSocketMessage to a stream (SAZ).
		/// NOTE: TRAILING \r\n is critical.
		/// </summary>
		/// <returns></returns>
		// Token: 0x060005A1 RID: 1441 RVA: 0x000335CC File Offset: 0x000317CC
		internal string ToHeaderString()
		{
			StringBuilder sbResult = new StringBuilder();
			if (this.dtDoneRead.Ticks > 0L)
			{
				sbResult.AppendFormat("DoneRead: {0}\r\n", this.dtDoneRead.ToString("o"));
			}
			if (this.dtBeginSend.Ticks > 0L)
			{
				sbResult.AppendFormat("BeginSend: {0}\r\n", this.dtBeginSend.ToString("o"));
			}
			if (this.dtDoneSend.Ticks > 0L)
			{
				sbResult.AppendFormat("DoneSend: {0}\r\n", this.dtDoneSend.ToString("o"));
			}
			if (sbResult.Length < 2)
			{
				sbResult.Append("\r\n");
			}
			return sbResult.ToString();
		}

		// Token: 0x060005A2 RID: 1442 RVA: 0x0003367B File Offset: 0x0003187B
		public override string ToString()
		{
			return this.ToString(false);
		}

		// Token: 0x060005A3 RID: 1443 RVA: 0x00033684 File Offset: 0x00031884
		public string ToString(bool bMultiLine)
		{
			if (bMultiLine)
			{
				return string.Format("DoneRead:\t{0:HH:mm:ss.fff}\r\nBeginSend:\t{1:HH:mm:ss.fff}\r\nDoneSend:\t{2:HH:mm:ss.fff}\r\n{3}", new object[]
				{
					this.dtDoneRead,
					this.dtBeginSend,
					this.dtDoneSend,
					(TimeSpan.Zero < this.dtDoneSend - this.dtDoneRead) ? string.Format("\r\n\tOverall Elapsed:\t{0:h\\:mm\\:ss\\.fff}\r\n", this.dtDoneSend - this.dtDoneRead) : string.Empty
				});
			}
			return string.Format("DoneRead: {0:HH:mm:ss.fff}, BeginSend: {1:HH:mm:ss.fff}, DoneSend: {2:HH:mm:ss.fff}{3}", new object[]
			{
				this.dtDoneRead,
				this.dtBeginSend,
				this.dtDoneSend,
				(TimeSpan.Zero < this.dtDoneSend - this.dtDoneRead) ? string.Format(",Overall Elapsed: {0:h\\:mm\\:ss\\.fff}", this.dtDoneSend - this.dtDoneRead) : string.Empty
			});
		}

		/// <summary>
		/// When was this message read from the sender
		/// </summary>
		// Token: 0x0400027B RID: 635
		public DateTime dtDoneRead;

		/// <summary>
		/// When did transmission of this message to the recipient begin
		/// </summary>
		// Token: 0x0400027C RID: 636
		public DateTime dtBeginSend;

		/// <summary>
		/// When did transmission of this message to the recipient end
		/// </summary>
		// Token: 0x0400027D RID: 637
		public DateTime dtDoneSend;
	}
}
