using ADOtoJiraMigratorTool.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace ADOtoJiraMigratorTool
{
    public class AzureDevOps {
        public static async Task<IEnumerable<WorkItem>> GetBacklogItemsForDataDump(AppConfig config, int startNum = 0, int pageSize = 0, string[]? states = null, string[]? fields = null) {
            // WIQL query format string used by ADO internally. Note that ADO API will only return
            // id's via WIQL, a second request is needed to get the actual field data, adding them
            // here does nothinggg
            string WIQL_FORMAT =
                @"SELECT
                    [System.Id]
                FROM workitems
                WHERE
                    [System.TeamProject] = '{0}' AND
                    [System.AreaPath] = '{1}' AND
                    [System.WorkItemType] In {2} AND
                    [System.State] Not In ('Removed', 'Closed')";

            if (startNum > 0) {
                WIQL_FORMAT += $" AND [Microsoft.VSTS.Common.StackRank] > {startNum}";
            }
            WIQL_FORMAT += "ORDER BY [Microsoft.VSTS.Common.StackRank] ASC";

            string[]? VALID_FIELDS = fields;

            string[] workTypes = config.AzureDevOpsConfig.WorkTypes.Split(",");
            string workItemTypeList = "(" + string.Join(",", workTypes.Select(e => $"'{e}'")) + ")";

            Wiql wiql = new Wiql();

            var creds = new VssBasicCredential(config.AzureDevOpsConfig.AccessToken ?? "", "");
            var conn = new VssConnection(new Uri($"https://dev.azure.com/{config.AzureDevOpsConfig.Organization}"), creds);
            var client = await conn.GetClientAsync<WorkItemTrackingHttpClient>();

            string query = string.Format(WIQL_FORMAT, config.AzureDevOpsConfig.Project, config.AzureDevOpsConfig.AreaPath, workItemTypeList);
            (int[] workIds, DateTime queryAsOf) = await GetWorkItemIDs(query, wiql, client, pageSize);

            // Now, we can get work item details
            List<WorkItem> workItems = await BatchGetWorkItems(workIds, client, queryAsOf, VALID_FIELDS);

            return workItems;
        }

        private static async Task<(int[], DateTime)> GetWorkItemIDs(string query, Wiql wiql, WorkItemTrackingHttpClient client, int count = 0) {
            wiql.Query = query;
            WorkItemQueryResult? results = null;
            if (count == 0) {
                results = await client.QueryByWiqlAsync(wiql);
            } else {
                results = await client.QueryByWiqlAsync(wiql, top: count);
            }
            var ids = results.WorkItems.Select(i => i.Id).ToArray();

            return (ids, results.AsOf);
        }

        private static async Task<List<WorkItem>> BatchGetWorkItems(int[] workIds, WorkItemTrackingHttpClient client, DateTime? AsOf, string[]? validFields = null, int batchSize = 100) {
            List<WorkItem> ret = new List<WorkItem>();
            int totalWorkItems = workIds.Length;

            // This is the maximum the ADO api supports
            if (batchSize > 200) batchSize = 200;

            for (int i = 0; i < totalWorkItems; i += batchSize) {
                int remainingWorkItems = totalWorkItems - i;
                int batchCount = Math.Min(batchSize, remainingWorkItems);

                int[] batchIds = new int[batchCount];
                Array.Copy(workIds, i, batchIds, 0, batchCount);

                List<WorkItem> workItems = new List<WorkItem>();
                try {
                    workItems = await client.GetWorkItemsAsync(batchIds, validFields, AsOf, errorPolicy: WorkItemErrorPolicy.Omit);
                } catch (Exception ex) {
                    //logger.LogWarning(ex, "An unexpected error occured while retreiving work items.");
                }

                ret.AddRange(workItems);
            }

            return ret;
        }
    }
}