using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Mail;
using System.Web;

namespace CronSynchroJiraAzure
{
    public class PostAttachment
    {
        public PostAttachment() { }

        public string PostAttachmentToAzureServer(string attachment, string project)
        {
            var filename = attachment.Split('/').Last();
            filename = HttpUtility.UrlEncode(filename);

            string url = $"https://dev.azure.com/IRIUMSOFTWARE/{project}/_apis/wit/attachments?fileName={filename}&api-version=6.0";


            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/octet-stream"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", get_credentials("azureToken")))));

            WebClient wc = new WebClient();

            wc.Headers.Add("Authorization", "Basic " + GetEncodedCredentials());

            try
            {
                byte[] byteData = wc.DownloadData(attachment);
                dynamic att_url = postImgToAzure(byteData, client, url).Result;
                att_url = JsonConvert.DeserializeObject<object>(att_url);
                return att_url["url"].ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR IMAGE------------------------------------------------- {ex.ToString()} ------------------");
                return "https://upload.wikimedia.org/wikipedia/commons/thumb/f/f7/Generic_error_message.png/220px-Generic_error_message.png";
            }

        }

        private string GetEncodedCredentials()
        {
            string mergedCredentials = string.Format("{0}:{1}", get_credentials("jira_username"), get_credentials("jira_pwd"));
            byte[] byteCredentials = UTF8Encoding.UTF8.GetBytes(mergedCredentials);
            return Convert.ToBase64String(byteCredentials);
        }

        private string get_credentials(string key)
        {
            JObject data = JObject.Parse(File.ReadAllText("data.json"));
            return (string)data[key];
        }

        public async Task<string> postImgToAzure(byte[] byteData, HttpClient client, string url)
        {
            try
            {
                // Send asynchronous POST request.
                using (var content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    HttpResponseMessage response = client.PostAsync(url, content).Result;
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ex.ToString();
            }
        }
    }
}
