using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using ReverseMarkdown;

namespace ADOtoJiraMigratorTool {
    public static class Utils {
        private static Converter htmlConverter;

        static Utils() {
            htmlConverter = new Converter(new ReverseMarkdown.Config() {
                UnknownTags = Config.UnknownTagsOption.Bypass
            });
        }

        public static string ADO_URL = "https://{0}.visualstudio.com/{1}/_workitems/edit/{2}";

        public static Dictionary<string, object> MapWorkItemToJira(WorkItem item, IConfiguration config) {
            if (item.Fields == null) return new Dictionary<string, object>();

            string issueType = item.Fields["System.WorkItemType"].ToString() == "User Story" ? config["JiraConfig:StoryId"] : config["JiraConfig:BugId"];

            return new Dictionary<string, object>() {
                { "assignee", null! },
                { "summary", item.Fields.GetValueOrDefault("System.Title", "N/A") },
                { "issuetype", new { id = issueType } },
                { "labels", (item.Fields.GetValueOrDefault("System.Tags", "").ToString() ?? "").Split(";").Select(e => e.Trim().Replace(' ', '_')).ToArray() },
                { "project", new { id = config["JiraConfig:ProjectId"]} },
                { "description", htmlConverter.Convert(item.Fields.GetValueOrDefault("System.Description", "N/A").ToString()).Truncate(short.MaxValue) },
                { config["JiraConfig:ADOTicketField"], string.Format(ADO_URL, config["AzureDevOpsConfig:Organization"], config["AzureDevOpsConfig:Project"], (item.Id ?? -1)).ToLower() },
            };
        }

        public static string Truncate(this string value, int maxLength, string suffix = "...") {
            return value.Length > maxLength ? value.Substring(0, maxLength - 3) + suffix : value;
        }
    }
}