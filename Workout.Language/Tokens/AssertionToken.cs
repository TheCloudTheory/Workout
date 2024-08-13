﻿using System.Linq.Expressions;
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

        // We need to flatten variables in similar way as resources, but it's possible that there are none. In that case,
        // we need to handle it gracefully.
        var flattenedVariables = this.compilations.Select(compilation => compilation.Template).Where(_ => _.Variables != null).SelectMany(_ => _.Variables).ToList();
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
            else
            {
                result = current[accessor];
            }

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
            var evaluatedProperty = EvaluateDynamicProperty(value, flattenedVariables).TrimStart('[').TrimEnd(']');

            this.logger.LogDebug($"Finished evaluating dynamic property. Result: {evaluatedProperty}.");
            return evaluatedProperty;
        }

        return value;
    }

    private string EvaluateDynamicProperty(string value, List<KeyValuePair<string, Azure.Deployments.Core.Entities.TemplateGenericProperty<JToken>>> variables)
    {
        var isMatch = true;
        while(isMatch)
        {
            isMatch = false;

            var paramMatches = ParamRegex().Matches(value);
            if (paramMatches.Count > 0)
            {
                this.logger.LogDebug($"Found {paramMatches.Count} parameters in dynamic property.");

                isMatch = true;
                foreach (Match match in paramMatches)
                {
                    this.logger.LogDebug($"Evaluating parameter {match.Value}.");

                    var param = match.Value.Replace("parameters(", string.Empty).TrimEnd(')');
                    var paramValue = this.parent.Params.Single(_ => _.Name == param.Replace("'", string.Empty)).ParamValue;
                    var replacedValue = value.Replace(match.Value, paramValue!.ToString());

                    this.logger.LogDebug($"Replaced parameter {param} with value {paramValue}.");
                    value = replacedValue;
                }
            }

            var variableMatches = VariableRegex().Matches(value);
            if (variableMatches.Count > 0)
            {
                this.logger.LogDebug($"Found {variableMatches.Count} variables in dynamic property.");

                isMatch = true;
                foreach (Match match in variableMatches)
                {
                    this.logger.LogDebug($"Evaluating variable {match.Value}.");

                    var variable = match.Value.Replace("variables(", string.Empty).TrimEnd(')');
                    var variableValue = variables.Single(_ => _.Key == variable.Replace("'", string.Empty)).Value.Value;
                    var replacedValue = value.Replace(match.Value, variableValue!.ToString());

                    this.logger.LogDebug($"Replaced variable {variable} with value {variableValue}.");
                    value = replacedValue;
                }
            }

            var formatMatches = FormatRegex().Matches(value);
            if (formatMatches.Count > 0)
            {
                this.logger.LogDebug($"Found {formatMatches.Count} format functions in dynamic property.");

                isMatch = true;
                foreach (Match match in formatMatches)
                {
                    this.logger.LogDebug($"Evaluating format function {match.Value}.");

                    var args = match.Value.Replace("format(", string.Empty).TrimEnd(')').Split(",");
                    var formatValue = args[0].Replace("'", string.Empty).Trim();
                    var formatArgs = args[1].Replace("'", string.Empty).Trim();
                    var replacedValue = value.Replace(match.Value, string.Format(formatValue, formatArgs));

                    this.logger.LogDebug($"Replaced format function {match.Value} with value {replacedValue}.");
                    value = replacedValue;
                }
            }

            var ifMatches = IfRegex().Matches(value);
            if (CanPerformIfConditionCompilation(paramMatches, variableMatches, formatMatches, ifMatches))
            {
                this.logger.LogDebug($"Found {ifMatches.Count} if functions in dynamic property.");

                isMatch = true;
                foreach (Match match in ifMatches)
                {
                    this.logger.LogDebug($"Evaluating if function {match.Value}.");

                    var args = match.Value.Replace("if(", string.Empty).TrimEnd(')').Split(",");
                    var condition = args[0].Replace("'", string.Empty).Trim();
                    var conditionValue = bool.Parse(condition);
                    var replacedValue = conditionValue.ToString();

                    this.logger.LogDebug($"Replaced if function {match.Value} with value {replacedValue}.");
                    value = replacedValue;
                }
            }
        }

        var result = value;
        return result;
    }

    // if() statement can be evaluated only if there are no other dynamic expressions awaiting compilation.
    // This is because if() statement needs conrete values to evaluate the condition.
    private static bool CanPerformIfConditionCompilation(MatchCollection paramMatches, MatchCollection variableMatches, MatchCollection formatMatches, MatchCollection ifMatches)
    {
        return ifMatches.Count > 0 && paramMatches.Count == 0 && variableMatches.Count == 0 && formatMatches.Count == 0;
    }

    [GeneratedRegex(@"parameters\('.+'\)", RegexOptions.Compiled)]
    private static partial Regex ParamRegex();

    [GeneratedRegex(@"variables\('.+'\)", RegexOptions.Compiled)]
    private static partial Regex VariableRegex();

    // TODO: This regex must be enhanced in the future to support format() functions with more
    // than one parameter.
    [GeneratedRegex(@"format\('.+', ?'.+'\)", RegexOptions.Compiled)]
    private static partial Regex FormatRegex();

    [GeneratedRegex(@"if\(.+, .+, .+\)", RegexOptions.Compiled)]
    private static partial Regex IfRegex();
}

internal enum ParameterType
{
    String,
    Integer,
    Boolean,
    Expression
}
