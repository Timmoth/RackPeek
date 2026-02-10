using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RackPeek;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using Xunit.Abstractions;

namespace Tests.EndToEnd.Infra;

public static class YamlCliTestHost
{
    public static async Task<string> RunAsync(
        string[] args,
        string hardwarePath,
        ITestOutputHelper output,
        string yamlFile)
    {
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HardwarePath"] = hardwarePath
            })
            .Build();

        await CliBootstrap.RegisterInternals(services, config, hardwarePath, yamlFile);
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new XUnitLoggerProvider(output));
        });
        
        var console = new TestConsole();

        var registrar = new TypeRegistrar(services.BuildServiceProvider());
        var app = new CommandApp(registrar);

        AnsiConsole.Console = console;
        app.Configure(c => c.Settings.Console = console);

        CliBootstrap.BuildApp(app);
        
        await app.RunAsync(args);

        return console.Output;
    }
}