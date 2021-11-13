using System;

namespace Fiddler
{
	/// <summary>
	/// The MockTunnel represents a CONNECT tunnel which was reloaded from a SAZ file.
	/// </summary>
	// Token: 0x02000049 RID: 73
	internal class MockTunnel : ITunnel
	{
		// Token: 0x060002EC RID: 748 RVA: 0x0001C527 File Offset: 0x0001A727
		public MockTunnel(long lngEgress, long lngIngress)
		{
			this._lngBytesEgress = lngEgress;
			this._lngBytesIngress = lngIngress;
		}

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x060002ED RID: 749 RVA: 0x0001C53D File Offset: 0x0001A73D
		public long IngressByteCount
		{
			get
			{
				return this._lngBytesIngress;
			}
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x060002EE RID: 750 RVA: 0x0001C545 File Offset: 0x0001A745
		public long EgressByteCount
		{
			get
			{
				return this._lngBytesEgress;
			}
		}

		// Token: 0x060002EF RID: 751 RVA: 0x0001C54D File Offset: 0x0001A74D
		public void CloseTunnel()
		{
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x060002F0 RID: 752 RVA: 0x0001C54F File Offset: 0x0001A74F
		public bool IsOpen
		{
			get
			{
				return false;
			}
		}

		// Token: 0x04000148 RID: 328
		private long _lngBytesEgress;

		// Token: 0x04000149 RID: 329
		private long _lngBytesIngress;
	}
}
