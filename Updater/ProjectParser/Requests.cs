using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace Updater
{
    internal class Requests
    {
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

        private static string username;
        private static string password;
        internal static String error { get; set; }

        public static String getRequest(String url)
        {
            username = Data.username;
            password = Data.password;

            WebClient client = new WebClient();
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.Headers.Add("user-agent", "Updater");
            client.Headers.Add("Cookie", "bamboouserauth=triangle-happier-ecard-climate-scoreless-stubborn");
            client.Headers.Add("Accept", "application/json");
            client.Headers.Add("Authorization", $"Basic {base64}");
            String s = "";

            try
            {
                Stream data = client.OpenRead(url);
                StreamReader reader = new StreamReader(data);
                s = reader.ReadToEnd();
                data.Close();
                reader.Close();
            } catch (WebException ex)
            {
                WebExceptionStatus status = ex.Status;

                if (status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)ex.Response;
                    Log.Info("Статусный код ошибки: {0} - {1}",
                            (int)httpResponse.StatusCode, httpResponse.StatusCode);
                    error = $"{(int)httpResponse.StatusCode} - {httpResponse.StatusCode}";
                }

                return null;
            };

            return s;
        }
    }
}
