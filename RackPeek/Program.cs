using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Rcl;
using Spectre.Console.Cli;

namespace RackPeek;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configuration
        var appBasePath = AppContext.BaseDirectory;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(appBasePath)
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", true)
            .Build();

        var yamlDir = configuration.GetValue<string>("RPK_YAML_DIR") ?? "config";

        // DI
        var services = new ServiceCollection();
        await CliBootstrap.RegisterInternals(services, configuration, yamlDir, "config.yaml");

        services.AddLogging(configure =>
            configure.AddSimpleConsole(opts => { opts.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; }));

        var registrar = new TypeRegistrar(services.BuildServiceProvider());
        var app = new CommandApp(registrar);

        CliBootstrap.BuildApp(app);
        CliBootstrap.SetContext(args, app);

        return await app.RunAsync(args);
    }
}