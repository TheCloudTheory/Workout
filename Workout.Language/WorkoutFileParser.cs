using Workout.Bicep;
using Workout.Cli.Internals.Logging;

namespace Workout.Language;

internal sealed class WorkoutFileParser
{
    private readonly List<Token> tokens = [];
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

    public async Task Parse(string filePath)
    {
        var fileContent = File.ReadAllText(filePath);
        var lines = fileContent.Split(Environment.NewLine);
        var blockedOpened = false;

        for (var i = 0; i <= lines.Length - 1; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;
            line = line.Replace("\t", ""); // Remove tabs

            if (line.StartsWith("import"))
            {
                var importPath = line.Split(" ")[1];
                this.tokens.Add(new Token(TokenType.Import, lineNumber, importPath));
            }

            if (line.StartsWith("@smoke"))
            {
                this.tokens.Add(new Token(TokenType.SmokeTestDecorator, lineNumber, "@smoke"));
            }

            if (line.StartsWith("test"))
            {
                blockedOpened = true;
                var testName = line.Split(" ")[1];
                this.tokens.Add(new Token(TokenType.Test, lineNumber, testName));
            }

            if (line.StartsWith("}"))
            {
                this.tokens.Add(new Token(TokenType.EndOfBlock, lineNumber, "}"));
                blockedOpened = false;
            }

            if (blockedOpened)
            {
                if (line.StartsWith("assert"))
                {
                    var expression = line.Replace("assert(", string.Empty).Replace(")", string.Empty);
                    var previousToken = this.tokens.Last();
                    previousToken.AddToken(new Token(TokenType.Assertion, lineNumber, expression));
                }
            }
        }

        await ValidateTokens();
    }

    private async Task ValidateTokens()
    {
        var errors = new List<Error>();

        if(this.tokens.Any(_ => _.Type == TokenType.Import) == false)
        {
            errors.Add(Errors.Error_NoImportFound(0, 0));
        }

        foreach (var token in this.tokens)
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
                    await this.provider.CompileAsync(importPath);
                    if(File.Exists(importPath) == false)
                    {
                        this.logger.LogDebug($"Failed to validate import path: {importPath}");
                        errors.Add(Errors.Error_InvalidImportPath(token.Line, 0));
                        break;
                    }

                    break;
                case TokenType.Test:
                    foreach(var childToken in token.Tokens)
                    {
                        if(childToken.Type != TokenType.Assertion)
                        {
                            errors.Add(Errors.Error_InvalidToken(token.Value!, childToken.Line, 0));
                        }
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
}
