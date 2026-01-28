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
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Hardware.AccessPoints;
using RackPeek.Domain.Resources.Hardware.Desktops;
using RackPeek.Domain.Resources.Hardware.Desktops.Cpus;
using RackPeek.Domain.Resources.Hardware.Desktops.Drives;
using RackPeek.Domain.Resources.Hardware.Desktops.Gpus;
using RackPeek.Domain.Resources.Hardware.Desktops.Nics;
using RackPeek.Domain.Resources.Hardware.Reports;
using RackPeek.Domain.Resources.Hardware.Servers;
using RackPeek.Domain.Resources.Hardware.Servers.Cpus;
using RackPeek.Domain.Resources.Hardware.Servers.Drives;
using RackPeek.Domain.Resources.Hardware.Servers.Gpus;
using RackPeek.Domain.Resources.Hardware.Servers.Nics;
using RackPeek.Domain.Resources.Hardware.Switches;
using RackPeek.Domain.Resources.Hardware.UpsUnits;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.Services.UseCases;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using RackPeek.Yaml;
using Spectre.Console.Cli;

namespace RackPeek;

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
        services.AddScoped<IServiceRepository>(_ => new YamlServiceRepository(collection));

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
        services.AddScoped<GetServerSystemTreeUseCase>();
        services.AddScoped<ServerTreeCommand>();

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
        services.AddScoped<GetSystemServiceTreeUseCase>();

        // System commands
        services.AddScoped<SystemSetCommand>();
        services.AddScoped<SystemGetCommand>();
        services.AddScoped<SystemGetByNameCommand>();
        services.AddScoped<SystemDescribeCommand>();
        services.AddScoped<SystemDeleteCommand>();
        services.AddScoped<SystemAddCommand>();
        services.AddScoped<SystemReportCommand>();
        services.AddScoped<SystemTreeCommand>();

        // AccessPoint use cases
        services.AddScoped<AddAccessPointUseCase>();
        services.AddScoped<DeleteAccessPointUseCase>();
        services.AddScoped<GetAccessPointUseCase>();
        services.AddScoped<GetAccessPointsUseCase>();
        services.AddScoped<UpdateAccessPointUseCase>();
        services.AddScoped<DescribeAccessPointUseCase>();

        // AccessPoint commands
        services.AddScoped<AccessPointAddCommand>();
        services.AddScoped<AccessPointDeleteCommand>();
        services.AddScoped<AccessPointDescribeCommand>();
        services.AddScoped<AccessPointGetByNameCommand>();
        services.AddScoped<AccessPointGetCommand>();
        services.AddScoped<AccessPointSetCommand>();

        // UPS use cases
        services.AddScoped<AddUpsUseCase>();
        services.AddScoped<DeleteUpsUseCase>();
        services.AddScoped<GetUpsUnitUseCase>();
        services.AddScoped<GetUpsUseCase>();
        services.AddScoped<UpdateUpsUseCase>();
        services.AddScoped<DescribeUpsUseCase>();

        // UPS commands
        services.AddScoped<UpsAddCommand>();
        services.AddScoped<UpsDeleteCommand>();
        services.AddScoped<UpsDescribeCommand>();
        services.AddScoped<UpsGetByNameCommand>();
        services.AddScoped<UpsGetCommand>();
        services.AddScoped<UpsSetCommand>();

        // Desktop use cases
        services.AddScoped<AddDesktopUseCase>();
        services.AddScoped<DeleteDesktopUseCase>();
        services.AddScoped<DescribeDesktopUseCase>();
        services.AddScoped<GetDesktopUseCase>();
        services.AddScoped<GetDesktopsUseCase>();
        services.AddScoped<UpdateDesktopUseCase>();

// Desktop CPU use cases
        services.AddScoped<AddDesktopCpuUseCase>();
        services.AddScoped<UpdateDesktopCpuUseCase>();
        services.AddScoped<RemoveDesktopCpuUseCase>();

// Desktop Drive use cases
        services.AddScoped<AddDesktopDriveUseCase>();
        services.AddScoped<UpdateDesktopDriveUseCase>();
        services.AddScoped<RemoveDesktopDriveUseCase>();

// Desktop GPU use cases
        services.AddScoped<AddDesktopGpuUseCase>();
        services.AddScoped<UpdateDesktopGpuUseCase>();
        services.AddScoped<RemoveDesktopGpuUseCase>();

// Desktop NIC use cases
        services.AddScoped<AddDesktopNicUseCase>();
        services.AddScoped<UpdateDesktopNicUseCase>();
        services.AddScoped<RemoveDesktopNicUseCase>();

// Desktop CRUD commands
        services.AddScoped<DesktopAddCommand>();
        services.AddScoped<DesktopDeleteCommand>();
        services.AddScoped<DesktopDescribeCommand>();
        services.AddScoped<DesktopGetByNameCommand>();
        services.AddScoped<DesktopGetCommand>();
        services.AddScoped<DesktopSetCommand>();

// Desktop CPU commands
        services.AddScoped<DesktopCpuAddCommand>();
        services.AddScoped<DesktopCpuSetCommand>();
        services.AddScoped<DesktopCpuRemoveCommand>();

// Desktop Drive commands
        services.AddScoped<DesktopDriveAddCommand>();
        services.AddScoped<DesktopDriveSetCommand>();
        services.AddScoped<DesktopDriveRemoveCommand>();

// Desktop GPU commands
        services.AddScoped<DesktopGpuAddCommand>();
        services.AddScoped<DesktopGpuSetCommand>();
        services.AddScoped<DesktopGpuRemoveCommand>();

// Desktop NIC commands
        services.AddScoped<DesktopNicAddCommand>();
        services.AddScoped<DesktopNicSetCommand>();
        services.AddScoped<DesktopNicRemoveCommand>();

        // Service use cases
        services.AddScoped<AddServiceUseCase>();
        services.AddScoped<DeleteServiceUseCase>();
        services.AddScoped<DescribeServiceUseCase>();
        services.AddScoped<GetServiceUseCase>();
        services.AddScoped<GetServiceUseCase>();
        services.AddScoped<UpdateServiceUseCase>();
        services.AddScoped<ServiceReportUseCase>();
        services.AddScoped<ServiceSubnetsUseCase>();

        // Service commands
        services.AddScoped<ServiceSetCommand>();
        services.AddScoped<ServiceGetCommand>();
        services.AddScoped<ServiceGetByNameCommand>();
        services.AddScoped<ServiceDescribeCommand>();
        services.AddScoped<ServiceDeleteCommand>();
        services.AddScoped<ServiceAddCommand>();
        services.AddScoped<ServiceReportCommand>();
        services.AddScoped<ServiceSubnetsCommand>();
        
        // Spectre bootstrap
        app.Configure(config =>
        {
            config.SetApplicationName("rpk");

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