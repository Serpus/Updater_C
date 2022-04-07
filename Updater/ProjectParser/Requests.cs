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
            Log.Info("responseRequestCode: " + response.StatusCode);
            Log.Info("responseRequest: " + responseStr);
            return responseStr;
        }

        public static async Task<String> postRequestAsyncJenkins(String url)
        {
            username = Data.username;
            password = Data.password;

            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            var baseAddress = new Uri("https://ci-sel.dks.lanit.ru/jenkins/");

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

            var response = await client.GetAsync("https://ci-sel.dks.lanit.ru/jenkins/crumbIssuer/api/json?pretty=true");
            IEnumerable<string> headerCookie = null;
            foreach (var header in response.Headers)
            {
                if(header.Key.Equals("Set-Cookie"))
                {
                    headerCookie = header.Value;
                }
            }

            var responseStr1 = await response.Content.ReadAsStringAsync();
            CrumbResponseParser crumb = JsonConvert.DeserializeObject<CrumbResponseParser>(responseStr1);
            client.DefaultRequestHeaders.Add(crumb.CrumbRequestField, crumb.Crumb);

            String[] str = headerCookie.First().Substring(0, headerCookie.First().IndexOf(";")).Split('=');
            cookies.Add(baseAddress, new Cookie(str[0], str[1]));

            var response2 = await client.PostAsync(url, null);

            var responseStr = await response2.Content.ReadAsStringAsync();
            return responseStr;
        }

        public static async Task<String> postRequestAsync(String url, object jsonBodyClass)
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
            Log.Info("jsonBody: " + JsonConvert.SerializeObject(jsonBodyClass));
            var content = new StringContent(JsonConvert.SerializeObject(jsonBodyClass), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            
            var responseStr = await response.Content.ReadAsStringAsync();
            Log.Info("response: " + responseStr);
            return responseStr;
        }
    }
}
