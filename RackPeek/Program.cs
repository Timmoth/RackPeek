using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RackPeek.Spectre;
using Spectre.Console.Cli;

namespace RackPeek;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true)
            .Build();

        // DI
        var services = new ServiceCollection();

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);

        await CliBootstrap.BuildApp(app, services, configuration, "./config", "config.yaml");

        services.AddLogging(configure =>
            configure
                .AddSimpleConsole(opts => { opts.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; }));

        return await app.RunAsync(args);
    }
}