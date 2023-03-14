using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CronSynchroJiraAzure
{
    public class PostToAzure
    {
        private string token;
        public string json { get; set; }
        public string url { get; set; }
        public string contentType { get; set; } = "application/json-patch+json";


        public PostToAzure() { }

        public dynamic postingToAzure()
        {
            get_credentials();
            dynamic result = null;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{token}:")));

                var response = client.PostAsync(url, new StringContent(json, Encoding.UTF8, this.contentType)).Result;
                if (response.IsSuccessStatusCode)
                {
                    result = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync().Result);
                    Console.WriteLine(result);
                }
                else
                {
                    result = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync().Result);
                    Console.WriteLine("Request failed: " + result);
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
