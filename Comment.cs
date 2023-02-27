using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CronSynchroJiraAzure
{
    public class Comment
    {
        public string text;
        public string user;
        public DateTime date;

        public Comment(string text, string user, DateTime date)
        {
            this.text = text;
            this.user = user;
            this.date = date;
        }
    }
}
