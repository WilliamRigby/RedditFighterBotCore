using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RedditFighterBot
{
    public static class BingSpellCheckAccessor
    {

        private const string apiKey1 = "de9a7d7224d046b68e6b38c9367135d7";
        private const string apiKey2 = "ab55a194166b418d8b47e04b259fc3a7";
        private const string endpoint = "https://api.cognitive.microsoft.com/bing/v5.0/spellcheck";

        public static async Task<string> SendRequestAsync(string url)
        {
            string response_string = null;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey1);
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US");
                var response = await httpClient.GetAsync(url);
                response_string = await response.Content.ReadAsStringAsync();

                response.Dispose();
            }

            return response_string;
        }

        public static string GenerateRequestUri(string text)
        {
            string requestUri = endpoint;
            requestUri += string.Format("?text={0}", text);
            requestUri += "&mode=spell";
            requestUri += "&cc=US";
            requestUri += "&mkt=en-US";
            return requestUri;
        }
    }
}
