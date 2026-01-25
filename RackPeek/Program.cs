using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware;
using Spectre.Console;
using Spectre.Console.Cli;
using Microsoft.Extensions.Logging;
using RackPeek.Commands;
using RackPeek.Commands.Server;
using RackPeek.Commands.Server.Cpus;
using RackPeek.Commands.Server.Nics;
using RackPeek.Domain.Resources.Hardware.Reports;
using RackPeek.Domain.Resources.Hardware.Server;
using RackPeek.Domain.Resources.Hardware.Server.Cpu;
using RackPeek.Domain.Resources.Hardware.Server.Nic;
using RackPeek.Yaml;

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

        // Infrastructure
        services.AddScoped<IHardwareRepository>(_ =>
        {
            var path = configuration["HardwareFile"] ?? "hardware.yaml";
            
            var collection = new YamlResourceCollection();
            collection.LoadFiles([
                "servers.yaml",
                "aps.yaml",
                "desktops.yaml",
                "switches.yaml",
                "ups.yaml",
                "firewalls.yaml",
                "laptops.yaml",
                "routers.yaml"]);

            return new YamlHardwareRepository(collection);
        });
        
        services.AddLogging(configure =>
            configure
                .AddSimpleConsole(opts => { opts.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; }));


        
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
        
        services.AddScoped<AddServerUseCase>();
        services.AddScoped<ServerAddCommand>();
        
        services.AddScoped<DeleteServerUseCase>();
        services.AddScoped<ServerDeleteCommand>();

        services.AddScoped<DescribeServerUseCase>();
        services.AddScoped<ServerDescribeCommand>();
        
        services.AddScoped<GetServerUseCase>();
        services.AddScoped<ServerGetByNameCommand>();
        
        services.AddScoped<UpdateServerUseCase>();
        services.AddScoped<ServerSetCommand>();
        
        // CPU use cases
        services.AddScoped<AddCpuUseCase>();
        services.AddScoped<UpdateCpuUseCase>();
        services.AddScoped<RemoveCpuUseCase>();

        // CPU commands
        services.AddScoped<ServerCpuAddCommand>();
        services.AddScoped<ServerCpuSetCommand>();
        services.AddScoped<ServerCpuRemoveCommand>();
        
        // NIC use cases
        services.AddScoped<AddNicUseCase>();
        services.AddScoped<UpdateNicUseCase>();
        services.AddScoped<RemoveNicUseCase>();
        
        // NIC commands
        services.AddScoped<ServerNicAddCommand>();
        services.AddScoped<ServerNicUpdateCommand>();
        services.AddScoped<ServerNicRemoveCommand>();

        
        // Spectre bootstrap
        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName("rackpeek");

            // ----------------------------
            // Server commands (CRUD-style)
            // ----------------------------
            config.AddBranch("servers", server =>
            {
                server.SetDescription("Manage servers");
                
                server.AddCommand<ServerReportCommand>("summary")
                    .WithDescription("Show server hardware report");
                
                server.AddCommand<ServerAddCommand>("add")
                    .WithDescription("Add a new server");

                server.AddCommand<ServerGetByNameCommand>("get")
                    .WithDescription("List servers or get a server by name");

                server.AddCommand<ServerDescribeCommand>("describe")
                    .WithDescription("Show detailed information about a server");

                server.AddCommand<ServerSetCommand>("set")
                    .WithDescription("Update server properties");

                server.AddCommand<ServerDeleteCommand>("del")
                    .WithDescription("Delete a server");
                
                server.AddBranch("cpu", cpu =>
                {
                    cpu.SetDescription("Manage server CPUs");

                    cpu.AddCommand<ServerCpuAddCommand>("add")
                        .WithDescription("Add a CPU to a server");

                    cpu.AddCommand<ServerCpuSetCommand>("set")
                        .WithDescription("Update a CPU on a server");

                    cpu.AddCommand<ServerCpuRemoveCommand>("del")
                        .WithDescription("Remove a CPU from a server");
                });
                
                server.AddBranch("nic", nic =>
                {
                    nic.SetDescription("Manage server NICs");

                    nic.AddCommand<ServerNicAddCommand>("add")
                        .WithDescription("Add a NIC to a server");

                    nic.AddCommand<ServerNicUpdateCommand>("set")
                        .WithDescription("Update a NIC on a server");

                    nic.AddCommand<ServerNicRemoveCommand>("del")
                        .WithDescription("Remove a NIC from a server");
                });

            });

            // ----------------------------
            // Reports (read-only summaries)
            // ----------------------------
            config.AddCommand<AccessPointReportCommand>("ap")
                .WithDescription("Show access point hardware report");

            config.AddCommand<DesktopReportCommand>("desktops")
                .WithDescription("Show desktop hardware report");

            config.AddCommand<SwitchReportCommand>("switches")
                .WithDescription("Show switch hardware report");

            config.AddCommand<UpsReportCommand>("ups")
                .WithDescription("Show UPS hardware report");

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