using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ADOtoJiraMigratorTool {
    public class JiraIssue {
        public string Id { get; set; } = "";
        public string Self { get; set; } = "";
        public string Key { get; set; } = "";

        public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();

        public static string ADOKey = "customfield_13219";
    }

    public class JiraIssueCreateUpdate {
        [JsonPropertyName("fields")]
        public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();
    }

    public class JiraIssueSearchResults {
        public int Total { get; set; }

        public List<JiraIssue> Issues { get; set; } = new List<JiraIssue>();
    }

    public class JiraBulkCreateResponse {
        public List<object> Issues { get; set; } = new List<object>();
        public List<object> Errors { get; set; } = new List<object>();
    }
}