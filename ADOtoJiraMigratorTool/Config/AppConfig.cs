namespace ADOtoJiraMigratorTool.Config {
    public class AppConfig {
        public AzureDevOpsConfig AzureDevOpsConfig { get; set; } = new AzureDevOpsConfig();
        public JiraConfig JiraConfig { get; set; } = new JiraConfig();

        public List<FieldMapConfig> FieldMapConfig { get; set; } = new List<FieldMapConfig>();
    }
}