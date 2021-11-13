using System;
using System.Collections.Generic;
using System.Threading;

namespace Fiddler
{
	/// <summary>
	/// The ScheduledTasks class allows addition of jobs by name. It ensures that ONE instance of the named
	/// job will occur at *some* point in the future, between 0 and a specified max delay. If you queue multiple
	/// instances of the same-named Task, it's only done once.
	/// </summary>
	// Token: 0x0200004F RID: 79
	public static class ScheduledTasks
	{
		/// <summary>
		/// Under the lock, we enumerate the schedule to find work to do and remove that work from the schedule.
		/// After we release the lock, we then do the queued work.
		/// </summary>
		/// <param name="objState"></param>
		// Token: 0x06000307 RID: 775 RVA: 0x0001D2E4 File Offset: 0x0001B4E4
		private static void doWork(object objState)
		{
			List<KeyValuePair<string, ScheduledTasks.jobItem>> listWorkToDoNow = null;
			try
			{
				ScheduledTasks._RWLockDict.AcquireReaderLock(-1);
				ulong iNow = Utilities.GetTickCount();
				foreach (KeyValuePair<string, ScheduledTasks.jobItem> oDE in ScheduledTasks._dictSchedule)
				{
					if (iNow > oDE.Value._ulRunAfter)
					{
						oDE.Value._ulRunAfter = ulong.MaxValue;
						if (listWorkToDoNow == null)
						{
							listWorkToDoNow = new List<KeyValuePair<string, ScheduledTasks.jobItem>>();
						}
						listWorkToDoNow.Add(oDE);
					}
				}
				if (listWorkToDoNow == null)
				{
					return;
				}
				LockCookie oLC = ScheduledTasks._RWLockDict.UpgradeToWriterLock(-1);
				try
				{
					foreach (KeyValuePair<string, ScheduledTasks.jobItem> oItem in listWorkToDoNow)
					{
						ScheduledTasks._dictSchedule.Remove(oItem.Key);
					}
					if (ScheduledTasks._dictSchedule.Count < 1 && ScheduledTasks._timerInternal != null)
					{
						ScheduledTasks._timerInternal.Dispose();
						ScheduledTasks._timerInternal = null;
					}
				}
				finally
				{
					ScheduledTasks._RWLockDict.DowngradeFromWriterLock(ref oLC);
				}
			}
			finally
			{
				ScheduledTasks._RWLockDict.ReleaseReaderLock();
			}
			foreach (KeyValuePair<string, ScheduledTasks.jobItem> oItem2 in listWorkToDoNow)
			{
				try
				{
					oItem2.Value._oJob();
				}
				catch (Exception eX)
				{
				}
			}
		}

		// Token: 0x06000308 RID: 776 RVA: 0x0001D480 File Offset: 0x0001B680
		public static bool CancelWork(string sTaskName)
		{
			bool result;
			try
			{
				ScheduledTasks._RWLockDict.AcquireWriterLock(-1);
				result = ScheduledTasks._dictSchedule.Remove(sTaskName);
			}
			finally
			{
				ScheduledTasks._RWLockDict.ReleaseWriterLock();
			}
			return result;
		}

		// Token: 0x06000309 RID: 777 RVA: 0x0001D4C4 File Offset: 0x0001B6C4
		public static bool ScheduleWork(string sTaskName, uint iMaxDelay, SimpleEventHandler workFunction)
		{
			try
			{
				ScheduledTasks._RWLockDict.AcquireReaderLock(-1);
				if (ScheduledTasks._dictSchedule.ContainsKey(sTaskName))
				{
					return false;
				}
			}
			finally
			{
				ScheduledTasks._RWLockDict.ReleaseReaderLock();
			}
			ScheduledTasks.jobItem oJob = new ScheduledTasks.jobItem(workFunction, iMaxDelay);
			try
			{
				ScheduledTasks._RWLockDict.AcquireWriterLock(-1);
				if (ScheduledTasks._dictSchedule.ContainsKey(sTaskName))
				{
					return false;
				}
				ScheduledTasks._dictSchedule.Add(sTaskName, oJob);
				if (ScheduledTasks._timerInternal == null)
				{
					ScheduledTasks._timerInternal = new Timer(new TimerCallback(ScheduledTasks.doWork), null, 15, 15);
				}
			}
			finally
			{
				ScheduledTasks._RWLockDict.ReleaseWriterLock();
			}
			return true;
		}

		// Token: 0x04000166 RID: 358
		private const int CONST_MIN_RESOLUTION = 15;

		// Token: 0x04000167 RID: 359
		private static Dictionary<string, ScheduledTasks.jobItem> _dictSchedule = new Dictionary<string, ScheduledTasks.jobItem>();

		// Token: 0x04000168 RID: 360
		private static Timer _timerInternal = null;

		// Token: 0x04000169 RID: 361
		private static ReaderWriterLock _RWLockDict = new ReaderWriterLock();

		/// <summary>
		/// A jobItem represents a Function+Time tuple. The function will run after the given time.
		/// </summary>
		// Token: 0x020000CF RID: 207
		private class jobItem
		{
			// Token: 0x06000719 RID: 1817 RVA: 0x00038E0E File Offset: 0x0003700E
			internal jobItem(SimpleEventHandler oJob, uint iMaxDelay)
			{
				this._ulRunAfter = (ulong)iMaxDelay + Utilities.GetTickCount();
				this._oJob = oJob;
			}

			/// <summary>
			/// TickCount at which this job must run.
			/// </summary>
			// Token: 0x04000368 RID: 872
			internal ulong _ulRunAfter;

			/// <summary>
			/// Method to invoke to complete the job
			/// </summary>
			// Token: 0x04000369 RID: 873
			internal SimpleEventHandler _oJob;
		}
	}
}
