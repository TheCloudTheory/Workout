namespace Workout.Cli.Internals;

internal sealed class WorkoutContext
{
    public static bool IsDebugEnabled => _IsDebugEnabled;

    private static bool _IsDebugEnabled = false;

    public static void EnableDebug()
    {
        _IsDebugEnabled = true;
    }
}
