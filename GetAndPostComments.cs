using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;

namespace CronSynchroJiraAzure
{
    public class GetAndPostComments
    {
        public List<Comment> listComments;
        public GetAndPostComments() { }

        public void getAndPostCommentFromSQL(string azureID, string project, string jiraID)
        {
            var sql = new GetSQL($"select id, actionbody, author, created from jiraaction where issueid = {jiraID} order by created desc ; ");
            var result = sql.getComment();
            if (result.Count > 0)
            {
                foreach( var c in result)
                {
                    var post = new PostToAzure();
                    post.url = $"https://dev.azure.com/IRIUMSOFTWARE/{project}/_apis/wit/workItems/{azureID}/comments?api-version=7.0-preview.3";
                    post.json = createJsonWithCommentToPost(c);
                    post.postingToAzure();
                }
            }
        }

        public void getAndPostCommentFromAzure(string azureID, string project, string jiraID)
        {
            var get = new GetAzure();
            get.url = $"https://dev.azure.com/IRIUMSOFTWARE/{project}/_apis/wit/workItems/{azureID}/comments?api-version=7.0-preview.3";
            var result = get.GettingFromAzure();
            if (result.comments > 0)
            {
                foreach (var c in result.comments)
                {
                    this.listComments.Add(new Comment(c.text, c.createdBy.uniqueName, c.CreatedDate));
                    var sql = new UpdateSQL($"insert into jiraaction (id, issueid, author, actionbody, actiontype, created) Select MAX(id)+1, {jiraID}, '{c.uniqueName}', '{c.text}', 'comment', GETDATE() from jiraaction;");
                    sql.UpdateRow();
                }
            }
        }

        public void getAndPostCommentFromAzureToAzure(string azureID, string project, string azureIDtoPost)
        {
            var get = new GetAzure();
            get.url = $"https://dev.azure.com/IRIUMSOFTWARE/{project}/_apis/wit/workItems/{azureID}/comments?api-version=7.0-preview.3";
            var result = get.GettingFromAzure();
            if (result.comments > 0)
            {
                foreach (var c in result.comments)
                {
                    var post = new PostToAzure();
                    post.url = $"https://dev.azure.com/IRIUMSOFTWARE/{project}/_apis/wit/workItems/{azureIDtoPost}/comments?api-version=7.0-preview.3";
                    post.json = createJsonWithCommentToPost(new Comment(c.text, c.createdBy.uniqueName, c.CreatedDate));
                    post.postingToAzure();
                }
            }
        }

        public string createJsonWithCommentToPost(Comment comment)
        {
            string c = String.Join("<br><br>", $"<h2><strong>Ecrit par {comment.user}</strong></h2> <h4>Le {comment.date}</h4>", comment.text);
            string jsonToPost = "{ \"text\": \"" + c + "\"}";
            return jsonToPost;
        }
    }
}
