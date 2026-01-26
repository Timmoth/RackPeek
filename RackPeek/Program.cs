using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RackPeek.Commands;
using RackPeek.Commands.Server;
using RackPeek.Commands.Server.Cpus;
using RackPeek.Commands.Server.Drives;
using RackPeek.Commands.Server.Gpu;
using RackPeek.Commands.Server.Nics;
using RackPeek.Commands.Switches;
using RackPeek.Commands.Systems;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.Reports;
using RackPeek.Domain.Resources.Hardware.Server;
using RackPeek.Domain.Resources.Hardware.Server.Cpu;
using RackPeek.Domain.Resources.Hardware.Server.Drive;
using RackPeek.Domain.Resources.Hardware.Server.Gpu;
using RackPeek.Domain.Resources.Hardware.Server.Nic;
using RackPeek.Domain.Resources.Hardware.Switches;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using RackPeek.Spectre;
using RackPeek.Yaml;
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
        
        CliBootstrap.BuildApp(app, services, configuration, [
            "servers.yaml",
            "desktops.yaml",
            "switches.yaml",
            "ups.yaml",
            "firewalls.yaml",
            "laptops.yaml",
            "routers.yaml"
        ]);
        
        services.AddLogging(configure =>
            configure
                .AddSimpleConsole(opts => { opts.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; }));
        
        return await app.RunAsync(args);
    }
}

public static class CliBootstrap
{
    public static void BuildApp(
        CommandApp app,
        IServiceCollection services, 
        IConfiguration configuration,
        string[] yamlFiles
        )
    {
        services.AddSingleton<IConfiguration>(configuration);

        var collection = new YamlResourceCollection();
        var basePath = configuration["HardwarePath"] ?? Directory.GetCurrentDirectory();

        collection.LoadFiles(yamlFiles.Select(f => Path.Combine(basePath, f)));
        
        // Infrastructure
        services.AddScoped<IHardwareRepository>(_ => new YamlHardwareRepository(collection));
        services.AddScoped<ISystemRepository>(_ => new YamlSystemRepository(collection));

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

        // Drive use cases
        services.AddScoped<AddDrivesUseCase>();
        services.AddScoped<UpdateDriveUseCase>();
        services.AddScoped<RemoveDriveUseCase>();
        
        // GPU use cases
        services.AddScoped<AddGpuUseCase>();
        services.AddScoped<UpdateGpuUseCase>();
        services.AddScoped<RemoveGpuUseCase>();


        // CPU commands
        services.AddScoped<ServerCpuAddCommand>();
        services.AddScoped<ServerCpuSetCommand>();
        services.AddScoped<ServerCpuRemoveCommand>();

        // Switch commands
        services.AddScoped<SwitchAddCommand>();
        services.AddScoped<SwitchDeleteCommand>();
        services.AddScoped<SwitchDescribeCommand>();
        services.AddScoped<SwitchGetByNameCommand>();
        services.AddScoped<SwitchGetCommand>();
        services.AddScoped<SwitchSetCommand>();

        // Switch Usecases
        services.AddScoped<AddSwitchUseCase>();
        services.AddScoped<DeleteSwitchUseCase>();
        services.AddScoped<GetSwitchUseCase>();
        services.AddScoped<GetSwitchesUseCase>();
        services.AddScoped<UpdateSwitchUseCase>();
        services.AddScoped<DescribeSwitchUseCase>();

        // NIC use cases
        services.AddScoped<AddNicUseCase>();
        services.AddScoped<UpdateNicUseCase>();
        services.AddScoped<RemoveNicUseCase>();

        // NIC commands
        services.AddScoped<ServerNicAddCommand>();
        services.AddScoped<ServerNicUpdateCommand>();
        services.AddScoped<ServerNicRemoveCommand>();
        
        // Drive commands
        services.AddScoped<ServerDriveAddCommand>();
        services.AddScoped<ServerDriveUpdateCommand>();
        services.AddScoped<ServerDriveRemoveCommand>();
        
        // GPU commands
        services.AddScoped<ServerGpuAddCommand>();
        services.AddScoped<ServerGpuUpdateCommand>();
        services.AddScoped<ServerGpuRemoveCommand>();

        // System use cases
        services.AddScoped<AddSystemUseCase>();
        services.AddScoped<DeleteSystemUseCase>();
        services.AddScoped<DescribeSystemUseCase>();
        services.AddScoped<GetSystemsUseCase>();
        services.AddScoped<GetSystemUseCase>();
        services.AddScoped<UpdateSystemUseCase>();
        services.AddScoped<SystemReportUseCase>();


        // System commands
        services.AddScoped<SystemSetCommand>();
        services.AddScoped<SystemGetCommand>();
        services.AddScoped<SystemGetByNameCommand>();
        services.AddScoped<SystemDescribeCommand>();
        services.AddScoped<SystemDeleteCommand>();
        services.AddScoped<SystemAddCommand>();
        services.AddScoped<SystemReportCommand>();

        
        
        // Spectre bootstrap
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
                    server.AddBranch("drive", drive =>
                    {
                        drive.SetDescription("Manage server drives");

                        drive.AddCommand<ServerDriveAddCommand>("add")
                            .WithDescription("Add a drive to a server");

                        drive.AddCommand<ServerDriveUpdateCommand>("set")
                            .WithDescription("Update a drive on a server");

                        drive.AddCommand<ServerDriveRemoveCommand>("del")
                            .WithDescription("Remove a drive from a server");
                    });
                    server.AddBranch("gpu", gpu =>
                    {
                        gpu.SetDescription("Manage server GPUs");

                        gpu.AddCommand<ServerGpuAddCommand>("add")
                            .WithDescription("Add a GPU to a server");

                        gpu.AddCommand<ServerGpuUpdateCommand>("set")
                            .WithDescription("Update a GPU on a server");

                        gpu.AddCommand<ServerGpuRemoveCommand>("del")
                            .WithDescription("Remove a GPU from a server");
                    });

                });

                config.AddBranch("switches", server =>
                {
                    server.SetDescription("Manage switches");

                    server.AddCommand<SwitchReportCommand>("summary")
                        .WithDescription("Show switch hardware report");

                    server.AddCommand<SwitchAddCommand>("add")
                        .WithDescription("Add a new switch");

                    server.AddCommand<SwitchGetCommand>("list")
                        .WithDescription("List switches");
                    
                    server.AddCommand<SwitchGetByNameCommand>("get")
                        .WithDescription("Get a switches by name");

                    server.AddCommand<SwitchDescribeCommand>("describe")
                        .WithDescription("Show detailed information about a switch");

                    server.AddCommand<SwitchSetCommand>("set")
                        .WithDescription("Update switch properties");

                    server.AddCommand<SwitchDeleteCommand>("del")
                        .WithDescription("Delete a switch");
                });

                config.AddBranch("systems", system =>
                {
                    system.SetDescription("Manage systems");

                    system.AddCommand<SystemReportCommand>("summary")
                        .WithDescription("Show system report");

                    system.AddCommand<SystemAddCommand>("add")
                        .WithDescription("Add a new system");

                    system.AddCommand<SystemGetCommand>("list")
                        .WithDescription("List systems");
                    
                    system.AddCommand<SystemGetByNameCommand>("get")
                        .WithDescription("Get a system by name");

                    system.AddCommand<SystemDescribeCommand>("describe")
                        .WithDescription("Show detailed information about a system");

                    system.AddCommand<SystemSetCommand>("set")
                        .WithDescription("Update system properties");

                    system.AddCommand<SystemDeleteCommand>("del")
                        .WithDescription("Delete a system");
                });
                
                // ----------------------------
                // Reports (read-only summaries)
                // ----------------------------
                config.AddCommand<AccessPointReportCommand>("ap")
                    .WithDescription("Show access point hardware report");

                config.AddCommand<DesktopReportCommand>("desktops")
                    .WithDescription("Show desktop hardware report");

                config.AddCommand<UpsReportCommand>("ups")
                    .WithDescription("Show UPS hardware report");

                config.ValidateExamples();
            });
        });
    }
}
