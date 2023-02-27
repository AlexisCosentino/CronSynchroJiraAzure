using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace CronSynchroJiraAzure
{
    public class CreateJsonBody
    {
        public JiraEntity entity { get; set; }
        public dynamic ticket { get; set; }
        public CreateJsonBody(JiraEntity entity) 
        {
            this.entity = entity;
        }

        public CreateJsonBody()
        { }
        public string createJsonWithPBIToPost(string type)
        {
            var parsedDate = DateTime.Parse(entity.created);
            if (parsedDate.Date == DateTime.Today)
            {
                // WARNING = if date of creation is less than 2hours, an error gonna occurs, thats why i substract 2h in case of ticket from today.
                parsedDate = parsedDate.AddHours(-2);
            }
            //"2022-11-29T12:26:05.707"
            var createdDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc).ToString("s") + ".000Z";

            string dueDate = translateDateTimeToAzure(entity.dueDate);
            string startDate = translateDateTimeToAzure(entity.startDate);
            string endDate = translateDateTimeToAzure(entity.endDate);
            string originalEstimate = getOriginalEstimateInHour(entity.originalEstimate);
            string creator = mappingUsers(entity.creator);
            string assignee = mappingUsers(entity.assignee);
            string reporter = mappingUsers(entity.reporter);

            if (entity.projectCategoryType == "Projet interne (DDP)" || entity.projectCategoryType == "Projet interne (DID)")
            {
                entity.projectName = "";
            }


            foreach (PropertyInfo property in entity.GetType().GetProperties())
            {
                if (property.Name != "areaPath")
                {
                    property.SetValue(entity, cleanJson((string)property.GetValue(entity)));
                }
            }

            var component = getOneComponent(entity.componentList);




            string jsonToPost = "[{ \"op\": \"add\", \"path\": \"/fields/System.Title\", \"from\": null, \"value\": \"" + entity.summary + "\"}";
            jsonToPost += ", { \"op\": \"add\", \"path\": \"/fields/System.Description\", \"from\": null, \"value\": \"" + entity.description + "\"} ";
            jsonToPost += ", { \"op\": \"add\", \"path\": \"/fields/System.State\", \"from\": null, \"value\": \"" + entity.issueStatus + "\"}";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/System.CreatedBy\", \"value\": \"" + creator + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/System.AssignedTo\", \"value\": \"" + assignee + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/System.CreatedDate\", \"value\": \"" + createdDate + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/System.AreaPath\", \"value\": \"" + entity.areaPath + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/System.Tags\", \"value\": \"" + entity.labelList + " \" }";
            //           jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Custom.Type\", \"value\": \"" + ticketData["issueType"] + "\" }";
            //           jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Custom.PriorityField\", \"value\": \"" + ticketData["priority"] + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Microsoft.VSTS.Scheduling.DueDate\", \"value\": \"" + dueDate + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Microsoft.VSTS.Scheduling.StartDate\", \"value\": \"" + startDate + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Microsoft.VSTS.Scheduling.OriginalEstimate\", \"value\": \"" + originalEstimate + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Microsoft.VSTS.Scheduling.Effort\", \"value\": \"" + originalEstimate + "\" }";
            //            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Custom.Enddate\", \"value\": \"" + endDate + "\" }";
            //            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Custom.WorkLog\", \"value\": \"" + worklog + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Custom.JiraLink\", \"value\": \"https://worklog.vega-systems.com/browse/" + entity.linkToJira + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Custom.Version\", \"value\": \"" + entity.fixedVersionList + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Custom.Order\", \"value\": \"" + component + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Custom.Customer\", \"value\": \"" + entity.projectName + "\" }";
            jsonToPost = check4sprint(jsonToPost, entity);

            if (type == "validate")
            {
                jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Custom.Toestimate\", \"value\": \"true\" }";
            }

            jsonToPost += "]";
            Console.WriteLine(jsonToPost);
            return jsonToPost;
        }

        public string cleanJson(string toformat)
        {
            toformat = toformat.Replace("{code:java}", "<code>");
            toformat = toformat.Replace("{code:java}", "<code>");

            toformat = toformat.Replace("{code}", "</code>");
            toformat = toformat.Replace("\r\n *****", "<br>&emsp;&emsp;&emsp;&emsp;&emsp;\t■");
            toformat = toformat.Replace("\r\n ****", "<br>&emsp;&emsp;&emsp;&emsp;\t■");
            toformat = toformat.Replace("\r\n ***", "<br>&emsp;&emsp;&emsp;\t■");
            toformat = toformat.Replace("\r\n **", "<br>&emsp;&emsp;\t■");
            toformat = toformat.Replace("\r\n *", "<br>&emsp;\t■");
            toformat = toformat.Replace("\r\n", "<br>"); //Transate line breaker
            toformat = toformat.Replace("\"", " "); // Remove every double quote of the text
            toformat = toformat.Replace("\\", "");  // Remove every backslash of the text
            toformat = toformat.Replace("*[", "<strong>[");
            toformat = toformat.Replace("]*", "]</strong>");
            return toformat;
        }

        public string translateDateTimeToAzure(string date)
        {
            if (!String.IsNullOrEmpty(date))
            {
                date = DateTime.SpecifyKind(DateTime.Parse(date), DateTimeKind.Utc).ToString("s") + ".000Z";
            }
            return date;
        }

        public string mappingUsers(string user)
        {
            if (!String.IsNullOrEmpty(user))
            {
                var temp = user.Split('@');
                if (temp[0] == "yvinee")
                {
                    return "y.vinee@irium-software.com";
                }
                if (temp[0] == "lpatissier")
                {
                    return "l.patissier@irium-software.com";
                }
                if (temp[0] == "cdavid")
                {
                    return "chdavid@irium-software.com";
                }
                return temp[0] + "@irium-software.com";
            }
            return "";
        }

        public string getOriginalEstimateInHour(string time)
        {
            if (!String.IsNullOrEmpty(time))
            {
                int timeInt = Int32.Parse(time);
                timeInt = (timeInt / 60) / 60;
                return timeInt.ToString();
            }
            return "";

        }

        public string getOneComponent(string c_list)
        {

            string[] subs = c_list.Split(';');
            return subs[0];
        }

        public string check4sprint(string jsonToPost, JiraEntity entity)
        {
            if (!String.IsNullOrEmpty(entity.sprintList))
            {
                var sprints = entity.sprintList.Split(',');
                string sprint = sprints.Last();
                var name = sprint.Split('_');
                if (name[0] == "MOB" && entity.azureProject == "Mobilité")
                {
                    return jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/System.IterationPath\", \"value\": \"\\\\Mobility\\\\" + sprint + "\" }";
                }
                else if (name[0] == "INNOV" && entity.azureProject == "Digital")
                {
                    return jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/System.IterationPath\", \"value\": \"\\\\Digital\\\\" + sprint + "\" }";
                }
                else if (name[0] == "DIG" && entity.azureProject == "Digital")
                {
                    return jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/System.IterationPath\", \"value\": \"\\\\Digital\\\\" + sprint + "\" }";
                }
                else if (name[0] == "DEV" && entity.azureProject == "Locpro")
                {
                    return jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/System.IterationPath\", \"value\": \"\\\\Locpro\\\\" + sprint + "\" }";

                }
                else
                {
                    foreach (string sp in entity.sprintList.Split(','))
                    {
                        if (!string.IsNullOrEmpty(sp))
                        {
                            entity.labelList += $"sprint : {sp};";
                        }
                    }
                }
            }

            return jsonToPost;
        }

        public string createJsonWithPBIToPostFromDynamic()
        {
            string areaPath = ticket.fields["System.AreaPath"].ToString().Replace("\\", "\\\\");

            var test = ticket.fields["Microsoft.VSTS.Scheduling.OriginalEstimate"] - 0; // tem^ps passé mais c'est fucking introuvable
            string jsonToPost = "[{ \"op\": \"add\", \"path\": \"/fields/System.Title\", \"from\": null, \"value\": \" [CLOSED SPRINT] " + ticket.fields["System.Title"] + "\"}";
            jsonToPost += ", { \"op\": \"add\", \"path\": \"/fields/System.Description\", \"from\": null, \"value\": \"" + ticket.fields["System.Description"] + "\"} ";
            jsonToPost += ", { \"op\": \"add\", \"path\": \"/fields/System.State\", \"from\": null, \"value\": \"To Do\"}";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/System.CreatedBy\", \"value\": \"" + ticket.fields["System.CreatedBy"].uniqueName + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/System.CreatedDate\", \"value\": \"" + ticket.fields["System.CreatedDate"] + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/System.AreaPath\", \"value\": \"" + areaPath + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/System.IterationPath\", \"value\": \"Mobility\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Microsoft.VSTS.Scheduling.RemainingWork\", \"value\": \"" + ticket.fields["Microsoft.VSTS.Scheduling.RemainingWork"] + "\" }";
            jsonToPost += ", {\"op\": \"add\", \"path\": \"/fields/Microsoft.VSTS.Scheduling.OriginalEstimate\", \"value\": \"" + test.ToString() + "\" }";
            jsonToPost += "]";
            Console.WriteLine(jsonToPost);
            return jsonToPost;
        }

    }
}
