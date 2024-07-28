namespace Workout.Language;

internal sealed class WorkoutFileParser
{
    // Change this to a strongly-typed list
    private readonly List<Tuple<TokenType, string?>> tokens = [];

    public void Parse(string filePath)
    {
        var fileContent = File.ReadAllText(filePath);
        var lines = fileContent.Split(Environment.NewLine);
        var blockedOpened = false;

        foreach (var line in lines)
        {
            var cleanedLine = line.Replace("\t", ""); // Remove tabs

            if(line.StartsWith("import"))
            {
                var importPath = line.Split(" ")[1];
                this.tokens.Add(new Tuple<TokenType, string?>(TokenType.Import, importPath));
            }

            if(line.StartsWith("@smoke"))
            {
                this.tokens.Add(new Tuple<TokenType, string?>(TokenType.SmokeTestDecorator, null));
            }

            if(line.StartsWith("test"))
            {
                blockedOpened = true;
                var testName = line.Split(" ")[1];
                this.tokens.Add(new Tuple<TokenType, string?>(TokenType.Test, testName));
            }

            if(line.StartsWith("}"))
            {
                this.tokens.Add(new Tuple<TokenType, string?>(TokenType.EndOfBlock, null));
                blockedOpened = false;
            }

            if(blockedOpened)
            {
                if(line.StartsWith("assert"))
                {
                    var expression = line.Replace("assert(", string.Empty).Replace(")", string.Empty);
                    this.tokens.Add(new Tuple<TokenType, string?>(TokenType.Assertion, expression));
                }
            }
        }

        ValidateTokens();
    }

    private void ValidateTokens()
    {
        var errors = new List<Error>();
        foreach(var token in this.tokens)
        {
            switch(token.Item1)
            {
                case TokenType.Import:
                    if(token.Item2 is null)
                    {
                        errors.Add(Errors.Error_NullImportPath(0, 0));
                    }
                    break;
                case TokenType.Test:
                    if(token.Item2 is null)
                    {
                        throw new Exception("Test name is required.");
                    }
                    break;
                case TokenType.Assertion:
                    if(token.Item2 is null)
                    {
                        throw new Exception("Assertion expression is required.");
                    }
                    break;
            }
        }
    }
}
