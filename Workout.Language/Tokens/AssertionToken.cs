namespace Workout.Language.Tokens;

internal sealed record AssertionToken : Token
{
    public AssertionToken(int line, string value) 
        : base(line, value, TokenType.Assertion)
    {
    }
}
