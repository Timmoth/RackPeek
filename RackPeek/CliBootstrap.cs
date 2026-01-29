using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Commands;
using RackPeek.Commands.AccessPoints;
using RackPeek.Commands.Desktops;
using RackPeek.Commands.Desktops.Cpus;
using RackPeek.Commands.Desktops.Drive;
using RackPeek.Commands.Desktops.Gpus;
using RackPeek.Commands.Desktops.Nics;
using RackPeek.Commands.Servers;
using RackPeek.Commands.Servers.Cpus;
using RackPeek.Commands.Servers.Drives;
using RackPeek.Commands.Servers.Gpus;
using RackPeek.Commands.Servers.Nics;
using RackPeek.Commands.Services;
using RackPeek.Commands.Switches;
using RackPeek.Commands.Systems;
using RackPeek.Commands.Ups;
using RackPeek.Domain;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Yaml;
using Spectre.Console.Cli;

namespace RackPeek;

public static class CliBootstrap
{
    public static void BuildApp(
        CommandApp app,
        IServiceCollection services,
        IConfiguration configuration,
        string yamlDir
    )
    {
        services.AddSingleton(configuration);

        var collection = new YamlResourceCollection();
        var basePath = configuration["HardwarePath"] ?? Directory.GetCurrentDirectory();

        // Resolve yamlDir as relative to basePath
        var yamlPath = Path.IsPathRooted(yamlDir)
            ? yamlDir
            : Path.Combine(basePath, yamlDir);

        if (!Directory.Exists(yamlPath))
            throw new DirectoryNotFoundException(
                $"YAML directory not found: {yamlPath}"
            );

        // Load all .yml and .yaml files
        var yamlFiles = Directory.EnumerateFiles(yamlPath, "*.yml")
            .Concat(Directory.EnumerateFiles(yamlPath, "*.yaml"))
            .ToArray();

        collection.LoadFiles(yamlFiles.Select(f => Path.Combine(basePath, f)));

        // Infrastructure
        services.AddScoped<IHardwareRepository>(_ => new YamlHardwareRepository(collection));
        services.AddScoped<ISystemRepository>(_ => new YamlSystemRepository(collection));
        services.AddScoped<IServiceRepository>(_ => new YamlServiceRepository(collection));

        // Application
        services.AddUseCases();
        services.AddCommands();

        // Spectre bootstrap
        app.Configure(config =>
        {
            config.SetApplicationName("rpk");
            config.ValidateExamples();


            config.AddCommand<GetTotalSummaryCommand>("summary")
                .WithDescription("Show a summarized report for all resources");
            // ----------------------------
            // Server commands (CRUD-style)
            // ----------------------------
            config.AddBranch("servers", server =>
            {
                server.SetDescription("Manage servers");

                server.AddCommand<ServerReportCommand>("summary")
                    .WithDescription("Show a summarized hardware report for all servers");

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

                server.AddCommand<ServerTreeCommand>("tree")
                    .WithDescription("Displays a dependency tree for the server.");

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
            });

            config.AddBranch("switches", switches =>
            {
                switches.SetDescription("Manage switches");

                switches.AddCommand<SwitchReportCommand>("summary")
                    .WithDescription("Show switch hardware report");

                switches.AddCommand<SwitchAddCommand>("add")
                    .WithDescription("Add a new switch");

                switches.AddCommand<SwitchGetCommand>("list")
                    .WithDescription("List switches");

                switches.AddCommand<SwitchGetByNameCommand>("get")
                    .WithDescription("Get a switches by name");

                switches.AddCommand<SwitchDescribeCommand>("describe")
                    .WithDescription("Show detailed information about a switch");

                switches.AddCommand<SwitchSetCommand>("set")
                    .WithDescription("Update switch properties");

                switches.AddCommand<SwitchDeleteCommand>("del")
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

                system.AddCommand<SystemTreeCommand>("tree")
                    .WithDescription("Displays a dependency tree for the system.");
            });

            config.AddBranch("accesspoints", ap =>
            {
                ap.SetDescription("Manage access points");

                ap.AddCommand<AccessPointReportCommand>("summary")
                    .WithDescription("Show access point hardware report");

                ap.AddCommand<AccessPointAddCommand>("add")
                    .WithDescription("Add a new access point");

                ap.AddCommand<AccessPointGetCommand>("list")
                    .WithDescription("List access points");

                ap.AddCommand<AccessPointGetByNameCommand>("get")
                    .WithDescription("Get an access point by name");

                ap.AddCommand<AccessPointDescribeCommand>("describe")
                    .WithDescription("Show detailed information about an access point");

                ap.AddCommand<AccessPointSetCommand>("set")
                    .WithDescription("Update access point properties");

                ap.AddCommand<AccessPointDeleteCommand>("del")
                    .WithDescription("Delete an access point");
            });

            config.AddBranch("ups", ups =>
            {
                ups.SetDescription("Manage UPS units");

                ups.AddCommand<UpsReportCommand>("summary")
                    .WithDescription("Show UPS hardware report");

                ups.AddCommand<UpsAddCommand>("add")
                    .WithDescription("Add a new UPS");

                ups.AddCommand<UpsGetCommand>("list")
                    .WithDescription("List UPS units");

                ups.AddCommand<UpsGetByNameCommand>("get")
                    .WithDescription("Get a UPS by name");

                ups.AddCommand<UpsDescribeCommand>("describe")
                    .WithDescription("Show detailed information about a UPS");

                ups.AddCommand<UpsSetCommand>("set")
                    .WithDescription("Update UPS properties");

                ups.AddCommand<UpsDeleteCommand>("del")
                    .WithDescription("Delete a UPS");
            });

            config.AddBranch("desktops", desktops =>
            {
                // CRUD
                desktops.AddCommand<DesktopAddCommand>("add");
                desktops.AddCommand<DesktopGetCommand>("list");
                desktops.AddCommand<DesktopGetByNameCommand>("get");
                desktops.AddCommand<DesktopDescribeCommand>("describe");
                desktops.AddCommand<DesktopSetCommand>("set");
                desktops.AddCommand<DesktopDeleteCommand>("del");
                desktops.AddCommand<DesktopReportCommand>("summary")
                    .WithDescription("Show desktop hardware report");
                desktops.AddCommand<DesktopTreeCommand>("tree");


                // CPU
                desktops.AddBranch("cpu", cpu =>
                {
                    cpu.AddCommand<DesktopCpuAddCommand>("add");
                    cpu.AddCommand<DesktopCpuSetCommand>("set");
                    cpu.AddCommand<DesktopCpuRemoveCommand>("del");
                });

                // Drives
                desktops.AddBranch("drive", drive =>
                {
                    drive.AddCommand<DesktopDriveAddCommand>("add");
                    drive.AddCommand<DesktopDriveSetCommand>("set");
                    drive.AddCommand<DesktopDriveRemoveCommand>("del");
                });

                // GPUs
                desktops.AddBranch("gpu", gpu =>
                {
                    gpu.AddCommand<DesktopGpuAddCommand>("add");
                    gpu.AddCommand<DesktopGpuSetCommand>("set");
                    gpu.AddCommand<DesktopGpuRemoveCommand>("del");
                });

                // NICs
                desktops.AddBranch("nic", nic =>
                {
                    nic.AddCommand<DesktopNicAddCommand>("add");
                    nic.AddCommand<DesktopNicSetCommand>("set");
                    nic.AddCommand<DesktopNicRemoveCommand>("del");
                });
            });

            config.AddBranch("services", service =>
            {
                service.SetDescription(
                    "Manage services."
                );

                service.AddCommand<ServiceReportCommand>("summary")
                    .WithDescription("Show service summary report");

                service.AddCommand<ServiceAddCommand>("add")
                    .WithDescription("Add a new service");

                service.AddCommand<ServiceGetCommand>("list")
                    .WithDescription("List all services");

                service.AddCommand<ServiceGetByNameCommand>("get")
                    .WithDescription("Get a service by name");

                service.AddCommand<ServiceDescribeCommand>("describe")
                    .WithDescription("Show detailed information about a service");

                service.AddCommand<ServiceSetCommand>("set")
                    .WithDescription("Update service properties");

                service.AddCommand<ServiceDeleteCommand>("del")
                    .WithDescription("Delete a service");

                service.AddCommand<ServiceSubnetsCommand>("subnets")
                    .WithDescription("List service subnets or filter by CIDR");
            });
        });
    }
}