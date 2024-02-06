using ADOtoJiraMigratorTool.Config;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using ReverseMarkdown;

namespace ADOtoJiraMigratorTool {
    public static class Utils {
        private static Converter htmlConverter;

        static Utils() {
            htmlConverter = new Converter(new ReverseMarkdown.Config() {
                UnknownTags = ReverseMarkdown.Config.UnknownTagsOption.Bypass
            });
        }

        public static string ADO_URL = "https://{0}.visualstudio.com/{1}/_workitems/edit/{2}";

        public static Dictionary<string, object> MapWorkItemToJira(WorkItem item, AppConfig config) {
            if (item.Fields == null) return new Dictionary<string, object>();

            string issueType = item.Fields["System.WorkItemType"].ToString() == "User Story" ? config.JiraConfig.StoryId.ToString() : config.JiraConfig.BugId.ToString();

            /*
                Map fields using JSON config instead of hard coded nonsense

                This works kind of, but assumes all of the data is in the field object of the ADO WorkItem
                which may or may not be the case, we should use the transform data object to standardize as
                much data as possible in a single dictionary.

                Also this needs to be tested.
            */
            /*
            // Build our mapped object using json config
            Dictionary<string, object> ret = new Dictionary<string, object>();
            foreach (FieldMapConfig field in config.FieldMapConfig) {
                ret.Add(
                    field.JiraField,
                    field.AdoField != null ? item.Fields.GetValueOrDefault(field.AdoField, field.DefaultValue) : null!
                );
            }

            // These are required no matter what!
            // TOOD: Should we include summary/title?
            if (!ret.ContainsKey("project")) {
                ret.Add("project", config.JiraConfig.ProjectId);
            }

            if (!ret.ContainsKey("issuetype")) {
                ret.Add("issuetype", issueType);
            }
            */

            // TODO: This sucks, move field maps to JSON
            return new Dictionary<string, object>() {
                // This tells Jira what project to put this issue in
                {
                    "project",
                    new { id = config.JiraConfig.ProjectId }
                },
                {
                    "issuetype",
                    new { id = issueType }
                },
                // TODO: Figure out user assignments in Jira
                {
                    "assignee",
                    null!
                },
                {
                    "summary",
                    item.Fields.GetValueOrDefault("System.Title", "N/A")
                },
                {
                    "description",
                    htmlConverter.Convert(item.Fields.GetValueOrDefault("System.Description", "N/A").ToString()).Truncate(short.MaxValue)
                },
                {
                    "labels",
                    (item.Fields.GetValueOrDefault("System.Tags", "").ToString() ?? "").Split(";").Select(e => e.Trim().Replace(' ', '_')).ToArray()
                },
                // Save ADO Url in Jira
                {
                    config.JiraConfig.ProjectId,
                    string.Format(ADO_URL, config.AzureDevOpsConfig.Organization, config.AzureDevOpsConfig.Project, (item.Id ?? -1)).ToLower()
                },
                {
                    "customfield_12148",
                    item.Fields.GetValueOrDefault("Custom.AngularPlanningHours", "0")
                },
                {
                    "customfield_12149",
                    Convert.ToInt32(item.Fields.GetValueOrDefault("Custom.SQLPlanningHours", "0")) + Convert.ToInt32(item.Fields.GetValueOrDefault("Custom.WebPlanningHours", "0"))
                },
                {
                    "customfield_13198",
                    Convert.ToInt32(item.Fields.GetValueOrDefault("Custom.QAHours", "0"))
                },
                //{
                //    "Billable Hours",
                //    Convert.ToInt32(item.Fields.GetValueOrDefault("TranscendentAgile.BillableHours", "0"))
                //},
            };
        }

        public static string Truncate(this string value, int maxLength, string suffix = "...") {
            return value.Length > maxLength ? value.Substring(0, maxLength - 3) + suffix : value;
        }
    }
}