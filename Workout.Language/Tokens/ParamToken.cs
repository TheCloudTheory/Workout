
namespace Workout.Language.Tokens;

internal sealed record ParamToken : Token
{
    public ParamToken(int Line, string? Value) 
    : base(Line, Value, TokenType.Param)
    {
        var parts = TrimToken(Value!).Split(",");

        Name = parts[0];
        ParamValue = parts[1].Trim();
    }

    private string TrimToken(string value)
    {
        return value.Trim().Replace("param(", string.Empty).TrimEnd(')');
    }

    public string Name { get; }
    public string ParamValue { get; }

    internal void Validate(List<Error> errors)
    {
        if(string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(Errors.Error_InvalidParamName(Line, 0));
        }

        if(string.IsNullOrWhiteSpace(ParamValue))
        {
            errors.Add(Errors.Error_InvalidParamValue(Line, 0));
        }
    }
}
