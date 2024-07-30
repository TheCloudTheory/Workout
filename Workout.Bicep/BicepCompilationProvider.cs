using System.IO.Abstractions;
using Bicep.Core;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Configuration;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem.Providers;
using Environment = Bicep.Core.Utils.Environment;

namespace Workout.Bicep;

internal sealed class BicepCompilationProvider
{
    private readonly IFileSystem fileSystem;
    private readonly IServiceProvider serviceProvider;

    public BicepCompilationProvider(
        IFileSystem fileSystem,
        IServiceProvider serviceProvider)
    {
        this.fileSystem = fileSystem;
        this.serviceProvider = serviceProvider;
    }

    public async Task CompileAsync(string path)
    {
        var configurationManager = new ConfigurationManager(this.fileSystem);
        var featureProviderFactory = new FeatureProviderFactory(configurationManager);
        var tokenCredentialFactory = new TokenCredentialFactory();

        var compiler = new BicepCompiler(
            featureProviderFactory,
            new Environment(),
            new NamespaceProvider(new ResourceTypeProviderFactory(this.fileSystem)),
            configurationManager,
            new LinterAnalyzer(this.serviceProvider),
            new FileResolver(this.fileSystem),
            new ModuleDispatcher(
                new DefaultArtifactRegistryProvider(
                    serviceProvider, 
                    new FileResolver(this.fileSystem), 
                    this.fileSystem, 
                    new ContainerRegistryClientFactory(tokenCredentialFactory), new TemplateSpecRepositoryFactory(tokenCredentialFactory), featureProviderFactory, configurationManager),
                configurationManager)
        );

        var absolutePath = this.fileSystem.Path.GetFullPath(path);
        var compilation = await compiler.CreateCompilation(new Uri(absolutePath), null, true);
        var models = compilation.GetAllBicepModels();

        // Return everything required for proper mapping of Bicep models to Workout models
    }
}
