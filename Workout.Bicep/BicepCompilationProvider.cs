using System.Collections.Immutable;
using System.IO.Abstractions;
using Bicep.Core;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Configuration;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics.Metadata;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Providers;
using Environment = Bicep.Core.Utils.Environment;

namespace Workout.Bicep;

internal sealed class BicepCompilationProvider
{
    private readonly BicepCompiler compiler;
    private readonly IFileSystem fileSystem;

    public BicepCompilationProvider(
        IFileSystem fileSystem,
        IServiceProvider serviceProvider)
    {
        var configurationManager = new ConfigurationManager(fileSystem);
        var featureProviderFactory = new FeatureProviderFactory(configurationManager);
        var tokenCredentialFactory = new TokenCredentialFactory();

        var compiler = new BicepCompiler(
            featureProviderFactory,
            new Environment(),
            new NamespaceProvider(new ResourceTypeProviderFactory(fileSystem)),
            configurationManager,
            new LinterAnalyzer(serviceProvider),
            new FileResolver(fileSystem),
            new ModuleDispatcher(
                new DefaultArtifactRegistryProvider(
                    serviceProvider, 
                    new FileResolver(fileSystem), 
                    fileSystem, 
                    new ContainerRegistryClientFactory(tokenCredentialFactory), new TemplateSpecRepositoryFactory(tokenCredentialFactory), featureProviderFactory, configurationManager),
                configurationManager)
        );

        this.compiler = compiler;
        this.fileSystem = fileSystem;
    }

    public async Task<CompilationResult> CompileAsync(string path)
    {
        var absolutePath = this.fileSystem.Path.GetFullPath(path);
        var compilation = await compiler.CreateCompilation(new Uri(absolutePath), null, true);
        var models = compilation.GetAllBicepModels();
        var model = models.First(); // TODO: Is it safe to always assume a single result?

        return new CompilationResult(model.AllResources, model.Outputs);
    }
}

internal sealed class CompilationResult
{
    private ImmutableArray<ResourceMetadata> allResources;
    private ImmutableArray<OutputMetadata> outputs;

    public CompilationResult(ImmutableArray<ResourceMetadata> allResources, ImmutableArray<OutputMetadata> outputs)
    {
        this.allResources = allResources;
        this.outputs = outputs;
    }

    public ImmutableArray<ResourceMetadata> AllResources => this.allResources;
    public ImmutableArray<OutputMetadata> Outputs => this.outputs;
}