using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;

namespace CronSynchroJiraAzure
{
    internal class GetSQL
    {
        private string username;
        private string dbname;
        private string hostname;
        private string password;
        public string query { get; set; }

        public GetSQL(string query)
        {
            this.query = query;
        }

        public List<JiraEntity> getListOfTicket()
        {
            get_credentials();
            var list = new List<JiraEntity>();
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

                        var entity = new JiraEntity();

                        /**
                        entity.issueNb = reader[0].ToString();
                        entity.summary = reader[1].ToString();
                        entity.updated = reader[2].ToString();
                        entity.issueStatus = reader[3].ToString();
                        **/

                       
                        entity.issueNb =  reader[0].ToString();
                        entity.project = reader[1].ToString();
                        entity.reporter= reader[2].ToString();
                        entity.assignee= reader[3].ToString();
                        entity.creator = reader[4].ToString();
                        entity.summary= reader[5].ToString();
                        entity.description= reader[6].ToString();
                        entity.created= reader[7].ToString();
                        entity.updated= reader[8].ToString();
                        entity.dueDate= reader[9].ToString();
                        entity.projectName= reader[10].ToString();
                        entity.issueType= reader[11].ToString();
                        entity.issueStatus= reader[12].ToString();
                        entity.priority= reader[13].ToString();
                        entity.componentList= reader[15].ToString();
                        entity.fixedVersionList= reader[16].ToString();
                        entity.labelList= reader[17].ToString();
                        entity.sprintList= reader[18].ToString();
                        entity.startDate= reader[19].ToString();
                        entity.endDate= reader[20].ToString();
                        entity.worklog= reader[21].ToString();
                        entity.linkToJira= reader[23].ToString();
                        entity.projectCategoryType= reader[25].ToString();
                        entity.service = reader[26].ToString();
                        entity.originalEstimate= reader[27].ToString();
                        entity.linkChild= reader[28].ToString();
                        entity.linkParent= reader[29].ToString();
                        entity.azureLink= reader[30].ToString();
                        list.Add(entity);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Globals.Logger.Error(ex.ToString());

                }
                finally
                {
                    reader.Close();
                    //Console.WriteLine(JsonConvert.SerializeObject(list).ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Globals.Logger.Error(ex.ToString());
            }
            finally
            {
                conn.Close();
            }
            return list;
        }

        public int getNbDaysKO()
        {
            get_credentials();
            string numberOfDays = null;
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
                        numberOfDays = reader[1].ToString();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Globals.Logger.Error(ex.ToString());

                }
                finally
                {
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Globals.Logger.Error(ex.ToString());
            }
            finally
            {
                conn.Close();
            }
            return ParseStringToInt(numberOfDays);
        }

        public string getLastComment()
        {
            get_credentials();
            SqlConnection conn = new SqlConnection($"Server={hostname};Database={dbname};User Id={username};Password={password};");
            string comment = "";
            string date = "";
            string user = "";
            try
            {
                SqlCommand command = new SqlCommand(this.query, conn);
                conn.Open();
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    while (reader.Read())
                    {
                        comment = reader[1].ToString();
                        user = reader[0].ToString();
                        date = reader[2].ToString();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Globals.Logger.Error(ex.ToString());
                }
                finally
                {
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Globals.Logger.Error(ex.ToString());
            }
            finally
            {
                conn.Close();
            }
            comment = cleaning(comment);
            if (!string.IsNullOrEmpty(comment))
            {
                return String.Join("<br><br>", $"<h2><strong>Ecrit par {user}</strong></h2> <h4>Le {date}</h4>", comment);
            }
            else
            {
                return null;
            }
        }

        public List<string> getDate()
        {
            get_credentials();
            SqlConnection conn = new SqlConnection($"Server={hostname};Database={dbname};User Id={username};Password={password};");
            string id = "";
            string startDate = "";
            string endDate = "";
            try
            {
                SqlCommand command = new SqlCommand(this.query, conn);
                conn.Open();
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    while (reader.Read())
                    {
                        startDate = reader[1].ToString();
                        id = reader[0].ToString();
                        endDate = reader[2].ToString();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Globals.Logger.Error(ex.ToString());
                }
                finally
                {
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Globals.Logger.Error(ex.ToString());
            }
            finally
            {
                conn.Close();
            }
            return new List<string>{ id, startDate, endDate };

        }

        public List<Comment> getComment()
        {
            get_credentials();
            SqlConnection conn = new SqlConnection($"Server={hostname};Database={dbname};User Id={username};Password={password};");
            List<Comment> comments = new List<Comment>();
            try
            {
                SqlCommand command = new SqlCommand(this.query, conn);
                conn.Open();
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    while (reader.Read())
                    {
                        comments.Add(new Comment(reader[1].ToString(), reader[2].ToString(), DateTime.ParseExact(reader[3].ToString(), "yyyy-MM-dd'T'HH:mm:ss.ff'Z'", CultureInfo.InvariantCulture)));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Globals.Logger.Error(ex.ToString());
                }
                finally
                {
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Globals.Logger.Error(ex.ToString());
            }
            finally
            {
                conn.Close();
            }
            return comments;

        }

        public List<string> getAttachments() 
        {
            get_credentials();
            SqlConnection conn = new SqlConnection($"Server={hostname};Database={dbname};User Id={username};Password={password};");
            List<string> attachments = new List<string>();
            try
            {
                SqlCommand command = new SqlCommand(this.query, conn);
                conn.Open();
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    while (reader.Read())
                    {
                        attachments.Add($"https://worklog.irium-software.com/secure/attachment/{reader[0]}/{reader[2]}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Globals.Logger.Error(ex.ToString());
                }
                finally
                {
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Globals.Logger.Error(ex.ToString());
            }
            finally
            {
                conn.Close();
            }
            return attachments;

        }

        private void get_credentials()
        {
            JObject data = JObject.Parse(File.ReadAllText("data.json"));
            this.password = (string)data["db_pwd"];
            this.username = (string)data["db_username"];
            this.dbname = (string)data["db_name"];
            this.hostname = (string)data["db_hostname"];
        }

        public static int ParseStringToInt(string inputString)
        {
            int parsedInt;

            if (int.TryParse(inputString, out parsedInt))
            {
                return parsedInt;
            }

            return 0;
        }

        public string cleaning(string input)
        {
            input = input.Replace("{code:java}", "<code>");
            input = input.Replace("{code:java}", "<code>");
            input = input.Replace("{code}", "</code>");
            input = input.Replace("\r\n *****", "<br>&emsp;&emsp;&emsp;&emsp;&emsp;\t■");
            input = input.Replace("\r\n ****", "<br>&emsp;&emsp;&emsp;&emsp;\t■");
            input = input.Replace("\r\n ***", "<br>&emsp;&emsp;&emsp;\t■");
            input = input.Replace("\r\n **", "<br>&emsp;&emsp;\t■");
            input = input.Replace("\r\n *", "<br>&emsp;\t■");
            input = input.Replace("\r\n", "<br>"); //Transate line breaker
            input = input.Replace("\"", " "); // Remove every double quote of the text
            input = input.Replace("\\", "");  // Remove every backslash of the text
            input = input.Replace("*[", "<strong>[");
            input = input.Replace("]*", "]</strong>");
            return input;
        }

    }
}
