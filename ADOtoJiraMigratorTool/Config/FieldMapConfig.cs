namespace ADOtoJiraMigratorTool.Config {
    public class FieldMapConfig {
        public string JiraField { get; set; } = "";
        public string AdoField { get; set; } = "";
        public string DefaultValue { get; set; } = "";

        public FieldMapMetadata? Meta { get; set; } = null;
    }
}