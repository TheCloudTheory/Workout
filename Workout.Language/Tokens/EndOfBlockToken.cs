namespace Workout.Language.Tokens;

internal sealed record EndOfBlockToken : Token
{
    public EndOfBlockToken(int line, string value) 
        : base(line, value, TokenType.EndOfBlock)
    {
    }
}
