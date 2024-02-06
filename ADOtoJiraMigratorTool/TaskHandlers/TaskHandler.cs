using ADOtoJiraMigratorTool.Config;
using Spectre.Console;

namespace ADOtoJiraMigratorTool.TaskHandlers {
    public abstract class TaskHandler {
        public ProgressTask ProgressTask { get; set; }

        public AppConfig? Config { get; set; }
        public object? Input { get; set; }
        public object? Output { get; set; }

        public TaskHandler(ProgressTask task) {
            ProgressTask = task;
        }

        public TaskHandler(ProgressTask task, AppConfig config) : this(task) {
            Config = config;
        }

        public abstract Task DoWork();
    }
}