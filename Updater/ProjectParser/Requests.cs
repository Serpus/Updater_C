using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;

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

        public static async Task<String> postRequestAsync(String url)
        {
            username = Data.username;
            password = Data.password;

            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            var baseAddress = new Uri("https://ci-sel.dks.lanit.ru");

            CookieContainer cookies = new CookieContainer();
            var handler = new HttpClientHandler()
            {
                CookieContainer = cookies
            };

            HttpClient client = new HttpClient(handler);
            client.BaseAddress = baseAddress;
            cookies.Add(baseAddress, new Cookie("bamboouserauth", "triangle-happier-ecard-climate-scoreless-stubborn"));

            client.DefaultRequestHeaders.Add("user-agent", "Updater");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {base64}");

            var response = await client.PostAsync(url, null);

            var responseStr = await response.Content.ReadAsStringAsync();
            return responseStr;
        }

        public static async Task<String> postRequestAsync(String url, object jsonBodyCass)
        {
            username = Data.username;
            password = Data.password;

            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            var baseAddress = new Uri("https://ci-sel.dks.lanit.ru");

            CookieContainer cookies = new CookieContainer();
            var handler = new HttpClientHandler()
            {
                CookieContainer = cookies
            };

            HttpClient client = new HttpClient(handler);
            client.BaseAddress = baseAddress;
            cookies.Add(baseAddress, new Cookie("bamboouserauth", "triangle-happier-ecard-climate-scoreless-stubborn"));

            client.DefaultRequestHeaders.Add("user-agent", "Updater");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {base64}");
            Log.Info("jsonBody: " + JsonConvert.SerializeObject(jsonBodyCass));
            var content = new StringContent(JsonConvert.SerializeObject(jsonBodyCass), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            
            var responseStr = await response.Content.ReadAsStringAsync();
            Log.Info("response: " + responseStr);
            return responseStr;
        }
    }
}
