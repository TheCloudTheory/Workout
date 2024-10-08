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

    public AssertionToken(int line, string value, CompilationResult[] compilations, TestToken parent, ILogger logger)
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
                [left.Value!, right.Value!],
                this.logger
            );
        }

        throw new Exception("Invalid assertion.");
    }
}

internal sealed record AssertionExpression(LambdaExpression Expression, object[] Args, ILogger Logger)
{
    public bool Evaluate()
    {
        var compiled = Expression.Compile();
        var left = TrimQuotes(Args[0].ToString()!);
        var right = TrimQuotes(Args[1].ToString()!);
        var value = compiled.DynamicInvoke(left, right);

        Logger.LogDebug($"Comparing [{left}, {right}]; assertion evaluated to {value}.");

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

        var flattenedResources = this.compilations.SelectMany(compilation => compilation.Templates).SelectMany(_ => _.Resources).ToList();

        // We need to flatten variables in similar way as resources, but it's possible that there are none. In that case,
        // we need to handle it gracefully.
        var flattenedVariables = this.compilations.SelectMany(compilation => compilation.Templates).Where(_ => _.Variables != null).SelectMany(_ => _.Variables).ToList();

        // We need to do the same for parameters
        var flattenedParameters = this.compilations.SelectMany(compilation => compilation.Templates).Where(_ => _.Parameters != null).SelectMany(_ => _.Parameters).ToList();

        // We need to flatten output as well
        var flattenedOutputs = this.compilations.SelectMany(compilation => compilation.Templates).Where(_ => _.Outputs != null).SelectMany(_ => _.Outputs).ToList();

        var resourceDefinition = flattenedResources.Single(_ => _.WorkoutResourceId.Value == resourceAccessor);
        var json = resourceDefinition.ToJson();
        var rawObject = JObject.Parse(json);

        string? value;

        // Expression may contain either outputs or properties. If it contains outputs, there're two ways how we need to handle it.
        // If it's an output of the main module, we can reference defined outputs directly. If it's an output of a nested module,
        // we need to reference the nested module and then the output.
        if (accessors.Contains("outputs"))
        {
            var outputAccessor = accessors.Last();
            var output = flattenedOutputs.Single(_ => _.Key == outputAccessor);
            value = output.Value.Value.Value.ToString();
        }
        else
        {
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

            value = property.ToString();
        }

        // If value of a property starts with '[' and ends with ']' it means, that it's built dynamically
        // using functions, parameters and variables. We need to evaluate it.
        if (value.StartsWith("[") && value.EndsWith("]"))
        {
            this.logger.LogDebug($"Evaluating dynamic property {value}.");

            // We trim a bunch of extra characters like '[, [, )' from the format value to make sure that we got rid.
            // of all the artifacts of evaluation.
            var evaluatedProperty = EvaluateDynamicProperty(value, flattenedVariables, flattenedParameters, flattenedResources, resourceDefinition).TrimStart('[').TrimEnd(']').TrimEnd(')');

            this.logger.LogDebug($"Finished evaluating dynamic property. Result: {evaluatedProperty}.");
            return evaluatedProperty;
        }

        return value;
    }

    private string EvaluateDynamicProperty(
        string value,
        List<KeyValuePair<string, Azure.Deployments.Core.Entities.TemplateGenericProperty<JToken>>> variables,
        List<KeyValuePair<string, Azure.Deployments.Core.Definitions.Schema.TemplateInputParameter>> parameters,
        List<TemplateResource> flattenedResources,
        TemplateResource resourceDefinition)
    {
        var isMatch = true;
        while (isMatch)
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
                    var paramValue = this.parent.Params.SingleOrDefault(_ => _.Name == param.Replace("'", string.Empty))?.ParamValue;

                    // A parameters may have been defined with default value, hence it's worth checking for it 
                    // in the template
                    if (paramValue == null)
                    {
                        var paramKey = param.Replace("'", string.Empty);
                        var parameter = parameters.SingleOrDefault(_ => _.Key == paramKey).Value?.DefaultValue?.Value.ToString();
                        if (parameter != null)
                        {
                            paramValue = parameter;
                        }
                        else
                        {
                            // It's possible that the parameter is not passed as input of a test, but rather is
                            // a parameter of a nested module. In that case, we need to find the parameter in the
                            // schema of the module.
                            parameter = resourceDefinition.Properties.Value["parameters"]![paramKey]!["value"]!.ToString();
                            if (parameter != null)
                            {
                                paramValue = parameter;
                            }
                            else
                            {
                                throw new Exception($"Parameter {param} not found.");
                            }
                        }
                    }

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

            // Do not attempt to evaluate other dynamic expressions if there are parameters or variables left
            if (variableMatches.Count != 0 || paramMatches.Count != 0)
            {
                continue;
            }

            var formatMatches = FormatRegex().Matches(value);
            if (formatMatches.Count > 0)
            {
                this.logger.LogDebug($"Found {formatMatches.Count} format functions in dynamic property.");

                isMatch = true;
                foreach (Match match in formatMatches)
                {
                    this.logger.LogDebug($"Evaluating format function {match.Value}.");

                    // As we're evaluating an exact match, we can safely try to compile other dynamic expressions
                    // first and then evaluate the format function.
                    var rawMatch = match.Value;
                    CompileIfExpression(ref rawMatch, ref isMatch);

                    var args = rawMatch.Replace("format(", string.Empty).TrimEnd(')').Split(",");
                    var formatValue = args[0].Replace("'", string.Empty).Trim();
                    var formatArgs = args.Skip(1).Select(_ => _.Replace("'", string.Empty).Trim()).ToArray();
                    var replacedValue = value.Replace(match.Value, string.Format(formatValue, formatArgs));

                    this.logger.LogDebug($"Replaced format function {match.Value} with value {replacedValue}.");
                    value = replacedValue;
                }
            }

            CompileIfExpression(ref value, ref isMatch);
        }

        var result = value;
        return result;
    }

    private void CompileIfExpression(ref string value, ref bool isMatch)
    {
        var ifMatches = IfRegex().Matches(value);
        if (ifMatches.Count == 0)
        {
            return;
        }

        this.logger.LogDebug($"Found {ifMatches.Count} if functions in dynamic property.");

        isMatch = true;
        foreach (Match match in ifMatches)
        {
            this.logger.LogDebug($"Evaluating if function {match.Value}.");

            // Note we need to replace true() and false() with True and False respectively to make sure that
            // they are compiled before the evaluation.
            var args = match.Value.Replace("true()", "True").Replace("false()", "False").Replace("if(", string.Empty).TrimEnd(')').Split(",");
            var condition = args[0].Replace("'", string.Empty).Trim();
            var conditionValue = bool.Parse(condition) ? args[1] : args[2];
            var replacedValue = conditionValue.ToString();

            this.logger.LogDebug($"Replaced if function {match.Value} with value {replacedValue}.");
            value = value.Replace(match.Value, replacedValue);
        }
    }

    [GeneratedRegex(@"parameters\('\w+'\)", RegexOptions.Compiled)]
    private static partial Regex ParamRegex();

    [GeneratedRegex(@"variables\('.+'\)", RegexOptions.Compiled)]
    private static partial Regex VariableRegex();

    // TODO: This regex must be enhanced in the future to support format() functions with more
    // than one parameter.
    [GeneratedRegex(@"format\('.+', ?'?.+'?\)", RegexOptions.Compiled)]
    private static partial Regex FormatRegex();

    [GeneratedRegex(@"if\(.+, .+, .+\){1}", RegexOptions.Compiled)]
    private static partial Regex IfRegex();
}

internal enum ParameterType
{
    String,
    Integer,
    Boolean,
    Expression
}
