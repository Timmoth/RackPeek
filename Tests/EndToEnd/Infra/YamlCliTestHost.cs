using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Rcl;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using Xunit.Abstractions;

namespace Tests.EndToEnd.Infra;

public static class YamlCliTestHost {
    public static async Task<string> RunAsync(
        string[] args,
        string hardwarePath,
        ITestOutputHelper output,
        string yamlFile,
        Action<int>? captureExitCode = null) {
        var services = new ServiceCollection();

        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["HardwarePath"] = hardwarePath
            })
            .Build();

        await CliBootstrap.RegisterInternals(services, config, hardwarePath, yamlFile);
        services.AddLogging(builder => {
            builder.ClearProviders();
            builder.AddProvider(new XUnitLoggerProvider(output));
        });

        var console = new TestConsole();

        var registrar = new TypeRegistrar(services.BuildServiceProvider());

        var app = new CommandApp(registrar);
        AnsiConsole.Console = console;
        app.Configure(c => c.Settings.Console = console);

        CliBootstrap.BuildApp(app);

        // Some commands deliberately bypass Spectre and write raw to
        // System.Console.Out (e.g. `graph topology`, which must emit
        // unwrapped Mermaid). Capture that too so tests see the full output.
        TextWriter originalOut = Console.Out;
        var rawCapture = new StringWriter();
        Console.SetOut(rawCapture);

        try {
            var exitCode = await app.RunAsync(args);
            captureExitCode?.Invoke(exitCode);
        }
        finally {
            Console.SetOut(originalOut);
        }

        return console.Output + rawCapture.ToString();
    }
}
