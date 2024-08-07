using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Workout.Bicep;
using Workout.Cli.Internals.Logging;

namespace Workout.Language.Tokens;

internal sealed record AssertionToken : Token
{
    private readonly TestToken parent;
    private readonly ILogger logger;

    public AssertionToken(int line, string value, CompilationResult[] compilations, TestToken parent, Cli.Internals.Logging.ILogger logger)
        : base(line, value, TokenType.Assertion)
    {
        this.parent = parent;
        this.logger = logger;

        Assertion = GetExpression(compilations);
    }

    public AssertionExpression Assertion { get; }


    public AssertionExpression GetExpression(CompilationResult[] compilations)
    {
        var expression = this.Value!.Trim();

        if (expression.StartsWith("equals"))
        {
            this.logger.LogDebug("Building expression for 'equals' assertion.");

            expression = expression.Replace("equals(", string.Empty).Replace(")", string.Empty).Trim();
            var args = expression.Split(",");
            var left = new AssertionExpressionParameter(args[0], compilations, this.parent, this.logger);
            var right = new AssertionExpressionParameter(args[1], compilations, this.parent, this.logger);

            var leftExpression = Expression.Parameter(typeof(string), "left");
            var rightExpression = Expression.Parameter(typeof(string), "right");
            var equalsExpression = Expression.Equal(
                leftExpression,
                rightExpression
            );

            this.logger.LogDebug("Finished building expression for 'equals' assertion.");

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

        if (value == null) return false; // TODO: May be better to find something meaningful to return here.

        return (bool)value;
    }

    private string TrimQuotes(string value)
    {
        return value.Trim().TrimStart('\'').TrimEnd('\'');
    }
}

internal sealed partial record AssertionExpressionParameter
{
    private readonly CompilationResult[] compilations;
    private readonly TestToken parent;
    private readonly ILogger logger;

    public AssertionExpressionParameter(string value, CompilationResult[] compilations, TestToken parent, ILogger logger)
    {
        this.compilations = compilations;
        this.parent = parent;
        this.logger = logger;

        RawValue = value.Trim();
        Type = GetParameterType();

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

        var accessors = RawValue.Split(".");
        if (accessors.Length > 1)
        {
            return ParameterType.Expression;
        }

        throw new Exception("Invalid parameter type.");
    }

    private object EvaluateExpression()
    {
        var accessors = RawValue.Split(".");
        var resourceAccessor = accessors[0];

        if (accessors.Length == 1)
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
        var property = accessors.Skip(1).Aggregate((JToken)rawObject, (current, accessor) =>
        {
            JToken? result;
            this.logger.LogDebug($"Evaluating property {accessor}.");

            // If the current token is an array, we need to parse the accessor as an integer
            // and get the element from the array. The requirement for that integer parsing
            // comes from Newtonsoft.Json library.
            //
            // Note, that this doesn't work for accessing dictionary elements. From the syntax
            // perspective, nothing prevents us from accessing them, but the implementation
            // doesn't support it as of now.
            if (current.Type == JTokenType.Array)
            {
                var index = int.Parse(accessor);
                result = current[index];
            }

            result = current[accessor];

            if (result == null)
            {
                throw new Exception($"Property {accessor} not found.");
            }

            return result;
        });

        var value = property.ToString();

        // If value of a property starts with '[' and ends with ']' it means, that it's built dynamically
        // using functions, parameters and variables. We need to evaluate it.
        if (value.StartsWith("[") && value.EndsWith("]"))
        {
            this.logger.LogDebug($"Evaluating dynamic property {value}.");
            return EvaluateDynamicProperty(value);
        }

        return value;
    }

    private string EvaluateDynamicProperty(string value)
    {
        var paramMatches = ParamRegex().Matches(value);
        if(paramMatches.Count > 0)
        {
            this.logger.LogDebug($"Found {paramMatches.Count} parameters in dynamic property.");

            foreach(Match match in paramMatches)
            {
                this.logger.LogDebug($"Evaluating parameter {match.Value}.");

                var param = match.Value.Replace("parameters(", string.Empty).TrimEnd(')');
                var paramValue = this.parent.Params.Single(_ => _.Name == param.Replace("'", string.Empty)).ParamValue;
                var replacedValue = value.Replace(match.Value, paramValue!.ToString());

                this.logger.LogDebug($"Replaced parameter {param} with value {paramValue}.");
                value = replacedValue;
            }
        }

        var result = value.TrimStart('[').TrimEnd(']');
        this.logger.LogDebug($"Finished evaluating dynamic property. Result: {result}.");

        return result;
    }

    [GeneratedRegex(@"parameters\('.+'\)", RegexOptions.Compiled)]
    private static partial Regex ParamRegex();
}

internal enum ParameterType
{
    String,
    Integer,
    Boolean,
    Expression
}
