using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware;
using Spectre.Console;
using Spectre.Console.Cli;
using Microsoft.Extensions.Logging;
using RackPeek.Domain.Resources.Hardware.Reports;

namespace RackPeek;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        // DI
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        // Application
        services.AddScoped<ServerHardwareReportUseCase>();
        services.AddScoped<ServerReportCommand>();
        services.AddScoped<AccessPointHardwareReportUseCase>();
        services.AddScoped<AccessPointReportCommand>();
        services.AddScoped<SwitchHardwareReportUseCase>();
        services.AddScoped<SwitchReportCommand>();
        services.AddScoped<UpsHardwareReportUseCase>();
        services.AddScoped<UpsReportCommand>();
        services.AddScoped<DesktopHardwareReportUseCase>();
        services.AddScoped<DesktopReportCommand>();

        // Infrastructure
        services.AddScoped<IHardwareRepository>(_ =>
        {
            var path = configuration["HardwareFile"] ?? "hardware.yaml";
            
            var collection = new YamlResourceCollection();
            collection.Load([
                File.ReadAllText("servers.yaml"),
                File.ReadAllText("aps.yaml"),
                File.ReadAllText("desktops.yaml"),
                File.ReadAllText("switches.yaml"),
                File.ReadAllText("ups.yaml"),
                File.ReadAllText("firewalls.yaml"),
                File.ReadAllText("laptops.yaml"),
                File.ReadAllText("routers.yaml")]);

            return new YamlHardwareRepository(collection);
        });
        
        services.AddLogging(configure =>
            configure
                .AddSimpleConsole(opts => { opts.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; }));


        // Spectre bootstrap
        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName("rackpeek");

            config.AddCommand<ServerReportCommand>("servers")
                .WithDescription("Show server hardware report");

            config.AddCommand<AccessPointReportCommand>("ap")
                .WithDescription("Show access point hardware report");
            
            config.AddCommand<DesktopReportCommand>("desktops")
                .WithDescription("Show desktop hardware report");
            
            config.AddCommand<SwitchReportCommand>("switches")
                .WithDescription("Show switch hardware report");
            
            config.AddCommand<UpsReportCommand>("ups")
                .WithDescription("Show ups hardware report");
            
            config.ValidateExamples();
        });

        return await app.RunAsync(args);
    }
}

public class SpectreConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new SpectreConsoleLogger();
    }

    public void Dispose()
    {
    }
}

public class SpectreConsoleLogger : ILogger
{
    public IDisposable BeginScope<T>(T state)
    {
        return null!;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        var message = formatter(state, exception);
        var style = GetStyle(logLevel);

        AnsiConsole.MarkupLine($"[{style}] {message}[/]");
    }

    private Style GetStyle(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => new Style(Color.Grey),
            LogLevel.Debug => new Style(Color.Grey),
            LogLevel.Information => new Style(Color.Green),
            LogLevel.Warning => new Style(Color.Yellow),
            LogLevel.Error => new Style(Color.Red),
            LogLevel.Critical => new Style(Color.Red),
            _ => new Style(Color.White)
        };
    }
}