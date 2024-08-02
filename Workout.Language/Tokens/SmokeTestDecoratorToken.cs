namespace Workout.Language.Tokens;

internal sealed record SmokeTestDecoratorToken : Token
{
    public SmokeTestDecoratorToken(int line, string value) 
        : base(line, value, TokenType.SmokeTestDecorator)
    {
    }
}
