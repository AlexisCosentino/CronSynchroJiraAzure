using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CronSynchroJiraAzure
{
    public class UpdateSQL
    {
        private string username;
        private string dbname;
        private string hostname;
        private string password;
        public string query { get; set; }

        public UpdateSQL(string query) 
        {
            this.query = query;
        }

        public void UpdateRow()
        {
            get_credentials();
            SqlConnection conn = new SqlConnection($"Server={hostname};Database={dbname};User Id={username};Password={password};");
            {
                conn.Open();

                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    int rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine($"{rowsAffected} row(s) updated.");
                }
            }
        }

        public string getJiraID()
        {
            string jiraID = "";
            get_credentials();
            SqlConnection conn = new SqlConnection($"Server={hostname};Database={dbname};User Id={username};Password={password};");
            try
            {
                SqlCommand command = new SqlCommand(this.query, conn);
                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        jiraID = reader[0].ToString();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                conn.Close();
            }
            return jiraID;
        }

        private void get_credentials()
        {
            JObject data = JObject.Parse(File.ReadAllText("data.json"));
            this.password = (string)data["db_pwd"];
            this.username = (string)data["db_username"];
            this.dbname = (string)data["db_name"];
            this.hostname = (string)data["db_hostname"];
        }
    }
}
