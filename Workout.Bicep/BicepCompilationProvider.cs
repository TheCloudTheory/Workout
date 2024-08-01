﻿using System.IO.Abstractions;
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

    public async Task CompileAsync(string path)
    {
        var absolutePath = this.fileSystem.Path.GetFullPath(path);
        var compilation = await compiler.CreateCompilation(new Uri(absolutePath), null, true);
        var models = compilation.GetAllBicepModels();

        // Return everything required for proper mapping of Bicep models to Workout models
    }
}
