using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CronSynchroJiraAzure
{
    public class GetAzure
    {
        private string token;
        public string url { get; set; }

        public GetAzure() { }

        public dynamic GettingFromAzure()
        {
            get_credentials();
            dynamic result = null;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{token}:")));

                var response = client.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    result = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync().Result);
                    Console.WriteLine(result);
                }
                else
                {
                    result = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync().Result);
                    Console.WriteLine("Request failed: " + result);
                    Globals.Logger.Error("Request failed : " + result);
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
