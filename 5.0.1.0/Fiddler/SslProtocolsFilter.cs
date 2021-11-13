using System;
using System.Security.Authentication;

namespace Fiddler
{
	// Token: 0x02000030 RID: 48
	internal class SslProtocolsFilter
	{
		/// <summary>
		/// Use this method to ensure that the passed protocols are consecutive. It is done by adding missing
		/// protocols from the sequence, thus filling the gaps, if any. Works only with Tls, Tls11 and Tls12.
		/// </summary>
		/// <example>
		/// Passed protocols: Tls, Tls12
		/// Return value: Tls, Tls11, Tls12
		/// </example>
		/// <param name="protocols">The input SSL protocols</param>
		/// <returns>Consecutive version of the input SSL protocols</returns>
		// Token: 0x060001E9 RID: 489 RVA: 0x0001510C File Offset: 0x0001330C
		internal static SslProtocols EnsureConsecutiveProtocols(SslProtocols protocols)
		{
			SslProtocols tls11 = SslProtocols.Tls11;
			SslProtocols tls12 = SslProtocols.Tls12;
			if (protocols.HasFlag(SslProtocols.Tls) && !protocols.HasFlag(tls11) && protocols.HasFlag(tls12))
			{
				return protocols | tls11;
			}
			return protocols;
		}
	}
}
