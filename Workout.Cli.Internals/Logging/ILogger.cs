namespace Workout.Cli.Internals.Logging;

internal interface ILogger
{
    void LogDebug(string message);
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message);
}
