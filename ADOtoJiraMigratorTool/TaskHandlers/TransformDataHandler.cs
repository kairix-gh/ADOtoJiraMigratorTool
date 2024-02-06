using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Spectre.Console;

namespace ADOtoJiraMigratorTool.TaskHandlers {
    public class TransformDataHandler : TaskHandler {
        public TransformDataHandler(ProgressTask task) : base(task) {
        }

        public override async Task DoWork() {
            if (Input == null) {
                ProgressTask.Increment(100);
                return;
            }

            List<WorkItem> data = (Input as List<WorkItem>) ?? new List<WorkItem>();
            ProgressTask.MaxValue = data.Count;

            // Transform stuff I guess
            foreach (WorkItem item in data) {
                await Task.Delay(2);
                ProgressTask.Increment(1);
            }

            Output = Input;
        }
    }
}