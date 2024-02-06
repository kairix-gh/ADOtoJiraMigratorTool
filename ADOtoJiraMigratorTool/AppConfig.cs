using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADOtoJiraMigratorTool {
    internal class AppConfig {
        public AzureDevOpsConfig AzureDevOps { get; set; } = new AzureDevOpsConfig();
    }

    internal class AzureDevOpsConfig {
        public string Organization { get; set; } = "";
        public string Project { get; set; } = "";
        public string AreaPath { get; set; } = "";
        public string Team { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string WorkTypes { get; set; } = "";
    }

    internal class JiraConfig {
        public string BaseUrl { get; set; } = "";
        public string Username { get; set; } = "";
        public string APIToken { get; set; } = "";
        public string ProjectId { get; set; } = "";

        public int StoryId { get; set; } = -1;
        public int BugId { get; set; } = -1;

        public string ADOTicketField { get; set; } = "";
    }
}