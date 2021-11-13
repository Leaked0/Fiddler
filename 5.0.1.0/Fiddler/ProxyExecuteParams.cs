using System;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Fiddler
{
	/// <summary>
	/// Parameters passed into the AcceptConnection method.
	/// </summary>
	// Token: 0x0200005C RID: 92
	internal class ProxyExecuteParams
	{
		// Token: 0x06000496 RID: 1174 RVA: 0x0002D955 File Offset: 0x0002BB55
		public ProxyExecuteParams(Socket oS, X509Certificate2 oC)
		{
			this.dtConnectionAccepted = DateTime.Now;
			this.oSocket = oS;
			this.oServerCert = oC;
		}

		/// <summary>
		/// The Socket which represents the newly-accepted Connection
		/// </summary>
		// Token: 0x040001E9 RID: 489
		public Socket oSocket;

		/// <summary>
		/// The Certificate to pass to SecureClientPipeDirect immediately after accepting the connection.
		/// Normally null, this will be set if the proxy endpoint is configured as a "Secure" endpoint
		/// by AssignEndpointCertificate / ActAsHTTPSEndpointForHostname.
		/// </summary>
		// Token: 0x040001EA RID: 490
		public X509Certificate2 oServerCert;

		/// <summary>
		/// The DateTime of Creation of this connection
		/// </summary>
		// Token: 0x040001EB RID: 491
		public DateTime dtConnectionAccepted;
	}
}
