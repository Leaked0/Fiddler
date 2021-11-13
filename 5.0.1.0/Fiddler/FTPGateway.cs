using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;

namespace Fiddler
{
	/// <summary>
	/// This FTP Gateway class is used if Fiddler is configured as the FTP proxy and there's no upstream gateway configured. 
	/// Fiddler must act as a HTTP-&gt;FTP protocol converter, which it does by using the .NET FTP classes.
	/// </summary>
	// Token: 0x0200003D RID: 61
	internal class FTPGateway
	{
		/// <summary>
		/// Make a FTP request using the .NET FTPWebRequest class.
		/// WARNING: This method will throw.
		/// </summary>
		/// <param name="oSession">Session bearing an FTP request</param>
		/// <param name="buffBody">Returns Response body stream</param>
		/// <param name="oRH">Returns generated Response headers</param>
		// Token: 0x06000262 RID: 610 RVA: 0x000164FC File Offset: 0x000146FC
		internal static void MakeFTPRequest(Session oSession, PipeReadBuffer buffBody, out HTTPResponseHeaders oRH)
		{
			if (!Utilities.HasHeaders(oSession.oRequest))
			{
				throw new ArgumentException("Session missing Request objects.");
			}
			if (buffBody == null)
			{
				throw new ArgumentException("Response Stream may not be null.");
			}
			string sFTPUri = oSession.fullUrl;
			FtpWebRequest oFTP = (FtpWebRequest)WebRequest.Create(sFTPUri);
			oFTP.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
			if (sFTPUri.EndsWith("/"))
			{
				oFTP.Method = "LIST";
			}
			else
			{
				oFTP.Method = "RETR";
				if (oSession.oFlags.ContainsKey("FTP-UseASCII"))
				{
					oFTP.UseBinary = false;
				}
				else
				{
					oFTP.UseBinary = FiddlerApplication.Prefs.GetBoolPref("fiddler.ftp.UseBinary", true);
				}
			}
			if (!string.IsNullOrEmpty(oSession.oRequest.headers.UriUserInfo))
			{
				string sAuth = Utilities.TrimAfter(oSession.oRequest.headers.UriUserInfo, '@');
				sAuth = Utilities.UrlDecode(sAuth);
				string sUser = Utilities.TrimAfter(sAuth, ':');
				string sPass = (sAuth.Contains(":") ? Utilities.TrimBefore(sAuth, ':') : string.Empty);
				oFTP.Credentials = new NetworkCredential(sUser, sPass);
			}
			else if (oSession.oRequest.headers.ExistsAndContains("Authorization", "Basic "))
			{
				string sAuth2 = oSession.oRequest.headers["Authorization"].Substring(6);
				sAuth2 = Encoding.UTF8.GetString(Convert.FromBase64String(sAuth2));
				string sUser2 = Utilities.TrimAfter(sAuth2, ':');
				string sPass2 = Utilities.TrimBefore(sAuth2, ':');
				oFTP.Credentials = new NetworkCredential(sUser2, sPass2);
			}
			else if (oSession.oFlags.ContainsKey("x-AutoAuth") && oSession.oFlags["x-AutoAuth"].Contains(":"))
			{
				string sUser3 = Utilities.TrimAfter(oSession.oFlags["x-AutoAuth"], ':');
				string sPass3 = Utilities.TrimBefore(oSession.oFlags["x-AutoAuth"], ':');
				oFTP.Credentials = new NetworkCredential(sUser3, sPass3);
			}
			else if (FiddlerApplication.Prefs.GetBoolPref("fiddler.ftp.AlwaysDemandCredentials", false))
			{
				byte[] arrErrorBody = Encoding.UTF8.GetBytes("Please provide login credentials for this FTP server".PadRight(512, ' '));
				buffBody.Write(arrErrorBody, 0, arrErrorBody.Length);
				oRH = new HTTPResponseHeaders();
				oRH.SetStatus(401, "Need Creds");
				oRH.Add("Content-Length", buffBody.Length.ToString());
				oRH.Add("WWW-Authenticate", "Basic realm=\"ftp://" + oSession.host + "\"");
				return;
			}
			oFTP.UsePassive = FiddlerApplication.Prefs.GetBoolPref("fiddler.ftp.UsePassive", true);
			oFTP.Proxy = null;
			FtpWebResponse oResponse;
			try
			{
				oResponse = (FtpWebResponse)oFTP.GetResponse();
			}
			catch (WebException eX)
			{
				FtpWebResponse ftwError = (FtpWebResponse)eX.Response;
				if (ftwError != null)
				{
					byte[] arrErrorBody2;
					if (FtpStatusCode.NotLoggedIn == ftwError.StatusCode)
					{
						arrErrorBody2 = Encoding.UTF8.GetBytes("This FTP server requires login credentials".PadRight(512, ' '));
						buffBody.Write(arrErrorBody2, 0, arrErrorBody2.Length);
						oRH = new HTTPResponseHeaders();
						oRH.SetStatus(401, "Need Creds");
						oRH.Add("Content-Length", buffBody.Length.ToString());
						oRH.Add("WWW-Authenticate", "Basic realm=\"ftp://" + oSession.host + "\"");
						return;
					}
					arrErrorBody2 = Encoding.UTF8.GetBytes(string.Format("{0}{1}{2}", "Fiddler was unable to act as a HTTP-to-FTP gateway for this response. ", ftwError.StatusDescription, string.Empty.PadRight(512, ' ')));
					buffBody.Write(arrErrorBody2, 0, arrErrorBody2.Length);
				}
				else
				{
					byte[] arrErrorBody2 = Encoding.UTF8.GetBytes(string.Format("{0}{1}{2}", "Fiddler was unable to act as a HTTP-to-FTP gateway for this response. ", eX.Message, string.Empty.PadRight(512, ' ')));
					buffBody.Write(arrErrorBody2, 0, arrErrorBody2.Length);
				}
				oRH = new HTTPResponseHeaders();
				oRH.SetStatus(504, "HTTP-FTP Gateway failed");
				oRH.Add("Content-Length", buffBody.Length.ToString());
				return;
			}
			Stream oStream = oResponse.GetResponseStream();
			byte[] arrBuff = new byte[8192];
			for (int iRead = oStream.Read(arrBuff, 0, 8192); iRead > 0; iRead = oStream.Read(arrBuff, 0, 8192))
			{
				buffBody.Write(arrBuff, 0, iRead);
			}
			oRH = new HTTPResponseHeaders();
			oRH.SetStatus(200, "OK");
			oRH.Add("Date", DateTime.UtcNow.ToString("r"));
			oRH.Add("FTP-Status", Utilities.ConvertCRAndLFToSpaces(oResponse.StatusDescription));
			oRH.Add("Content-Length", buffBody.Length.ToString());
			oResponse.Close();
		}
	}
}
