using System;
using System.Collections.Generic;
using System.Text;

namespace Fiddler
{
	/// <summary>
	/// The PipePool maintains a collection of connected ServerPipes for reuse
	/// </summary>
	// Token: 0x02000052 RID: 82
	internal class PipePool
	{
		// Token: 0x0600032E RID: 814 RVA: 0x0001E018 File Offset: 0x0001C218
		internal PipePool()
		{
			PipePool.MSEC_PIPE_POOLED_LIFETIME = (uint)FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.serverpipe.reuse", 115000);
			this.thePool = new Dictionary<string, Stack<ServerPipe>>();
			FiddlerApplication.Janitor.assignWork(new SimpleEventHandler(this.ScavengeCache), PipePool.MSEC_POOL_CLEANUP_INTERVAL);
		}

		/// <summary>
		/// Remove any pipes from Stacks if they exceed the age threshold
		/// Remove any Stacks from pool if they are empty
		/// </summary>
		// Token: 0x0600032F RID: 815 RVA: 0x0001E06C File Offset: 0x0001C26C
		internal void ScavengeCache()
		{
			if (this.thePool.Count < 1)
			{
				return;
			}
			List<ServerPipe> pipesToClose = new List<ServerPipe>();
			Dictionary<string, Stack<ServerPipe>> obj = this.thePool;
			lock (obj)
			{
				List<string> poolExpiredStacks = new List<string>();
				ulong tcExpireBefore = Utilities.GetTickCount() - (ulong)PipePool.MSEC_PIPE_POOLED_LIFETIME;
				foreach (KeyValuePair<string, Stack<ServerPipe>> oKVP in this.thePool)
				{
					Stack<ServerPipe> stPipes = oKVP.Value;
					Stack<ServerPipe> obj2 = stPipes;
					lock (obj2)
					{
						if (stPipes.Count > 0)
						{
							ServerPipe oPipe = stPipes.Peek();
							if (oPipe.ulLastPooled < tcExpireBefore)
							{
								pipesToClose.AddRange(stPipes);
								stPipes.Clear();
							}
							else if (stPipes.Count > 1)
							{
								ServerPipe[] oPipesInStack = stPipes.ToArray();
								if (oPipesInStack[oPipesInStack.Length - 1].ulLastPooled < tcExpireBefore)
								{
									stPipes.Clear();
									for (int iX = oPipesInStack.Length - 1; iX >= 0; iX--)
									{
										if (oPipesInStack[iX].ulLastPooled < tcExpireBefore)
										{
											pipesToClose.Add(oPipesInStack[iX]);
										}
										else
										{
											stPipes.Push(oPipesInStack[iX]);
										}
									}
								}
							}
						}
						if (stPipes.Count == 0)
						{
							poolExpiredStacks.Add(oKVP.Key);
						}
					}
				}
				foreach (string sKey in poolExpiredStacks)
				{
					this.thePool.Remove(sKey);
				}
			}
			foreach (BasePipe oPipe2 in pipesToClose)
			{
				oPipe2.End();
			}
		}

		/// <summary>
		/// Clear all pooled Pipes, calling .End() on each.
		/// </summary>
		// Token: 0x06000330 RID: 816 RVA: 0x0001E2B4 File Offset: 0x0001C4B4
		internal void Clear()
		{
			this.lngLastPoolPurge = DateTime.Now.Ticks;
			if (this.thePool.Count < 1)
			{
				return;
			}
			List<ServerPipe> pipesToClose = new List<ServerPipe>();
			Dictionary<string, Stack<ServerPipe>> obj = this.thePool;
			lock (obj)
			{
				foreach (KeyValuePair<string, Stack<ServerPipe>> oKVP in this.thePool)
				{
					Stack<ServerPipe> value = oKVP.Value;
					lock (value)
					{
						pipesToClose.AddRange(oKVP.Value);
					}
				}
				this.thePool.Clear();
			}
			foreach (ServerPipe oPipe in pipesToClose)
			{
				oPipe.End();
			}
		}

		/// <summary>
		/// Return a string representing the Pipes in the Pool
		/// </summary>
		/// <returns>A string representing the pipes in the pool</returns>
		// Token: 0x06000331 RID: 817 RVA: 0x0001E3DC File Offset: 0x0001C5DC
		internal string InspectPool()
		{
			StringBuilder sbResult = new StringBuilder(8192);
			sbResult.AppendFormat("ServerPipePool\nfiddler.network.timeouts.serverpipe.reuse: {0}ms\nContents\n--------\n", PipePool.MSEC_PIPE_POOLED_LIFETIME);
			Dictionary<string, Stack<ServerPipe>> obj = this.thePool;
			lock (obj)
			{
				foreach (string sPoolKey in this.thePool.Keys)
				{
					Stack<ServerPipe> oStack = this.thePool[sPoolKey];
					sbResult.AppendFormat("\t[{0}] entries for '{1}'.\n", oStack.Count, sPoolKey);
					Stack<ServerPipe> obj2 = oStack;
					lock (obj2)
					{
						foreach (ServerPipe oPipe in oStack)
						{
							sbResult.AppendFormat("\t\t{0}\n", oPipe.ToString());
						}
					}
				}
			}
			sbResult.Append("\n--------\n");
			return sbResult.ToString();
		}

		/// <summary>
		/// Get a Server connection for reuse, or null if a suitable connection is not in the pool.
		/// </summary>
		/// <param name="sPoolKey">The key which identifies the connection to search for.</param>
		/// <param name="iPID">The ProcessID of the client requesting the Pipe</param>
		/// <param name="HackiForSession">HACK to be removed; the SessionID# of the request for logging</param>
		/// <returns>A Pipe to reuse, or NULL</returns>
		// Token: 0x06000332 RID: 818 RVA: 0x0001E52C File Offset: 0x0001C72C
		internal ServerPipe TakePipe(string sPoolKey, int iPID, int HackiForSession)
		{
			if (!CONFIG.ReuseServerSockets)
			{
				return null;
			}
			Dictionary<string, Stack<ServerPipe>> obj = this.thePool;
			Stack<ServerPipe> oStack;
			lock (obj)
			{
				if ((iPID == 0 || !this.thePool.TryGetValue(string.Format("pid{0}*{1}", iPID, sPoolKey), out oStack) || oStack.Count < 1) && (!this.thePool.TryGetValue(sPoolKey, out oStack) || oStack.Count < 1))
				{
					return null;
				}
			}
			Stack<ServerPipe> obj2 = oStack;
			ServerPipe oResult;
			lock (obj2)
			{
				try
				{
					if (oStack.Count == 0)
					{
						return null;
					}
					oResult = oStack.Pop();
				}
				catch (Exception eX)
				{
					FiddlerApplication.Log.LogString(eX.ToString());
					return null;
				}
			}
			return oResult;
		}

		/// <summary>
		/// Store a pipe for later use, if reuse is allowed by settings and state of the pipe.
		/// </summary>
		/// <param name="oPipe">The Pipe to place in the pool</param>
		// Token: 0x06000333 RID: 819 RVA: 0x0001E620 File Offset: 0x0001C820
		internal void PoolOrClosePipe(ServerPipe oPipe)
		{
			if (!CONFIG.ReuseServerSockets)
			{
				oPipe.End();
				return;
			}
			if (oPipe.ReusePolicy == PipeReusePolicy.NoReuse || oPipe.ReusePolicy == PipeReusePolicy.MarriedToClientPipe)
			{
				oPipe.End();
				return;
			}
			if (this.lngLastPoolPurge > oPipe.dtConnected.Ticks)
			{
				oPipe.End();
				return;
			}
			if (oPipe.sPoolKey == null || oPipe.sPoolKey.Length < 2)
			{
				oPipe.End();
				return;
			}
			oPipe.ulLastPooled = Utilities.GetTickCount();
			Dictionary<string, Stack<ServerPipe>> obj = this.thePool;
			Stack<ServerPipe> oStack;
			lock (obj)
			{
				if (!this.thePool.TryGetValue(oPipe.sPoolKey, out oStack))
				{
					oStack = new Stack<ServerPipe>();
					this.thePool.Add(oPipe.sPoolKey, oStack);
				}
			}
			Stack<ServerPipe> obj2 = oStack;
			lock (obj2)
			{
				oStack.Push(oPipe);
			}
		}

		/// <summary>
		/// Minimum idle time of pipes to be expired from the pool.
		/// Note, we don't check the pipe's ulLastPooled value when extracting a pipe, 
		/// so its age could exceed the allowed lifetime by up to MSEC_POOL_CLEANUP_INTERVAL
		/// WARNING: Don't change the timeout &gt;2 minutes casually. Server bugs apparently exist: https://bugzilla.mozilla.org/show_bug.cgi?id=491541
		/// </summary>
		// Token: 0x04000179 RID: 377
		internal static uint MSEC_PIPE_POOLED_LIFETIME = 115000U;

		// Token: 0x0400017A RID: 378
		internal static uint MSEC_POOL_CLEANUP_INTERVAL = 30000U;

		/// <summary>
		/// The Pool itself.
		/// </summary>
		// Token: 0x0400017B RID: 379
		private readonly Dictionary<string, Stack<ServerPipe>> thePool;

		/// <summary>
		/// Time at which a "Clear before" operation was conducted. We store this
		/// so that we don't accidentally put any pipes that were in use back into
		/// the pool after a clear operation
		/// </summary>
		// Token: 0x0400017C RID: 380
		private long lngLastPoolPurge;
	}
}
