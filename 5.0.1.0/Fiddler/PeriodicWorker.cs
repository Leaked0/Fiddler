using System;
using System.Collections.Generic;
using System.Threading;

namespace Fiddler
{
	/// <summary>
	/// Somewhat similar to the Framework's "BackgroundWorker" class, the periodic worker performs a similar function on a periodic schedule.
	/// NOTE: the callback occurs on a background thread.
	///
	/// The PeriodicWorker class is used by Fiddler to perform "cleanup" style tasks on a timer. Put work in the queue, 
	/// and it will see that it's done at least as often as the schedule specified until Fiddler begins to close at which
	/// point all work stops.
	///
	///
	/// The underlying timer's interval is 1 second.
	///
	/// </summary>
	/// <remarks>
	/// I think a significant part of the reason that this class exists is that I thought the System.Threading.Timer consumed one thread for each
	/// timer. In reality, per "CLR via C# 4e" all of the instances share one underlying thread and thus my concern was misplaced. Ah well.
	/// </remarks>
	// Token: 0x0200004E RID: 78
	internal class PeriodicWorker
	{
		// Token: 0x06000303 RID: 771 RVA: 0x0001D15F File Offset: 0x0001B35F
		internal PeriodicWorker()
		{
			this.timerInternal = new Timer(new TimerCallback(this.doWork), null, 500, 500);
		}

		// Token: 0x06000304 RID: 772 RVA: 0x0001D194 File Offset: 0x0001B394
		private void doWork(object objState)
		{
			if (FiddlerApplication.isClosing)
			{
				this.timerInternal.Dispose();
				return;
			}
			List<PeriodicWorker.taskItem> obj = this.oTaskList;
			PeriodicWorker.taskItem[] myTasks;
			lock (obj)
			{
				myTasks = new PeriodicWorker.taskItem[this.oTaskList.Count];
				this.oTaskList.CopyTo(myTasks);
			}
			foreach (PeriodicWorker.taskItem oTI in myTasks)
			{
				if (Utilities.GetTickCount() > oTI._ulLastRun + (ulong)oTI._iPeriod)
				{
					oTI._oTask();
					oTI._ulLastRun = Utilities.GetTickCount();
				}
			}
		}

		/// <summary>
		/// Assigns a "job" to the Periodic worker, on the schedule specified by iMS. 
		/// </summary>
		/// <param name="workFunction">The function to run on the timer specified.
		/// Warning: the function is NOT called on the UI thread, so use .Invoke() if needed.</param>
		/// <param name="iMS">The # of milliseconds to wait between runs</param>
		/// <returns>A taskItem which can be used to revokeWork later</returns>
		// Token: 0x06000305 RID: 773 RVA: 0x0001D248 File Offset: 0x0001B448
		internal PeriodicWorker.taskItem assignWork(SimpleEventHandler workFunction, uint iMS)
		{
			PeriodicWorker.taskItem oResult = new PeriodicWorker.taskItem(workFunction, iMS);
			List<PeriodicWorker.taskItem> obj = this.oTaskList;
			lock (obj)
			{
				this.oTaskList.Add(oResult);
			}
			return oResult;
		}

		/// <summary>
		/// Revokes a previously-assigned task from this worker.
		/// </summary>
		/// <param name="oToRevoke"></param>
		// Token: 0x06000306 RID: 774 RVA: 0x0001D298 File Offset: 0x0001B498
		internal void revokeWork(PeriodicWorker.taskItem oToRevoke)
		{
			if (oToRevoke == null)
			{
				return;
			}
			List<PeriodicWorker.taskItem> obj = this.oTaskList;
			lock (obj)
			{
				this.oTaskList.Remove(oToRevoke);
			}
		}

		// Token: 0x04000163 RID: 355
		private const int CONST_MIN_RESOLUTION = 500;

		// Token: 0x04000164 RID: 356
		private Timer timerInternal;

		// Token: 0x04000165 RID: 357
		private List<PeriodicWorker.taskItem> oTaskList = new List<PeriodicWorker.taskItem>();

		// Token: 0x020000CE RID: 206
		internal class taskItem
		{
			// Token: 0x06000718 RID: 1816 RVA: 0x00038DED File Offset: 0x00036FED
			public taskItem(SimpleEventHandler oTask, uint iPeriod)
			{
				this._ulLastRun = Utilities.GetTickCount();
				this._iPeriod = iPeriod;
				this._oTask = oTask;
			}

			// Token: 0x04000365 RID: 869
			public ulong _ulLastRun;

			// Token: 0x04000366 RID: 870
			public uint _iPeriod;

			// Token: 0x04000367 RID: 871
			public SimpleEventHandler _oTask;
		}
	}
}
