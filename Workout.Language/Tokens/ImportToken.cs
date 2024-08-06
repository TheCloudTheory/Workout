using Workout.Bicep;
using Workout.Cli.Internals.Logging;

namespace Workout.Language.Tokens;

internal sealed record ImportToken : Token
{
    private readonly string workingDirectory;
    private readonly BicepCompilationProvider provider;
    private readonly ILogger logger;

    public ImportToken(int line, string value, string workingDirectory, BicepCompilationProvider provider, ILogger logger) 
        : base(line, value, TokenType.Import)
    {
        this.workingDirectory = workingDirectory;
        this.provider = provider;
        this.logger = logger;
    }

    public void Validate(List<Error> validationErrors)
    {
        if (this.Value is null)
        {
            validationErrors.Add(Errors.Error_NullImportPath(this.Line, 0));
            return;
        }

        var importPath = Path.Combine(this.workingDirectory, this.Value).Replace("'", string.Empty); // Remove single quotes as they are not needed
        if(File.Exists(importPath) == false)
        {
            this.logger.LogDebug($"Failed to validate import path: {importPath}");
            validationErrors.Add(Errors.Error_InvalidImportPath(this.Line, 0));
            return;
        }
    }   

    public async Task<CompilationResult> GetCompiledImport()
    {
        var importPath = Path.Combine(this.workingDirectory, this.Value!).Replace("'", string.Empty);

        this.logger.LogDebug($"Compiling import: {importPath} for {this.workingDirectory}.");
        var result = await this.provider.CompileAsync(importPath);

        return result;
    }
}
