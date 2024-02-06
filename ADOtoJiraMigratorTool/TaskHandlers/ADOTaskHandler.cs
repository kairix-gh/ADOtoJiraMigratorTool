using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace ADOtoJiraMigratorTool.TaskHandlers {
    internal class ADOTaskHandler : TaskHandler {
        public ADOTaskHandler(ProgressTask task) : base(task) {
            ProgressTask.IsIndeterminate = true;
        }

        public ADOTaskHandler(ProgressTask task, IConfiguration config) : base(task, config) {
            ProgressTask.IsIndeterminate = true;
        }

        public override async Task DoWork() {
            ProgressTask.MaxValue(1);
            ProgressTask.StartTask();

            if (Config != null) {
                Output = await AzureDevOps.GetBacklogItemsForDataDump(Config, 0, 5000);
            }

            ProgressTask.Increment(1);
        }

        public void Init(object data) {
        }
    }
}