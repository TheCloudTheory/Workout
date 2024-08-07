
namespace Workout.Language.Tokens;

internal sealed record ParamToken : Token
{
    public ParamToken(int Line, string? Value) 
    : base(Line, Value, TokenType.Param)
    {
        var parts = TrimToken(Value!).Split(",");

        Name = parts[0];
        RawValue = parts[1].Trim();
        ParamType = GetParameterType();
    }

    private string TrimToken(string value)
    {
        return value.Trim().Replace("param(", string.Empty).TrimEnd(')');
    }

    public string Name { get; }
    public string RawValue { get; }
    public ParameterType ParamType { get; }

    public object? ParamValue => ParamType switch
    {
        ParameterType.String => RawValue,
        ParameterType.Integer => int.Parse(RawValue),
        ParameterType.Boolean => bool.Parse(RawValue),
        _ => null
    };

    private ParameterType GetParameterType()
    {
        if (RawValue.StartsWith("'") && RawValue.EndsWith("'"))
        {
            return ParameterType.String;
        }

        if (RawValue == "true" || RawValue == "false")
        {
            return ParameterType.Boolean;
        }

        if (int.TryParse(RawValue, out _))
        {
            return ParameterType.Integer;
        }

        throw new Exception("Invalid parameter type.");
    }

    internal void Validate(List<Error> errors)
    {
        if(string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(Errors.Error_InvalidParamName(Line, 0));
        }

        if(string.IsNullOrWhiteSpace(RawValue.ToString()))
        {
            errors.Add(Errors.Error_InvalidParamValue(Line, 0));
        }
    }
}
