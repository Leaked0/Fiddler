using System;

namespace FiddlerCore.Common.Rules
{
	/// <summary>
	/// The different types of match conditions supported by the Fiddler rules.
	/// </summary>
	// Token: 0x020000B0 RID: 176
	public enum RuleMatchType
	{
		/// <summary>
		/// Match the protocol of a URL (http, https)
		/// </summary>
		// Token: 0x040002FF RID: 767
		Protocol = 1,
		/// <summary>
		/// Match the host part of a URL
		/// </summary>
		// Token: 0x04000300 RID: 768
		Host,
		/// <summary>
		/// Match the path part of a URL
		/// </summary>
		// Token: 0x04000301 RID: 769
		Path,
		/// <summary>
		/// Match in the whole URL
		/// </summary>
		// Token: 0x04000302 RID: 770
		Url,
		/// <summary>
		/// Match a response status
		/// </summary>
		// Token: 0x04000303 RID: 771
		Status,
		/// <summary>
		/// Match a HTTP method
		/// </summary>
		// Token: 0x04000304 RID: 772
		Method,
		/// <summary>
		/// Match the initiating process ID
		/// </summary>
		// Token: 0x04000305 RID: 773
		Process,
		/// <summary>
		/// Match the client IP address
		/// </summary>
		// Token: 0x04000306 RID: 774
		ClientIP,
		/// <summary>
		/// Match the server IP address
		/// </summary>
		// Token: 0x04000307 RID: 775
		RemoteIP,
		/// <summary>
		/// Match the response body size
		/// </summary>
		// Token: 0x04000308 RID: 776
		BodySize,
		/// <summary>
		/// Match the request duration in milliseconds
		/// </summary>
		// Token: 0x04000309 RID: 777
		Duration,
		/// <summary>
		/// Match the fiddler session comment
		/// </summary>
		// Token: 0x0400030A RID: 778
		Comment,
		/// <summary>
		/// Match a part of the request body
		/// </summary>
		// Token: 0x0400030B RID: 779
		RequestBody,
		/// <summary>
		/// Match a request header name or value
		/// </summary>
		// Token: 0x0400030C RID: 780
		RequestHeader,
		/// <summary>
		/// Match a part of the response body
		/// </summary>
		// Token: 0x0400030D RID: 781
		ResponseBody,
		/// <summary>
		/// Match a response header name or value
		/// </summary>
		// Token: 0x0400030E RID: 782
		ResponseHeader,
		/// <summary>
		/// Match a session timer
		/// </summary>
		// Token: 0x0400030F RID: 783
		Timer,
		/// <summary>
		/// Match a request cookie
		/// </summary>
		// Token: 0x04000310 RID: 784
		Cookie,
		/// <summary>
		/// Magic string - old auto responder match condition
		/// </summary>
		// Token: 0x04000311 RID: 785
		MagicString
	}
}
