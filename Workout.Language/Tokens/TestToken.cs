namespace Workout.Language.Tokens;

internal sealed record TestToken : Token
{
    public List<AssertionToken> Assertions { get; } = [];

    public TestToken(int line, string value) 
        : base(line, value, TokenType.Test)
    {
    }

    public void AddAssertion(AssertionToken assertion)
    {
        this.Assertions.Add(assertion);
    }
}