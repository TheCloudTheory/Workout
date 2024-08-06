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
        var imports = new List<ImportToken>();

        for (var i = 0; i <= lines.Length - 1; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;

            if (line.StartsWith("import"))
            {
                var importPath = line.Split(" ")[1];
                var importToken = new ImportToken(lineNumber, importPath, this.workingDirectory, this.provider, this.logger);
                
                tokens.Add(importToken);
                imports.Add(importToken);
            }
        }

        var compiledImports = await Task.WhenAll(imports.Select(async x => await x.GetCompiledImport()));

        for (var i = 0; i <= lines.Length - 1; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;
            line = line.Trim();

            if (line.StartsWith("@smoke"))
            {
                tokens.Add(new SmokeTestDecoratorToken(lineNumber, "@smoke"));
            }

            if (line.StartsWith("test"))
            {
                this.logger.LogDebug($"Found test block at line {lineNumber}.");

                blockedOpened = true;
                var testName = line.Split(" ")[1];
                tokens.Add(new TestToken(lineNumber, testName));
            }

            if (line.StartsWith("}"))
            {
                tokens.Add(new EndOfBlockToken(lineNumber, "}"));
                blockedOpened = false;

                this.logger.LogDebug($"Closed block at line {lineNumber}.");
            }

            if (blockedOpened)
            {
                this.logger.LogDebug("Block is opened.");

                if (line.StartsWith("equals"))
                {
                    this.logger.LogDebug($"Found assertion (equals) at line {lineNumber}.");

                    if (tokens.Last() is not TestToken previousToken)
                    {
                        this.logger.LogError($"Error: Assertion found without a test block at line {lineNumber}.");
                        continue;
                    }

                    previousToken.AddAssertion(new AssertionToken(lineNumber, line, compiledImports));
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
        this.logger.LogDebug("Validating tokens.");

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
                    ((ImportToken)token).Validate(errors);
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

    private void ValidateAssertion(AssertionToken childToken, List<Error> errors)
    {
        var rawAssertion = childToken.Value!.Trim();

        if(rawAssertion.StartsWith("equals"))
        {
            return;
        }

        errors.Add(Errors.Error_InvalidAssertion(rawAssertion, childToken.Line, 0));
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
                tests.Add(new TestModel(token.Value!, [.. testToken!.Assertions]));
            }
        }

        return tests;
    }
}
