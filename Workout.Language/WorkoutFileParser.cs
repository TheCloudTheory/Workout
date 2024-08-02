using Workout.Bicep;
using Workout.Cli.Internals.Logging;
using Workout.Language.Tokens;

namespace Workout.Language;

internal sealed class WorkoutFileParser
{
    private readonly string workingDirectory;
    private readonly BicepCompilationProvider provider;
    private readonly ILogger logger;

    public WorkoutFileParser(
        string workingDirectory,
        BicepCompilationProvider provider,
        ILogger logger
    )
    {
        this.workingDirectory = workingDirectory;
        this.provider = provider;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<TestModel>> Parse(string filePath)
    {
        var tokens = new List<Token>();
        var fileContent = File.ReadAllText(filePath);
        var lines = fileContent.Split(Environment.NewLine);
        var blockedOpened = false;

        for (var i = 0; i <= lines.Length - 1; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;
            line = line.Trim();

            if (line.StartsWith("import"))
            {
                var importPath = line.Split(" ")[1];
                tokens.Add(new ImportToken(lineNumber, importPath));
            }

            if (line.StartsWith("@smoke"))
            {
                tokens.Add(new SmokeTestDecoratorToken(lineNumber, "@smoke"));
            }

            if (line.StartsWith("test"))
            {
                blockedOpened = true;
                var testName = line.Split(" ")[1];
                tokens.Add(new TestToken(lineNumber, testName));
            }

            if (line.StartsWith("}"))
            {
                tokens.Add(new EndOfBlockToken(lineNumber, "}"));
                blockedOpened = false;
            }

            if (blockedOpened)
            {
                if (line.StartsWith("assert"))
                {
                    var expression = line.Replace("assert(", string.Empty).Replace(")", string.Empty);
                    if (tokens.Last() is not TestToken previousToken)
                    {
                        this.logger.LogError($"Error: Assertion found without a test block at line {lineNumber}.");
                        continue;
                    }

                    previousToken.AddAssertion(new AssertionToken(lineNumber, expression));
                }
            }
        }

        ValidateTokens(tokens);
        var tests = await ExtractTests(tokens);

        return tests;
    }

    // TODO: Validation should be delegated to each token
    private void ValidateTokens(List<Token> tokens)
    {
        var errors = new List<Error>();

        if(tokens.Any(_ => _.Type == TokenType.Import) == false)
        {
            errors.Add(Errors.Error_NoImportFound(0, 0));
        }

        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Import:
                    if (token.Value is null)
                    {
                        errors.Add(Errors.Error_NullImportPath(token.Line, 0));
                        break;
                    }

                    var importPath = Path.Combine(this.workingDirectory, token.Value).Replace("'", string.Empty); // Remove single quotes as they are not needed
                    if(File.Exists(importPath) == false)
                    {
                        this.logger.LogDebug($"Failed to validate import path: {importPath}");
                        errors.Add(Errors.Error_InvalidImportPath(token.Line, 0));
                        break;
                    }

                    break;
                case TokenType.Test:
                    var testToken = token as TestToken;

                    foreach(var childToken in testToken!.Assertions)
                    {
                        if(childToken.Type != TokenType.Assertion)
                        {
                            errors.Add(Errors.Error_InvalidToken(token.Value!, childToken.Line, 0));
                        }

                        ValidateAssertion(childToken, errors);
                    }
                    break;
                case TokenType.SmokeTestDecorator:
                    break;
                case TokenType.EndOfBlock:
                    break;
                default:
                    errors.Add(Errors.Error_InvalidToken(token.Value!, token.Line, 0));
                    break;
            }
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                this.logger.LogError($"Error {error.Code} at line {error.Line}, column {error.Column}: {error.Message}");
            }
        }
    }

    private void ValidateAssertion(Token childToken, List<Error> errors)
    {
        var expression = childToken.Value!;
        var parts = expression.Split("==");

        if (parts.Length != 2)
        {
            errors.Add(Errors.Error_InvalidToken(expression, childToken.Line, 0));
        }
    }

    private async Task<IReadOnlyList<TestModel>> ExtractTests(List<Token> tokens)
    {
        var tests = new List<TestModel>();
        var imports = tokens.Where(_ => _.Type == TokenType.Import).ToList();

        foreach(var import in imports)
        {
            var importPath = Path.Combine(this.workingDirectory, import.Value!).Replace("'", string.Empty);

            this.logger.LogDebug($"Compiling import: {importPath} for {this.workingDirectory}.");
            var result = await this.provider.CompileAsync(importPath);
        }

        foreach (var token in tokens)
        {
            if (token.Type == TokenType.Test)
            {
                var testToken = token as TestToken;
                tests.Add(new TestModel(token.Value!, testToken!.Assertions.Select(_ => _.Value!).ToArray()));
            }
        }

        return tests;
    }
}
