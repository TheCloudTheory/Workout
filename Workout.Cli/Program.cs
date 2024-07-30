using Bicep.Core.Registry.PublicRegistry;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Workout.Cli.Commands;
using Workout.Cli.Infrastructure;
using Workout.Cli.Internals.Logging;

internal class Program
{
    internal static int Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger, Logger>();

        // TODO: Move that to Workout.Bicep
        services.AddSingleton<IPublicRegistryModuleMetadataProvider, PublicRegistryModuleMetadataProvider>();

        var logger = services.BuildServiceProvider().GetService<ILogger>() ?? throw new InvalidOperationException("Logger is not registered");
        logger.LogInformation("Starting application");

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.AddBranch("start", start => {
                start.AddCommand<StartWorkoutCommand>("workout");
            });
        });

        return app.Run(args);
    }
}