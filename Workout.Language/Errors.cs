namespace Workout.Language;

internal sealed class Errors
{
    public static Error Error_NullImportPath(int line, int column) => new("E001", "Import path cannot be null.", line, column);
}

internal sealed record Error(string Code, string Message, int Line, int Column);
