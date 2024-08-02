namespace Workout.Language.Tokens;

internal sealed record ImportToken : Token
{
    public ImportToken(int line, string value) 
        : base(line, value, TokenType.Import)
    {
    }
}
