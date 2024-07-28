namespace Workout.Language;

internal sealed record Token(TokenType Type, int Line, string? Value);

internal enum TokenType
{
    Import,
    SmokeTestDecorator,
    Test,
    EndOfBlock,
    Assertion
}
