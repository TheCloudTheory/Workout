namespace Workout.Language;

internal sealed class Errors
{
    public static Error Error_NullImportPath(int line, int column) => new("E001", "Import path cannot be null.", line, column);
    public static Error Error_InvalidImportPath(int line, int column) => new("E002", "Cannot import Bicep file - file doesn't exist.", line, column);
}

internal sealed record Error(string Code, string Message, int Line, int Column);
