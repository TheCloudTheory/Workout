namespace Workout.Language.Tokens;

internal record Token(int Line, string? Value, TokenType Type);

internal enum TokenType
{
    Import,
    SmokeTestDecorator,
    Test,
    EndOfBlock,
    Assertion,
    Param
}
