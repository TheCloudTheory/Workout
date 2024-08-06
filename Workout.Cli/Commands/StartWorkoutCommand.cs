using System.IO.Abstractions;
using Spectre.Console.Cli;
using Workout.Cli.Internals.Logging;
using Workout.Language;

namespace Workout.Cli.Commands;

internal sealed class StartWorkoutCommand : Command<StartWorkoutCommandSettings>
{
    private readonly IServiceProvider provider;
    private readonly ILogger logger;

    public StartWorkoutCommand(
        IServiceProvider provider,
        ILogger logger
    )
    {
        this.provider = provider;
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
        var workoutFiles = settings.File != null ?
            [Path.Combine(settings.WorkingDirectory, settings.File)] :
            Directory.GetFiles(settings.WorkingDirectory, "*.workout", SearchOption.AllDirectories);

        this.logger.LogInformation($"Found {workoutFiles.Length} workout files.");

        if (workoutFiles.Length == 0)
        {
            this.logger.LogWarning("No workout files found.");
            return 0;
        }

        var tests = new List<TestModel>();

        foreach (var workoutFile in workoutFiles)
        {
            var parser = new WorkoutFileParser(
                settings.WorkingDirectory,
                new Bicep.BicepCompilationProvider(new FileSystem(), this.provider),
                this.logger);

            try
            {
                this.logger.LogDebug($"Parsing workout file: {workoutFile}.");
                var parsedTests = parser.Parse(workoutFile).GetAwaiter().GetResult();
                this.logger.LogDebug($"Parsed {parsedTests.Count} tests from {workoutFile}.");

                tests.AddRange(parsedTests);
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Error parsing workout file: {workoutFile}. {ex.Message}");
                this.logger.LogDebug(ex.StackTrace!);
                return 1;
            }
        }

        this.logger.LogInformation($"Found {tests.Count} tests.");

        var failedTests = new List<TestModel>();

        if (settings.TestCase is not null)
        {
            tests = tests.Where(x => x.TestName == settings.TestCase).ToList();
            this.logger.LogDebug("Filtered tests by test case.");
        }

        foreach (var test in tests)
        {
            this.logger.LogInformation($"Running test: {test.TestName}.");

            var results = new List<bool>();
            foreach (var assertion in test.Assertions)
            {
                this.logger.LogDebug($"Running assertion: {assertion.Value}.");

                var result = assertion.Assertion.Evaluate();
                this.logger.LogDebug($"Running assertion: {assertion.Value} | Result: {result}.");

                results.Add(result);
            }

            if (results.All(x => x))
            {
                this.logger.LogInformation($"Test {test.TestName} passed.");
            }
            else
            {
                this.logger.LogError($"Test {test.TestName} failed.");
                failedTests.Add(test);
            }
        }

        if (failedTests.Count > 0)
        {
            this.logger.LogError($"Failed tests: {failedTests.Count}.");
            return 1;
        }

        this.logger.LogInformation("All tests passed.");
        return 0;
    }
}

internal sealed class StartWorkoutCommandSettings : CommandSettings
{
    [CommandOption("-d|--working-directory")]
    public string? WorkingDirectory { get; set; }

    [CommandOption("-t|--test-case")]
    public string? TestCase { get; set; }

    [CommandOption("-f|--file")]
    public string? File { get; set; }
}