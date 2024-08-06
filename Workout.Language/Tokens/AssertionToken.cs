using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using Workout.Bicep;

namespace Workout.Language.Tokens;

internal sealed record AssertionToken : Token
{
    public AssertionToken(int line, string value, CompilationResult[] compilations) 
        : base(line, value, TokenType.Assertion)
    {
        Assertion = GetExpression(compilations);
    }

    public AssertionExpression Assertion { get; }


    public AssertionExpression GetExpression(CompilationResult[] compilations)
    {
        var expression = this.Value!.Trim();
        
        if(expression.StartsWith("equals"))
        {
            expression = expression.Replace("equals(", string.Empty).Replace(")", string.Empty).Trim();
            var args = expression.Split(",");
            var left = new AssertionExpressionParameter(args[0], compilations);
            var right = new AssertionExpressionParameter(args[1], compilations);

            var leftExpression = Expression.Parameter(typeof(string), "left");
            var rightExpression = Expression.Parameter(typeof(string), "right");
            var equalsExpression = Expression.Equal(
                leftExpression,
                rightExpression
            );

            return new AssertionExpression(
                Expression.Lambda(equalsExpression, leftExpression, rightExpression),
                [left.Value!, right.Value!]
            );
        }

        throw new Exception("Invalid assertion.");
    }
}

internal sealed record AssertionExpression(LambdaExpression Expression, object[] Args)
{
    public bool Evaluate()
    {
        var compiled = Expression.Compile();
        var value = compiled.DynamicInvoke(TrimQuotes(Args[0].ToString()!), TrimQuotes(Args[1].ToString()!));

        if(value == null) return false; // TODO: May be better to find something meaningful to return here.

        return (bool)value;
    }

    private string TrimQuotes(string value)
    {
        return value.Trim().TrimStart('\'').TrimEnd('\'');
    }
}

internal sealed record AssertionExpressionParameter
{
    private readonly CompilationResult[] compilations;

    public AssertionExpressionParameter(string value, CompilationResult[] compilations)
    {
        RawValue = value.Trim();
        Type = GetParameterType();
        this.compilations = compilations;
    }

    public string RawValue { get; }
    public ParameterType Type { get; }
    public object? Value => Type switch
    {
        ParameterType.String => RawValue,
        ParameterType.Integer => int.Parse(RawValue),
        ParameterType.Boolean => bool.Parse(RawValue),
        ParameterType.Expression => EvaluateExpression(),
        _ => null
    };

    private ParameterType GetParameterType()
    {
        if(RawValue.StartsWith("'") && RawValue.EndsWith("'"))
        {
            return ParameterType.String;
        }

        if(RawValue == "true" || RawValue == "false")
        {
            return ParameterType.Boolean;
        }

        if(int.TryParse(RawValue, out _))
        {
            return ParameterType.Integer;
        }

        var accessors = RawValue.Split(".");
        if(accessors.Length > 1)
        {
            return ParameterType.Expression;
        }

        throw new Exception("Invalid parameter type.");
    }

    private object EvaluateExpression()
    {
        var accessors = RawValue.Split(".");
        var resourceAccessor = accessors[0];

        if(accessors.Length == 1)
        {
            // TODO: Nothing to do, it should evaluate to a whole resource model so the question is
            // how to handle this.

            return new object();
        }

        var flattenedResources = this.compilations.Select(compilation => compilation.Template).SelectMany(_ => _.Resources).ToList();
        var resourceDefinition = flattenedResources.Single(_ => _.WorkoutResourceId.Value == resourceAccessor);
        var json = resourceDefinition.ToJson();
        var rawObject = JObject.Parse(json);

        // We need to skip first accessor because it's the resource identifier from Bicep.
        // As an example, if the expression is "rg.name", we need to skip "rg" because
        // those identifiers are non-existent in the JSON object.
        var property = accessors.Skip(1).Aggregate((JToken)rawObject, (current, accessor) => {
            JToken? result;

            // If the current token is an array, we need to parse the accessor as an integer
            // and get the element from the array. The requirement for that integer parsing
            // comes from Newtonsoft.Json library.
            //
            // Note, that this doesn't work for accessing dictionary elements. From the syntax
            // perspective, nothing prevents us from accessing them, but the implementation
            // doesn't support it as of now.
            if(current.Type == JTokenType.Array)
            {
                var index = int.Parse(accessor);
                result = current[index];
            }
            
            result = current[accessor];
            
            if(result == null)
            {
                throw new Exception($"Property {accessor} not found.");
            }

            return result;
        });

        var value = property.ToString();
        return value;
    }
}

internal enum ParameterType
{
    String,
    Integer,
    Boolean,
    Expression
}
