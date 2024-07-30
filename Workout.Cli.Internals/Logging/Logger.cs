using Spectre.Console;

namespace Workout.Cli.Internals.Logging;

internal sealed class Logger : ILogger
{
    public void LogDebug(string message)
    {
        Log(message, LogLevel.Debug);
    }

    public void LogError(string message)
    {
        AnsiConsole.MarkupLine($"[red][bold]{message}[/][/]");
    }

    public void LogInformation(string message)
    {
        Log(message, LogLevel.Information);
    }

    public void LogWarning(string message)
    {
        Log(message, LogLevel.Warning);
    }

    private void Log(string message, LogLevel level)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        AnsiConsole.WriteLine($"[{timestamp}][{level}] {message}");
    }
}
