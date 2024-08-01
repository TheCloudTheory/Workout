namespace Workout.Language;

internal sealed record Token(TokenType Type, int Line, string? Value)
{
    private readonly List<Token> tokens = [];

    public IReadOnlyCollection<Token> Tokens => this.tokens;

    public void AddToken(Token token)
    {
        this.tokens.Add(token);
    }
};

internal enum TokenType
{
    Import,
    SmokeTestDecorator,
    Test,
    EndOfBlock,
    Assertion
}
