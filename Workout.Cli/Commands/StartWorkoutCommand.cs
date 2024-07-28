using Spectre.Console.Cli;
using Workout.Cli.Internals.Logging;
using Workout.Language;

namespace Workout.Cli.Commands;

internal sealed class StartWorkoutCommand : Command<StartWorkoutCommandSettings>
{
    private readonly ILogger logger;

    public StartWorkoutCommand(
        ILogger logger
    )
    {
        this.logger = logger;
    }

    public override int Execute(CommandContext context, StartWorkoutCommandSettings settings)
    {
        this.logger.LogDebug($"Starting command {nameof(StartWorkoutCommand)}.");

        if (settings.WorkingDirectory is not null)
        {
            this.logger.LogInformation($"Working directory: {settings.WorkingDirectory}");
        }
        else
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            settings.WorkingDirectory = currentDirectory;
            this.logger.LogInformation($"Working directory: {currentDirectory}");
        }

        this.logger.LogDebug("Searching for workout files.");
        var workoutFiles = Directory.GetFiles(settings.WorkingDirectory, "*.workout", SearchOption.AllDirectories);
        this.logger.LogInformation($"Found {workoutFiles.Length} workout files.");

        if(workoutFiles.Length == 0)
        {
            this.logger.LogWarning("No workout files found.");
            return 0;
        }

        foreach (var workoutFile in workoutFiles)
        {
            this.logger.LogInformation($"Parsing workout file: {workoutFile}");
            var parser = new WorkoutFileParser();
            parser.Parse(workoutFile);
        }

        return 0;
    }
}

internal sealed class StartWorkoutCommandSettings : CommandSettings
{
    [CommandOption("-d|--working-directory")]
    public string? WorkingDirectory { get; set; }
}