namespace ADOtoJiraMigratorTool.Config {
    public class JiraConfig {
        public string BaseUrl { get; set; } = "";
        public string Username { get; set; } = "";
        public string APIToken { get; set; } = "";
        public string ProjectId { get; set; } = "";

        public int StoryId { get; set; } = -1;
        public int BugId { get; set; } = -1;

        public string ADOTicketField { get; set; } = "";
    }
}