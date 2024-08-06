
namespace Workout.Language;

internal sealed class Errors
{
    public static Error Error_NullImportPath(int line, int column) => new("E001", "Import path cannot be null.", line, column);
    public static Error Error_InvalidImportPath(int line, int column) => new("E002", "Cannot import Bicep file - file doesn't exist.", line, column);
    public static Error Error_NoImportFound(int line, int column) => new("E003", "Workout file contains no imports.", line, column);
    public static Error Error_InvalidToken(string token, int line, int column) => new("E004", $"Provided token '{token}' is invalid.", line, column);
    public static Error Error_InvalidAssertion(string assertion, int line, int column) => new("E005", $"Provided assertion '{assertion}' is invalid.", line, column);
    internal static Error Error_InvalidParamName(int line, int column) => new("E006", $"Provided param name is invalid or empty.", line, column);
    internal static Error Error_InvalidParamValue(int line, int column) => new("E007", $"Provided param value is invalid or empty.", line, column);
}

internal sealed record Error(string Code, string Message, int Line, int Column);
