using Quartz;
using Quartz.Impl;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.ComponentModel;
using System.Reflection;
using System.Net.Sockets;
using static System.Net.WebRequestMethods;
using System.Security.Policy;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CSharp.RuntimeBinder;
using System.Net.Mail;
using Newtonsoft.Json;
using System.Threading;
using System.CodeDom;
using System.Security.Cryptography;
using System.Net;
using System.Text;
using NLog;
using NLog.Fluent;
using System.Web;
using System.IO;

namespace CronSynchroJiraAzure
{
    class Program
    {
        static void Main(string[] args)
        {
            IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler().Result;

            scheduler.Start();

            IJobDetail job = JobBuilder.Create<ExecuteJob>().Build();
            ITrigger trigger = TriggerBuilder.Create()
                .WithSimpleSchedule(x => x
                    .WithIntervalInHours(1)
                    .RepeatForever())
                .Build();
            scheduler.ScheduleJob(job, trigger);

            Console.ReadLine();
            scheduler.Shutdown();
        }
    }

    public class ExecuteJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            NLog.LogManager.Setup().LoadConfiguration(builder => {
                builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole();
                builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToFile(fileName: "SyncLogFile.txt");
            });
            Globals.Logger.Debug("Ca démarre !");
            SyncJiraToAzure_Validate();
            SyncJiraToAzure_Accepted();
            SyncJira_KO();
            SyncAzure_Done();
            SyncAzure_Removed();
            SyncAzure_Sprint();
            SyncAzure_ClosedSprint();
            return Task.CompletedTask;
        }


        //TESTED AND WORKS JUST FINE
        public void SyncJiraToAzure_Validate()
        {
            var sql = new GetSQL("SELECT jiraissue.id, project, reporter = (select  lower_user_name from app_user where jiraissue.reporter=user_key), assignee = (select lower_user_name from app_user where jiraissue.assignee=user_key), creator = (select lower_user_name from app_user where jiraissue.creator=user_key), summary, jiraissue.description, created, updated, duedate, project.pname, issuetype.pname as type_of_issue, issuestatus.pname as status_of_issue, priority.pname as priority, issuestatus,componentName = (select STRING_AGG( ISNULL(component.cname, ''), ';') from component inner join nodeassociation on nodeassociation.ASSOCIATION_TYPE = 'IssueComponent' where nodeassociation.sink_node_id = component.id and jiraissue.id = nodeassociation.SOURCE_NODE_ID and jiraissue.id = jiraissue.id), fixedVersion = (select STRING_AGG( ISNULL(projectversion.vname, ''), ';') from projectversion inner join nodeassociation on nodeassociation.ASSOCIATION_TYPE = 'IssueFixVersion' where nodeassociation.sink_node_id = projectversion.id and jiraissue.id = nodeassociation.SOURCE_NODE_ID and jiraissue.id = jiraissue.id), labels =  (select STRING_AGG( ISNULL(label.label, ''), ',') from label where label.issue = jiraissue.id), sprint_name = (select STRING_AGG( ISNULL(s.name, ''), ',') FROM customfieldvalue cfv INNER JOIN AO_60DB71_SPRINT s ON CAST(s.ID AS VARCHAR(10)) = CAST(cfv.stringvalue AS VARCHAR(10)) INNER JOIN jiraissue i ON i.id = cfv.issue where i.ID = jiraissue.ID), start_date = (select customfieldvalue.DATEVALUE from customfieldvalue where customfieldvalue.issue = jiraissue.id and CUSTOMFIELD = 10303), end_date = (select customfieldvalue.DATEVALUE from customfieldvalue where customfieldvalue.issue = jiraissue.id and CUSTOMFIELD = 10304), worklog = (select STRING_AGG(ISNULL(convert(nvarchar(max), CONCAT('Le ',STARTDATE,', ', author, ' a travaillé ', cast((timeworked/60)/60 as decimal(9,2)), 'h.')), ''), ';') from worklog where worklog.issueid = jiraissue.id), total_work_time = (select cast(sum((timeworked/60)/60) as decimal(9,2)) from worklog where worklog.issueid = jiraissue.id), jira_link = CONCAT(project.pkey, '-', jiraissue.issuenum), worklog2 = (select STRING_AGG(convert(nvarchar(max), ISNULL(CONCAT(CONVERT(VARCHAR, STARTDATE, 20), '~', author, '~', timeworked), ';')), ';') from worklog inner join cwd_user on user_name= AUTHOR where worklog.issueid = jiraissue.id), projectCategory = (select pc.cname from projectcategory pc left join nodeassociation na on project.id=na.source_node_id and na.sink_node_entity='ProjectCategory' where na.sink_node_id = pc.id), service = (select cfo.customvalue from customfieldoption cfo join customfield cf on cf.ID = 11100 inner join customfieldvalue cfv on cfv.STRINGVALUE = CAST(cfo.id AS NVARCHAR) and cfv.issue = jiraissue.id where cfo.customfield = 11100), jiraissue.TIMEORIGINALESTIMATE,  issue_linked_child = (select STRING_AGG( ISNULL(CONCAT(p.pkey, '-', j.issuenum), ''), ';') from jiraissue j , project p inner join  issuelink on DESTINATION = jiraissue.id where j.id = SOURCE and p.id = j.PROJECT), issue_linked_parent = (select STRING_AGG( ISNULL(CONCAT(p.pkey, '-', j.issuenum), ''), ';') from jiraissue j , project p inner join  issuelink on SOURCE = jiraissue.id where j.id = DESTINATION and p.id = j.PROJECT), azure_link = (select customfieldvalue.STRINGVALUE from customfieldvalue where customfieldvalue.issue = jiraissue.id and CUSTOMFIELD = 11900) FROM jiraissue, project, issuetype, issuestatus, priority WHERE jiraissue.priority=priority.ID and issuestatus.ID=jiraissue.issuestatus and issuetype.id=jiraissue.issuetype and project.id=jiraissue.project and issuetype != 10800 and not (project.id = 10000 or project.id= 13301) and  jiraissue.updated > DATEADD(hour, -1, GETDATE()) and jiraissue.issuestatus = 10003 order by UPDATED desc;");
            var listTicket = sql.getListOfTicket();

            foreach (JiraEntity ticket in listTicket)
            {
                ticket.doMethods();
                ticket.printEntity();
                if (String.IsNullOrEmpty(ticket.azureLink)) //If ticket doesnt exist on azure
                {
                    ticket.issueStatus = "New";
                    var json = new CreateJsonBody(ticket);
                    var jsonBody = json.createJsonWithPBIToPost("validate");
                    var posting = new PostToAzure();
                    posting.url = $"https://dev.azure.com/IRIUMSOFTWARE/{ticket.azureProject}/_apis/wit/workitems/${ticket.issueType}?bypassRules=true&api-version=6.0";
                    posting.json = jsonBody;
                    var result = posting.postingToAzure();
                    var update = new UpdateSQL($"INSERT INTO customfieldvalue (id, issue, CUSTOMFIELD, stringvalue) SELECT MAX(ID)+1, {ticket.issueNb}, 11900, 'https://dev.azure.com/IRIUMSOFTWARE/_workitems/edit/{result.id}' FROM Jira_Prod.dbo.customfieldvalue;");
                    update.UpdateRow();

                    //Send attachment to Azure
                    sql = new GetSQL($"SELECT id ,mimetype, filename FROM fileattachment Where issueid = {ticket.issueNb};");
                    List<string> attachments = sql.getAttachments();
                    try
                    {
                        patchPBIWithAttachmentFromJira(attachments, ticket.azureProject, result.id.ToString(), result.fields["System.Description"].ToString());
                    } catch (Exception ex)
                    {
                        Globals.Logger.Error("Une erreur est survenue : {0}", ex.Message);
                    }

                } else
                {
                    // Faire un Patch
                    string azureID = ticket.azureLink.Split('/').Last();
                    var patch = new PatchToAzure();
                    patch.json = "[{\"op\": \"add\", \"path\": \"/fields/System.State\", \"value\": \"New\" }, {\"op\": \"add\", \"path\": \"/fields/Custom.Toestimate\", \"value\": \"true\" }]";
                    patch.url = $"https://dev.azure.com/IRIUMSOFTWARE/_apis/wit/workitems/{azureID}?api-version=7.0";
                    patch.patchingToAzure();
                }
            }
        }


        //TESTED AND WORKS JUST FINE
        public void SyncJiraToAzure_Accepted()
        {
            var sql = new GetSQL("SELECT jiraissue.id, project, reporter = (select  lower_user_name from app_user where jiraissue.reporter=user_key), assignee = (select lower_user_name from app_user where jiraissue.assignee=user_key), creator = (select lower_user_name from app_user where jiraissue.creator=user_key), summary, jiraissue.description, created, updated, duedate, project.pname, issuetype.pname as type_of_issue, issuestatus.pname as status_of_issue, priority.pname as priority, issuestatus,componentName = (select STRING_AGG( ISNULL(component.cname, ''), ';') from component inner join nodeassociation on nodeassociation.ASSOCIATION_TYPE = 'IssueComponent' where nodeassociation.sink_node_id = component.id and jiraissue.id = nodeassociation.SOURCE_NODE_ID and jiraissue.id = jiraissue.id), fixedVersion = (select STRING_AGG( ISNULL(projectversion.vname, ''), ';') from projectversion inner join nodeassociation on nodeassociation.ASSOCIATION_TYPE = 'IssueFixVersion' where nodeassociation.sink_node_id = projectversion.id and jiraissue.id = nodeassociation.SOURCE_NODE_ID and jiraissue.id = jiraissue.id), labels =  (select STRING_AGG( ISNULL(label.label, ''), ',') from label where label.issue = jiraissue.id), sprint_name = (select STRING_AGG( ISNULL(s.name, ''), ',') FROM customfieldvalue cfv INNER JOIN AO_60DB71_SPRINT s ON CAST(s.ID AS VARCHAR(10)) = CAST(cfv.stringvalue AS VARCHAR(10)) INNER JOIN jiraissue i ON i.id = cfv.issue where i.ID = jiraissue.ID), start_date = (select customfieldvalue.DATEVALUE from customfieldvalue where customfieldvalue.issue = jiraissue.id and CUSTOMFIELD = 10303), end_date = (select customfieldvalue.DATEVALUE from customfieldvalue where customfieldvalue.issue = jiraissue.id and CUSTOMFIELD = 10304), worklog = (select STRING_AGG(ISNULL(convert(nvarchar(max), CONCAT('Le ',STARTDATE,', ', author, ' a travaillé ', cast((timeworked/60)/60 as decimal(9,2)), 'h.')), ''), ';') from worklog where worklog.issueid = jiraissue.id), total_work_time = (select cast(sum((timeworked/60)/60) as decimal(9,2)) from worklog where worklog.issueid = jiraissue.id), jira_link = CONCAT(project.pkey, '-', jiraissue.issuenum), worklog2 = (select STRING_AGG(convert(nvarchar(max), ISNULL(CONCAT(CONVERT(VARCHAR, STARTDATE, 20), '~', author, '~', timeworked), ';')), ';') from worklog inner join cwd_user on user_name= AUTHOR where worklog.issueid = jiraissue.id), projectCategory = (select pc.cname from projectcategory pc left join nodeassociation na on project.id=na.source_node_id and na.sink_node_entity='ProjectCategory' where na.sink_node_id = pc.id), service = (select cfo.customvalue from customfieldoption cfo join customfield cf on cf.ID = 11100 inner join customfieldvalue cfv on cfv.STRINGVALUE = CAST(cfo.id AS NVARCHAR) and cfv.issue = jiraissue.id where cfo.customfield = 11100), jiraissue.TIMEORIGINALESTIMATE,  issue_linked_child = (select STRING_AGG( ISNULL(CONCAT(p.pkey, '-', j.issuenum), ''), ';') from jiraissue j , project p inner join  issuelink on DESTINATION = jiraissue.id where j.id = SOURCE and p.id = j.PROJECT), issue_linked_parent = (select STRING_AGG( ISNULL(CONCAT(p.pkey, '-', j.issuenum), ''), ';') from jiraissue j , project p inner join  issuelink on SOURCE = jiraissue.id where j.id = DESTINATION and p.id = j.PROJECT), azure_link = (select customfieldvalue.STRINGVALUE from customfieldvalue where customfieldvalue.issue = jiraissue.id and CUSTOMFIELD = 11900) FROM jiraissue, project, issuetype, issuestatus, priority WHERE jiraissue.priority=priority.ID and issuestatus.ID=jiraissue.issuestatus and issuetype.id=jiraissue.issuetype and project.id=jiraissue.project and issuetype != 10800 and not (project.id = 10000 or project.id= 13301) and jiraissue.updated > DATEADD(hour, -1, GETDATE()) and jiraissue.issuestatus = 10000 order by UPDATED desc;");
            var listTicket = sql.getListOfTicket();

            foreach (JiraEntity ticket in listTicket)
            {
                ticket.doMethods();
                ticket.printEntity();
                if (String.IsNullOrEmpty(ticket.azureLink)) //If ticket doesnt exist on azure
                {
                    ticket.issueStatus = "New";
                    var json = new CreateJsonBody(ticket);
                    var jsonBody = json.createJsonWithPBIToPost("accepted");
                    var posting = new PostToAzure();
                    posting.url = $"https://dev.azure.com/IRIUMSOFTWARE/{ticket.azureProject}/_apis/wit/workitems/${ticket.issueType}?bypassRules=true&api-version=6.0";
                    posting.json = jsonBody;
                    var result = posting.postingToAzure();
                    // Post last comment
                    GetLastCommentAndPostToAzure(ticket, result.id.ToString());

                    //Send attachment to Azure
                    sql = new GetSQL($"SELECT id ,mimetype, filename FROM fileattachment Where issueid = {ticket.issueNb};");
                    List<string> attachments = sql.getAttachments();
                    try
                    {
                        patchPBIWithAttachmentFromJira(attachments, ticket.azureProject, result.id.ToString(), result.fields["System.Description"].ToString());
                    }
                    catch (Exception ex)
                    {
                        Globals.Logger.Error("Une erreur est survenue : {0}", ex.Message);
                    }
                    //Add link azure link to jira
                    var update = new UpdateSQL($"INSERT INTO customfieldvalue (id, issue, CUSTOMFIELD, stringvalue) SELECT MAX(ID)+1, {ticket.issueNb}, 11900, 'https://dev.azure.com/IRIUMSOFTWARE/_workitems/edit/{result.id}' FROM Jira_Prod.dbo.customfieldvalue;");
                    update.UpdateRow();
                }
                else
                {
                    // Faire un Patch
                    string azureID = ticket.azureLink.Split('/').Last();
                    var patch = new PatchToAzure();
                    patch.json = "[{\"op\": \"add\", \"path\": \"/fields/System.State\", \"value\": \"New\" }]";
                    patch.url = $"https://dev.azure.com/IRIUMSOFTWARE/_apis/wit/workitems/{azureID}?api-version=7.0";
                    patch.patchingToAzure();
                    GetLastCommentAndPostToAzure(ticket, azureID);
                }
            }
        }


        //TESTED AND WORKS JUST FINE
        public void SyncJira_KO()
        {
            // Obtenir les ticket dont status KO depuis moins de 1h
            var sql = new GetSQL("SELECT jiraissue.id, project, reporter = (select  lower_user_name from app_user where jiraissue.reporter=user_key), assignee = (select lower_user_name from app_user where jiraissue.assignee=user_key), creator = (select lower_user_name from app_user where jiraissue.creator=user_key), summary, jiraissue.description, created, updated, duedate, project.pname, issuetype.pname as type_of_issue, issuestatus.pname as status_of_issue, priority.pname as priority, issuestatus,componentName = (select STRING_AGG( ISNULL(component.cname, ''), ';') from component inner join nodeassociation on nodeassociation.ASSOCIATION_TYPE = 'IssueComponent' where nodeassociation.sink_node_id = component.id and jiraissue.id = nodeassociation.SOURCE_NODE_ID and jiraissue.id = jiraissue.id), fixedVersion = (select STRING_AGG( ISNULL(projectversion.vname, ''), ';') from projectversion inner join nodeassociation on nodeassociation.ASSOCIATION_TYPE = 'IssueFixVersion' where nodeassociation.sink_node_id = projectversion.id and jiraissue.id = nodeassociation.SOURCE_NODE_ID and jiraissue.id = jiraissue.id), labels =  (select STRING_AGG( ISNULL(label.label, ''), ',') from label where label.issue = jiraissue.id), sprint_name = (select STRING_AGG( ISNULL(s.name, ''), ',') FROM customfieldvalue cfv INNER JOIN AO_60DB71_SPRINT s ON CAST(s.ID AS VARCHAR(10)) = CAST(cfv.stringvalue AS VARCHAR(10)) INNER JOIN jiraissue i ON i.id = cfv.issue where i.ID = jiraissue.ID), start_date = (select customfieldvalue.DATEVALUE from customfieldvalue where customfieldvalue.issue = jiraissue.id and CUSTOMFIELD = 10303), end_date = (select customfieldvalue.DATEVALUE from customfieldvalue where customfieldvalue.issue = jiraissue.id and CUSTOMFIELD = 10304), worklog = (select STRING_AGG(ISNULL(convert(nvarchar(max), CONCAT('Le ',STARTDATE,', ', author, ' a travaillé ', cast((timeworked/60)/60 as decimal(9,2)), 'h.')), ''), ';') from worklog where worklog.issueid = jiraissue.id), total_work_time = (select cast(sum((timeworked/60)/60) as decimal(9,2)) from worklog where worklog.issueid = jiraissue.id), jira_link = CONCAT(project.pkey, '-', jiraissue.issuenum), worklog2 = (select STRING_AGG(convert(nvarchar(max), ISNULL(CONCAT(CONVERT(VARCHAR, STARTDATE, 20), '~', author, '~', timeworked), ';')), ';') from worklog inner join cwd_user on user_name= AUTHOR where worklog.issueid = jiraissue.id), projectCategory = (select pc.cname from projectcategory pc left join nodeassociation na on project.id=na.source_node_id and na.sink_node_entity='ProjectCategory' where na.sink_node_id = pc.id), service = (select cfo.customvalue from customfieldoption cfo join customfield cf on cf.ID = 11100 inner join customfieldvalue cfv on cfv.STRINGVALUE = CAST(cfo.id AS NVARCHAR) and cfv.issue = jiraissue.id where cfo.customfield = 11100), jiraissue.TIMEORIGINALESTIMATE,  issue_linked_child = (select STRING_AGG( ISNULL(CONCAT(p.pkey, '-', j.issuenum), ''), ';') from jiraissue j , project p inner join  issuelink on DESTINATION = jiraissue.id where j.id = SOURCE and p.id = j.PROJECT), issue_linked_parent = (select STRING_AGG( ISNULL(CONCAT(p.pkey, '-', j.issuenum), ''), ';') from jiraissue j , project p inner join  issuelink on SOURCE = jiraissue.id where j.id = DESTINATION and p.id = j.PROJECT), azure_link = (select customfieldvalue.STRINGVALUE from customfieldvalue where customfieldvalue.issue = jiraissue.id and CUSTOMFIELD = 11900) FROM jiraissue, project, issuetype, issuestatus, priority WHERE jiraissue.priority=priority.ID and issuestatus.ID=jiraissue.issuestatus and issuetype.id=jiraissue.issuetype and project.id=jiraissue.project and issuetype != 10800 and not (project.id = 10000 or project.id= 13301) and  jiraissue.updated > DATEADD(hour, -1, GETDATE()) and jiraissue.issuestatus = 10009 order by UPDATED desc;");
            var listTicket = sql.getListOfTicket();
            foreach (JiraEntity ticket in listTicket)
            {
                ticket.doMethods();
                ticket.printEntity();
                sql.query = $"SELECT i.id, DATEDIFF(day, MIN(CASE WHEN ci.NEWVALUE like '10002' THEN cg.created END), MAX(CASE WHEN ci.NEWVALUE like '10009' THEN cg.created END)) as number_of_days FROM changegroup cg inner join jiraissue i on cg.issueid = i.id inner join changeitem ci on ci.groupid = cg.id AND ci.FIELD='status' where i.id = {ticket.issueNb} AND (ci.NEWVALUE like '10002' OR ci.NEWVALUE like'10009') GROUP BY i.id;";
                int nbOFDays = sql.getNbDaysKO();
                if ( nbOFDays <= 15)
                {
                    //VERIFIER AVEC RENAUD SI TICKET SERA TOUJOURS TOUJOURS DEJA PRESENT DANS AZURE OU PAS ?????
                    //Si inférieur ou égal à 15 jours, je modifie le ticket au status BACK
                    if (!String.IsNullOrEmpty(ticket.azureLink))
                    {
                        string azureID = ticket.azureLink.Split('/').Last();
                        var patch = new PatchToAzure();
                        patch.json = "[{\"op\": \"add\", \"path\": \"/fields/System.State\", \"value\": \"Back - Test Failed\" }]";
                        patch.url = $"https://dev.azure.com/IRIUMSOFTWARE/_apis/wit/workitems/{azureID}?api-version=7.0";
                        patch.patchingToAzure();
                        GetLastCommentAndPostToAzure(ticket, azureID);
                    }
                } else
                {
                    //Si supérieur à 15jours, je passe le ticket Jira à  Demande KO
                    var update = new UpdateSQL($"UPDATE jiraissue SET issuestatus = 11900 WHERE jiraissue.id={ticket.issueNb}");
                    update.UpdateRow();
                }
            }
        }

        public void SyncAzure_Done()
        {
            var post = new PostToAzure();
            post.url = "https://dev.azure.com/IRIUMSOFTWARE/_apis/wit/wiql?api-version=7.1-preview.2";
            post.contentType = "application/json";

            // Je récupère les PBI dont estimate TRUE, status DONE, JiraLink présent, dont le status à changé dans la journée.
            post.json = "{\"query\": \"SELECT [System.Id] FROM WorkItems WHERE [Custom.Toestimate] = 'true' AND [System.State] = 'Done' AND [Microsoft.VSTS.Common.StateChangeDate] >= @today AND [Custom.jiraLink] != '' \"}";
            var result = post.postingToAzure();
            foreach (dynamic item in result.workItems)
            {
                var get = new GetAzure();
                get.url = item.url;
                var pbi = get.GettingFromAzure();
                var sql = new UpdateSQL($"UPDATE jiraissue SET issuestatus = 10001 WHERE jiraissue.id= (Select jiraissue.id from jiraissue inner join customfieldvalue cfv on cfv.customfield = 11900 and STRINGVALUE = 'https://dev.azure.com/IRIUMSOFTWARE/_workitems/edit/{pbi.id}' and cfv.ISSUE = jiraissue.id);");
                sql.UpdateRow();

                //Add last comment
                sql.query = $"select issue from customfieldvalue where CUSTOMFIELD = 11900 and stringvalue like '%/{pbi.id.ToString()}';";
                string jiraID = sql.getJiraID();
                var addLastComment = new GetAndPostComments();
                addLastComment.getAndPostLastCommentFromAzureToJira(pbi.id.ToString(), pbi.fields["System.TeamProject"].ToString(), jiraID);
            }

            // Je récupère les PBI dont estimate FALSE, status DONE, JiraLink présent, dont le status à changé dans la journée.
            post.json = "{\"query\": \"SELECT [System.Id] FROM WorkItems WHERE [Custom.Toestimate] = 'false' AND [System.State] = 'Done' AND [Microsoft.VSTS.Common.StateChangeDate] >= @today AND [Custom.jiraLink] != '' \"}";
            result = post.postingToAzure();
            foreach (dynamic item in result.workItems)
            {
                var get = new GetAzure();
                get.url = item.url;
                var pbi = get.GettingFromAzure();
                var sql = new UpdateSQL($"UPDATE jiraissue SET issuestatus = 10002 WHERE jiraissue.id= (Select jiraissue.id from jiraissue inner join customfieldvalue cfv on cfv.customfield = 11900 and STRINGVALUE = 'https://dev.azure.com/IRIUMSOFTWARE/_workitems/edit/{pbi.id}' and cfv.ISSUE = jiraissue.id);");
                sql.UpdateRow();

                //Add last comment
                sql.query = $"select issue from customfieldvalue where CUSTOMFIELD = 11900 and stringvalue like '%/{pbi.id.ToString()}';";
                string jiraID = sql.getJiraID();
                var addLastComment = new GetAndPostComments();
                addLastComment.getAndPostLastCommentFromAzureToJira(pbi.id.ToString(), pbi.fields["System.TeamProject"].ToString(), jiraID);
            }
        }

        public void SyncAzure_Removed()
        {
            var post = new PostToAzure();
            post.url = "https://dev.azure.com/IRIUMSOFTWARE/_apis/wit/wiql?api-version=7.1-preview.2";
            post.contentType = "application/json";

            // Je récupère les PBI dont status REMOVED, JiraLink présent, dont le status à changé dans la journée.
            post.json = "{\"query\": \"SELECT [System.Id] FROM WorkItems WHERE [System.State] = 'Removed' AND [Microsoft.VSTS.Common.StateChangeDate] >= @today AND [Custom.jiraLink] != '' \"}";
            var result = post.postingToAzure();
            foreach (dynamic item in result.workItems)
            {
                var get = new GetAzure();
                get.url = item.url;
                var pbi = get.GettingFromAzure();
                var sql = new UpdateSQL($"UPDATE jiraissue SET issuestatus = 10007 WHERE jiraissue.id= (Select jiraissue.id from jiraissue inner join customfieldvalue cfv on cfv.customfield = 11900 and STRINGVALUE = 'https://dev.azure.com/IRIUMSOFTWARE/_workitems/edit/{pbi.id}' and cfv.ISSUE = jiraissue.id);");
                sql.UpdateRow();

                //Add last comment
                sql.query = $"select issue from customfieldvalue where CUSTOMFIELD = 11900 and stringvalue like '%/{pbi.id}';";
                string jiraID = sql.getJiraID();
                var addLastComment = new GetAndPostComments();
                addLastComment.getAndPostLastCommentFromAzureToJira(pbi.id.ToString(), pbi.fields["System.TeamProject"].ToString(), jiraID);
            }
        }


        public void SyncAzure_Sprint()
        {
            // Recuperation du sprint actuel par projet
            // -- Get les ticket du sprint
            // -- filtrer les tasks et pbi
            // -- si pbi, get sur jira
            // -- si la date de start ou end est != alors j'update

            var projects = new List<string>{"Locpro", "Mobility", "Digital"};
            foreach (string project in projects)
            {
                // Recuperation du sprint actuel du projet
                var get = new GetAzure();
                get.url = $"https://dev.azure.com/IRIUMSOFTWARE/{project}/_apis/work/teamsettings/iterations?$timeframe=current&api-version=7.0";
                var result = get.GettingFromAzure();
                try
                {
                    string sprintID = result.value[0].id;
                    string startDate = result.value[0].attributes.startDate;
                    string finishDate = result.value[0].attributes.finishDate;
                    // Recuperation des tickets des works items de l'itération en cours
                    get.url = result.value[0].url + "/workitems?api-version=7.0";
                    result = get.GettingFromAzure();
                    foreach (var item in result.workItemRelations)
                    {
                        if (item.rel == null)
                        {
                            string ticketID = item.target.id;
                            var sql = new GetSQL($"Select jiraissue.id, start_date = (select customfieldvalue.DATEVALUE from customfieldvalue where customfieldvalue.issue = jiraissue.id and CUSTOMFIELD = 10303), end_date = (select customfieldvalue.DATEVALUE from customfieldvalue where customfieldvalue.issue = jiraissue.id and CUSTOMFIELD = 10304) from jiraissue WHERE jiraissue.id= (Select jiraissue.id from jiraissue inner join customfieldvalue cfv on cfv.customfield = 11900 and STRINGVALUE = 'https://dev.azure.com/IRIUMSOFTWARE/_workitems/edit/{ticketID}' and cfv.ISSUE = jiraissue.id);");
                            result = sql.getDate();

                            if (String.IsNullOrEmpty(result[1]) || DateTime.ParseExact(startDate, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture) != DateTime.ParseExact(result[1], "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture))
                            {
                                if (!String.IsNullOrEmpty(result[1]))
                                {
                                    var update = new UpdateSQL($"UPDATE customfieldvalue SET DATEVALUE = '{DateTime.ParseExact(startDate, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture)}' WHERE ISSUE = {result[0]} AND CUSTOMFIELD = 10303");
                                    update.UpdateRow();
                                }
                            }
                            if (String.IsNullOrEmpty(result[2]) || DateTime.ParseExact(finishDate, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture) != DateTime.ParseExact(result[2], "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture))
                            {
                                if (!String.IsNullOrEmpty(result[2]))
                                {
                                    var update = new UpdateSQL($"UPDATE customfieldvalue SET DATEVALUE = '{DateTime.ParseExact(finishDate, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture)}' WHERE ISSUE = {result[0]} AND CUSTOMFIELD = 10304");
                                    update.UpdateRow();
                                }
                            }
                        }
                    }
                }
                catch (RuntimeBinderException)
                {
                    Globals.Logger.Error("Error: Property 'value' does not exist in the dynamic object.");
                }
            }
        }

        public void SyncAzure_ClosedSprint()
        {
            // Je vais get les itérations
            // Je récupère la dernière itération "passé"
            // Je vais get les tickets associés par leur ID,
            // Si ils sont de type Task, non Done et non closed sprint
            // Alors je duplique
            // Puis je PATCH pour changer l'état du ticket sprint finished
            var projects = new List<string> {"Locpro", "Mobility", "Digital"};
            foreach (string project in projects)
            {
                // Recuperation du sprint actuel du projet
                var get = new GetAzure();
                get.url = $"https://dev.azure.com/IRIUMSOFTWARE/{project}/_apis/work/teamsettings/iterations?api-version=7.0";
                var result = get.GettingFromAzure();
                try
                {
                    DateTime mostRecentFinishDate = DateTime.MinValue;
                    string recentIterationId = string.Empty;

                    foreach (var val in result.value)
                    {
                        if (val.attributes.timeFrame == "past" && val.attributes.finishDate > mostRecentFinishDate)
                        {
                            mostRecentFinishDate = val.attributes.finishDate;
                            recentIterationId = val.id;
                        }
                    }

                    get.url = $"https://dev.azure.com/IRIUMSOFTWARE/{project}/_apis/work/teamsettings/iterations/{recentIterationId}/workitems?api-version=7.0";
                    result = get.GettingFromAzure();
                    try
                    {
                        foreach(var ticket in result.workItemRelations)
                        {
                            get.url = ticket.target.url;
                            result = get.GettingFromAzure();
                            if (result.fields["System.WorkItemType"] == "Task" && result.fields["System.State"] != "Done" && result.fields["System.State"] != "Closed Sprint")
                            {
                                var post = new PostToAzure();
                                post.url = $"https://dev.azure.com/IRIUMSOFTWARE/{project}/_apis/wit/workitems/$Task?api-version=7.0";
                                var json = new CreateJsonBody();
                                json.ticket = result;
                                post.json = json.createJsonWithPBIToPostFromDynamic();
                                var newResult = post.postingToAzure();

                                //Patch
                                var patch = new PatchToAzure();
                                patch.url = $"https://dev.azure.com/IRIUMSOFTWARE/{project}/_apis/wit/workitems/{result.id}?api-version=7.0";
                                patch.json = "[{\"op\": \"add\", \"path\": \"/fields/System.State\", \"value\": \"Closed Sprint\" }]";
                                patch.patchingToAzure();

                                //Get & Patch relation hierarchy
                                get.url = $"https://dev.azure.com/IRIUMSOFTWARE/_apis/wit/workitems/{result.id}?$expand=all&api-version=6.0";
                                result = get.GettingFromAzure();
                                foreach (var relation in result.relations)
                                {
                                    patch.url = $"https://dev.azure.com/IRIUMSOFTWARE/{project}/_apis/wit/workitems/{newResult.id}?api-version=7.0";
                                    patch.json = "[{\"op\": \"add\", \"path\": \"/relations/-\", \"value\": {\"rel\": \""+ relation.rel +"\",  \"url\": \" " + relation.url +" \"}}]";
                                    patch.patchingToAzure();
                                }

                                //Get comment and duplicate to new post
                                var getAndPostCom = new GetAndPostComments();
                                getAndPostCom.getAndPostCommentFromAzureToAzure(result.id.ToString(), project, newResult.id.ToString());

                                //Get attachment and duplicate to new post
                                patchPBIWithAttachmentFromAzure(project, result.id.ToString(), newResult.id.ToString());
                            }
                        }
                    }
                    catch (RuntimeBinderException)
                    {
                        Globals.Logger.Error("Error: Property 'workItemRelations' does not exist in the dynamic object 'result'.");
                    }
                }
                catch (RuntimeBinderException)
                {
                    Globals.Logger.Error("Error: Property 'value' does not exist in the dynamic object.");
                }
            }
        }



        public void GetLastCommentAndPostToAzure(JiraEntity entity, string azureID)
        {
            var sql = new GetSQL($"SELECT top 1 lower_user_name as username, jiraaction.actionbody, jiraaction.created FROM app_user JOIN jiraaction ON jiraaction.author = app_user.user_key JOIN jiraissue ON jiraaction.issueid = jiraissue.id WHERE jiraissue.id = {entity.issueNb} ORDER BY jiraaction.created DESC");
            string comment = sql.getLastComment();
            if (!string.IsNullOrEmpty(comment))
            {
                PostCommentToAzure(comment, entity, azureID);
            }
        }

        public void PostCommentToAzure(string comment, JiraEntity entity, string azureID)
        {
            var post = new PostToAzure();
            post.url = $"https://dev.azure.com/IRIUMSOFTWARE/{entity.azureProject}/_apis/wit/workItems/{azureID}/comments?api-version=7.1-preview.3";
            post.contentType = "application/json";
            post.json = "{\"text\": \" " + comment + " \"}";
            post.postingToAzure();
        }

        public void patchPBIWithAttachmentFromJira(List<string> attachments, string project, string azureID, string description)
        {
            foreach( var att in attachments)
            {
                PostAttachment pj = new PostAttachment();
                var azure_link = pj.PostAttachmentToAzureServer(att, project);
                string filename = azure_link.Split('=').Last();
                description = description.Replace(filename, $"<img alt='img_url' src='{azure_link}' >");
                var patchAtt = new PatchToAzure();
                patchAtt.url = $"https://dev.azure.com/IRIUMSOFTWARE/{project}/_apis/wit/workitems/{azureID}?api-version=7.0";
                patchAtt.json = "[{\"op\": \"add\", \"path\": \"/relations/-\", \"value\": { \"rel\": \"AttachedFile\", \"url\": \"" + azure_link + "\", \"attributes\": {\"comment\": \"Spec for the work\"}}}, {\"op\": \"add\", \"path\": \"/fields/System.Description\", \"value\": \"" + description + "\" } ]";
                patchAtt.patchingToAzure();
            }
        }

        public void patchPBIWithAttachmentFromAzure(string project, string azureID, string azureIDToPatch)
        {
            var get = new GetAzure();
            get.url = $"https://dev.azure.com/IRIUMSOFTWARE/_apis/wit/workItems/{azureID}?$expand=all&api-version=6.0";
            var result = get.GettingFromAzure();
            foreach (var link in result.relations)
            {
                if (link.rel == "AttachedFile")
                {
                    var url = link.url;
                    var patch = new PatchToAzure();
                    patch.url = $"https://dev.azure.com/IRIUMSOFTWARE/{project}/_apis/wit/workitems/{azureIDToPatch}?api-version=7.0";
                    patch.json = "[{\"op\": \"add\", \"path\": \"/relations/-\", \"value\": { \"rel\": \"AttachedFile\", \"url\": \"" + url + "\", \"attributes\": {\"comment\": \"Spec for the work\"}}}]";
                    patch.patchingToAzure();
                }
            }
        }

    }



    public class MyJobListener : IJobListener
    {
        public string Name => "MyJobListener";

        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            // This method is called before a job is executed. 
            // You can put here some code to handle job execution failures.
            return Task.CompletedTask;
        }

        public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            // This method is called if a job execution is vetoed by a trigger listener.
            return Task.CompletedTask;
        }

        public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
        {
            // This method is called after a job has been executed.
            // If the job has failed, you can put here some code to restart it automatically.
            if (jobException != null)
            {
                // Restart the job
                var scheduler = context.Scheduler;
                var jobKey = context.JobDetail.Key;
                var triggerKey = context.Trigger.Key;

                // Remove the failed job from the scheduler
                scheduler.DeleteJob(jobKey);

                // Create a new trigger to restart the job after a specified delay
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey.Name, triggerKey.Group)
                    .StartAt(DateTimeOffset.UtcNow.AddSeconds(10))
                    .Build();

                // Schedule the job with the new trigger
                scheduler.ScheduleJob(context.JobDetail, trigger);
            }

            return Task.CompletedTask;
        }
    }
}
