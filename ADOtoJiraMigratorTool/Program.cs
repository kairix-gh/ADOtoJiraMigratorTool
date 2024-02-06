using ADOtoJiraMigratorTool.Config;
using ADOtoJiraMigratorTool.TaskHandlers;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

internal class Program {
    private static async Task Main(string[] args) {
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appconfig.json")
            .AddJsonFile("fieldmapping.json")
            .AddUserSecrets<AppConfig>()
            .Build();

        AppConfig appConfig = config.Get<AppConfig>() ?? new AppConfig();

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
                    new ADOTaskHandler(ctx.AddTask("Download WorkItem Data from ADO", new ProgressTaskSettings() { AutoStart = true }), appConfig),
                    //new TransformDataHandler(ctx.AddTask("Prepare Data for Jira", new ProgressTaskSettings() { AutoStart = true })),
                    new JiraImportTaskHandler(ctx.AddTask("Importing Data into Jira", new ProgressTaskSettings() { AutoStart = true }), appConfig)
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