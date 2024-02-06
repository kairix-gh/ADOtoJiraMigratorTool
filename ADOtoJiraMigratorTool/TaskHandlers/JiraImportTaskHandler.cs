using ADOtoJiraMigratorTool.Config;
using Flurl;
using Flurl.Http;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Spectre.Console;

namespace ADOtoJiraMigratorTool.TaskHandlers {
    public class JiraImportTaskHandler : TaskHandler {
        public JiraImportTaskHandler(ProgressTask task) : base(task) {
        }

        public JiraImportTaskHandler(ProgressTask task, AppConfig config) : base(task, config) {
        }

        public override async Task DoWork() {
            if (Input == null || Config == null) {
                ProgressTask.Increment(100);
                return;
            }

            List<WorkItem> data = (Input as List<WorkItem>) ?? new List<WorkItem>();
            ProgressTask.MaxValue = data.Count;

            // Max allowed by Jira
            int batchSize = 50;
            int totalItems = data.Count;

            int[] adoIdsToProcess = data.Select(e => e.Id ?? 0).ToArray();

            for (int i = 0; i < totalItems; i += batchSize) {
                int remainingItems = totalItems - i;
                int batchCount = Math.Min(batchSize, remainingItems);
                int[] batchIds = new int[batchCount];
                Array.Copy(adoIdsToProcess, i, batchIds, 0, batchCount);

                // Get items that exist in Jira, convert to URL since that's what is in Jira
                var batchAdoUrls = batchIds.Select(e => "'" + string.Format(Utils.ADO_URL, Config.AzureDevOpsConfig.Organization, Config.AzureDevOpsConfig.Project, e) + "'").ToArray();
                var query = (Config.JiraConfig.BaseUrl + "/rest/api/2/search").SetQueryParams(new {
                    jql = $"project = {Config.JiraConfig.ProjectId} AND cf[{Config.JiraConfig.ADOTicketField.Split('_')[1]}] IN (" + string.Join(",", batchAdoUrls) + ")".ToLower()
                });

                // Query Jira API to find items where our custom field has the ADO URL in it
                // These are the items we need to update and not create, or we will make dupes
                JiraIssueSearchResults? searchResults = null;
                try {
                    searchResults = await query.WithBasicAuth(Config.JiraConfig.Username, Config.JiraConfig.APIToken).GetJsonAsync<JiraIssueSearchResults>();
                } catch (FlurlHttpException ex) {
                    var error = await ex.GetResponseStringAsync();
                    AnsiConsole.MarkupLine("[bold red]{0}[/]", error.EscapeMarkup());
                } catch (Exception ex) {
                    AnsiConsole.WriteException(ex);
                }

                // Transform Jira results into kvpairs of Jira IDs and ADO #s
                Dictionary<int, string> idsToUpdate = new Dictionary<int, string>();
                if (searchResults != null && searchResults.Total > 0) {
                    idsToUpdate = searchResults.Issues.ToDictionary(
                        result => {
                            return Convert.ToInt32((result.Fields[Config.JiraConfig.ADOTicketField].ToString() ?? "").Split('/').Last());
                        },
                        result => {
                            return result.Id;
                        }
                    );
                }
                List<int> idsToCreate = batchIds.Except(idsToUpdate.Keys).ToList();

                // Iterate each id to update and call the Jira API to update one at a time, there is bulk update
                foreach (KeyValuePair<int, string> kv in idsToUpdate) {
                    WorkItem? bli = data.FirstOrDefault(x => x.Id == kv.Key);
                    if (bli == null) continue;

                    Dictionary<string, object> mappedFields = Utils.MapWorkItemToJira(bli, Config);
                    mappedFields.Remove("issuetype");

                    JiraIssueCreateUpdate body = new JiraIssueCreateUpdate() {
                        Fields = mappedFields
                    };

                    try {
                        // Returns 201 with no body
                        await (Config.JiraConfig.BaseUrl + "/rest/api/2/issue/" + kv.Value)
                            .WithBasicAuth(Config.JiraConfig.Username, Config.JiraConfig.APIToken)
                            .PutJsonAsync(body);
                    } catch (FlurlHttpException ex) {
                        var error = await ex.GetResponseStringAsync();
                        AnsiConsole.MarkupLine("[bold red]Failed Updating Ticket (Jira: {0}, ADO: {1})[/]", kv.Value, kv.Key);
                        AnsiConsole.MarkupLine("[red]{0}[/]", error.EscapeMarkup());
                    } catch (Exception ex) {
                        AnsiConsole.WriteException(ex);
                    }

                    ProgressTask.Increment(1);
                }

                // Bulk Create
                List<JiraIssueCreateUpdate> IssueUpdates = new List<JiraIssueCreateUpdate>();
                foreach (int id in idsToCreate) {
                    WorkItem? bli = data.FirstOrDefault(x => x.Id == id);
                    if (bli == null) continue;

                    Dictionary<string, object> mappedFields = Utils.MapWorkItemToJira(bli, Config);

                    IssueUpdates.Add(new JiraIssueCreateUpdate() {
                        Fields = mappedFields
                    });
                }

                if (IssueUpdates.Count <= 0) continue;

                try {
                    var response = await (Config.JiraConfig.BaseUrl + "/rest/api/2/issue/bulk")
                        .WithBasicAuth(Config.JiraConfig.Username, Config.JiraConfig.APIToken)
                        .PostJsonAsync(new { issueUpdates = IssueUpdates })
                        .ReceiveJson<JiraBulkCreateResponse>();
                } catch (FlurlHttpException ex) {
                    var error = await ex.GetResponseStringAsync();
                    AnsiConsole.MarkupLine("[bold red]Bulk Create Failed for one or more of these ids: {0}.[/]", string.Join(", ", idsToCreate).EscapeMarkup());
                    AnsiConsole.MarkupLine("[bold red]Jira API Response Below:[/]");
                    AnsiConsole.MarkupLine("[red]{0}[/]", error.EscapeMarkup());
                } catch (Exception ex) {
                    AnsiConsole.WriteException(ex);
                }

                ProgressTask.Increment(IssueUpdates.Count);
                IssueUpdates.Clear();
            }

            // Ensure no infinite loops!
            ProgressTask.Value = ProgressTask.MaxValue;
        }
    }
}