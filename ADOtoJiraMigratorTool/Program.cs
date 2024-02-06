using ADOtoJiraMigratorTool;
using ADOtoJiraMigratorTool.TaskHandlers;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

internal class Program {
    private static async Task Main(string[] args) {
        IConfiguration configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appconfig.json")
            .AddUserSecrets<AppConfig>()
            .Build();

        await AnsiConsole.Progress()
            .Columns(new ProgressColumn[] {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new RemainingTimeColumn(),
            })
            .StartAsync(async ctx => {
                var taskList = new TaskHandler[] {
                    new ADOTaskHandler(ctx.AddTask("Download WorkItem Data from ADO", new ProgressTaskSettings() { AutoStart = true }), configBuilder),
                    //new TransformDataHandler(ctx.AddTask("Prepare Data for Jira", new ProgressTaskSettings() { AutoStart = true })),
                    new JiraImportTaskHandler(ctx.AddTask("Importing Data into Jira", new ProgressTaskSettings() { AutoStart = true }), configBuilder)
                };
                int currentTask = 0;

                while (!ctx.IsFinished) {
                    if (currentTask >= taskList.Length) break;
                    if (currentTask > 0) {
                        taskList[currentTask].Input = taskList[currentTask - 1].Output;
                    }

                    await taskList[currentTask].DoWork();

                    if (taskList[currentTask].ProgressTask.IsFinished) {
                        currentTask++;
                    }
                }
            });
    }
}