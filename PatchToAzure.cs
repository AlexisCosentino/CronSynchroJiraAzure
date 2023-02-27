using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Policy;

namespace CronSynchroJiraAzure
{
    public class PatchToAzure
    {
        private string token;
        public string json { get; set; }
        public string url { get; set; }


        public PatchToAzure() { }

        public dynamic patchingToAzure()
        {
            get_credentials();
            dynamic result = null;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{token}:")));

                var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                {
                    Content = content
                };

                var response = client.SendAsync(request).Result;
                if (response.IsSuccessStatusCode)
                {
                    result = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync().Result);
                    Console.WriteLine(result);
                }
                else
                {
                    result = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync().Result);
                    Console.WriteLine("Request failed: " + result.StatusCode);
                }
                return result;
            }
        }


        private void get_credentials()
        {
            JObject data = JObject.Parse(File.ReadAllText("data.json"));
            this.token = (string)data["azureToken"];

        }
    }
}
