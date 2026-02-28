using System.ComponentModel.DataAnnotations;
using DocMigrator.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using Shared.Rcl.Commands;
using Shared.Rcl.Commands.AccessPoints;
using Shared.Rcl.Commands.AccessPoints.Labels;
using Shared.Rcl.Commands.Ansible;
using Shared.Rcl.Commands.Desktops;
using Shared.Rcl.Commands.Desktops.Labels;
using Shared.Rcl.Commands.Desktops.Cpus;
using Shared.Rcl.Commands.Desktops.Drive;
using Shared.Rcl.Commands.Desktops.Gpus;
using Shared.Rcl.Commands.Desktops.Nics;
using Shared.Rcl.Commands.Firewalls;
using Shared.Rcl.Commands.Firewalls.Labels;
using Shared.Rcl.Commands.Firewalls.Ports;
using Shared.Rcl.Commands.Laptops;
using Shared.Rcl.Commands.Laptops.Labels;
using Shared.Rcl.Commands.Laptops.Cpus;
using Shared.Rcl.Commands.Laptops.Drive;
using Shared.Rcl.Commands.Laptops.Gpus;
using Shared.Rcl.Commands.Routers;
using Shared.Rcl.Commands.Routers.Labels;
using Shared.Rcl.Commands.Routers.Ports;
using Shared.Rcl.Commands.Servers;
using Shared.Rcl.Commands.Servers.Cpus;
using Shared.Rcl.Commands.Servers.Drives;
using Shared.Rcl.Commands.Servers.Gpus;
using Shared.Rcl.Commands.Servers.Labels;
using Shared.Rcl.Commands.Servers.Nics;
using Shared.Rcl.Commands.Services;
using Shared.Rcl.Commands.Services.Labels;
using Shared.Rcl.Commands.Switches;
using Shared.Rcl.Commands.Switches.Labels;
using Shared.Rcl.Commands.Switches.Ports;
using Shared.Rcl.Commands.Systems;
using Shared.Rcl.Commands.Systems.Labels;
using Shared.Rcl.Commands.Ups;
using Shared.Rcl.Commands.Ups.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl;

public static class CliBootstrap
{
    private static string[]? _lastArgs;
    private static CommandApp? _app;

    private static bool _showingHelp;

    public static void SetContext(string[] args, CommandApp app)
    {
        _lastArgs = args;
        _app = app;
    }

    public static async Task RegisterInternals(
        IServiceCollection services,
        IConfiguration configuration,
        string yamlDir,
        string yamlFile)
    {
        services.AddSingleton(configuration);
        var appBasePath = AppContext.BaseDirectory;

        var resolvedYamlDir = Path.IsPathRooted(yamlDir)
            ? yamlDir
            : Path.Combine(appBasePath, yamlDir);


        Directory.CreateDirectory(resolvedYamlDir);

        var fullYamlPath = Path.Combine(resolvedYamlDir, yamlFile);

        if (!File.Exists(fullYamlPath)) await File.WriteAllTextAsync(fullYamlPath, "");

        services.AddLogging();
        services.AddScoped<RackPeekConfigMigrationDeserializer>();
        services.AddScoped<IResourceYamlMigrationService, ResourceYamlMigrationService>();

        var b = services.BuildServiceProvider();
        
        var collection = new YamlResourceCollection(
            fullYamlPath,
            new PhysicalTextFileStore(),
            new ResourceCollection(),
                    b.GetRequiredService<IResourceYamlMigrationService>());
            

        await collection.LoadAsync();
        services.AddSingleton<IResourceCollection>(collection);

        // Infrastructure
        services.AddYamlRepos();

        // Application
        services.AddUseCases();
        services.AddCommands();
    }

    public static void BuildApp(CommandApp app)
    {
        // Spectre bootstrap
        app.Configure(config =>
        {
            config.SetApplicationName("rpk");
            config.ValidateExamples();
            config.SetApplicationVersion(RpkConstants.Version);

            config.SetExceptionHandler(HandleException);

            // Global summary
            config.AddCommand<GetTotalSummaryCommand>("summary")
                .WithDescription("Show a summarized report of all resources in the system.");

            // ----------------------------
            // Server commands (CRUD-style)
            // ----------------------------
            config.AddBranch("servers", server =>
            {
                server.SetDescription("Manage servers and their components.");

                server.AddCommand<ServerReportCommand>("summary")
                    .WithDescription("Show a summarized hardware report for all servers.");

                server.AddCommand<ServerAddCommand>("add").WithDescription("Add a new server to the inventory.");

                server.AddCommand<ServerGetByNameCommand>("get")
                    .WithDescription("List all servers or retrieve a specific server by name.");

                server.AddCommand<ServerDescribeCommand>("describe")
                    .WithDescription("Display detailed information about a specific server.");

                server.AddCommand<ServerSetCommand>("set").WithDescription("Update properties of an existing server.");

                server.AddCommand<ServerDeleteCommand>("del").WithDescription("Delete a server from the inventory.");

                server.AddCommand<ServerTreeCommand>("tree")
                    .WithDescription("Display the dependency tree of a server.");

                // Server CPUs
                server.AddBranch("cpu", cpu =>
                {
                    cpu.SetDescription("Manage CPUs attached to a server.");

                    cpu.AddCommand<ServerCpuAddCommand>("add").WithDescription("Add a CPU to a specific server.");

                    cpu.AddCommand<ServerCpuSetCommand>("set").WithDescription("Update configuration of a server CPU.");

                    cpu.AddCommand<ServerCpuRemoveCommand>("del").WithDescription("Remove a CPU from a server.");
                });

                // Server Drives
                server.AddBranch("drive", drive =>
                {
                    drive.SetDescription("Manage drives attached to a server.");

                    drive.AddCommand<ServerDriveAddCommand>("add").WithDescription("Add a storage drive to a server.");

                    drive.AddCommand<ServerDriveUpdateCommand>("set")
                        .WithDescription("Update properties of a server drive.");

                    drive.AddCommand<ServerDriveRemoveCommand>("del").WithDescription("Remove a drive from a server.");
                });

                // Server GPUs
                server.AddBranch("gpu", gpu =>
                {
                    gpu.SetDescription("Manage GPUs attached to a server.");

                    gpu.AddCommand<ServerGpuAddCommand>("add").WithDescription("Add a GPU to a server.");

                    gpu.AddCommand<ServerGpuUpdateCommand>("set").WithDescription("Update properties of a server GPU.");

                    gpu.AddCommand<ServerGpuRemoveCommand>("del").WithDescription("Remove a GPU from a server.");
                });

                // Server NICs
                server.AddBranch("nic", nic =>
                {
                    nic.SetDescription("Manage network interface cards (NICs) for a server.");

                    nic.AddCommand<ServerNicAddCommand>("add").WithDescription("Add a NIC to a server.");

                    nic.AddCommand<ServerNicUpdateCommand>("set").WithDescription("Update properties of a server NIC.");

                    nic.AddCommand<ServerNicRemoveCommand>("del").WithDescription("Remove a NIC from a server.");
                });

                // Server Labels
                server.AddBranch("label", label =>
                {
                    label.SetDescription("Manage labels on a server.");

                    label.AddCommand<ServerLabelAddCommand>("add").WithDescription("Add a label to a server.");

                    label.AddCommand<ServerLabelRemoveCommand>("remove").WithDescription("Remove a label from a server.");
                });
            });

            // ----------------------------
            // Switch commands
            // ----------------------------
            config.AddBranch("switches", switches =>
            {
                switches.SetDescription("Manage network switches.");

                switches.AddCommand<SwitchReportCommand>("summary")
                    .WithDescription("Show a hardware report for all switches.");

                switches.AddCommand<SwitchAddCommand>("add")
                    .WithDescription("Add a new network switch to the inventory.");

                switches.AddCommand<SwitchGetCommand>("list").WithDescription("List all switches in the system.");

                switches.AddCommand<SwitchGetByNameCommand>("get")
                    .WithDescription("Retrieve details of a specific switch by name.");

                switches.AddCommand<SwitchDescribeCommand>("describe")
                    .WithDescription("Show detailed information about a switch.");

                switches.AddCommand<SwitchSetCommand>("set").WithDescription("Update properties of a switch.");

                switches.AddCommand<SwitchDeleteCommand>("del").WithDescription("Delete a switch from the inventory.");
                switches.AddBranch("port", port =>
                {
                    port.SetDescription("Manage ports on a network switch.");

                    port.AddCommand<SwitchPortAddCommand>("add").WithDescription("Add a port to a switch.");

                    port.AddCommand<SwitchPortUpdateCommand>("set").WithDescription("Update a switch port.");

                    port.AddCommand<SwitchPortRemoveCommand>("del").WithDescription("Remove a port from a switch.");
                });

                switches.AddBranch("label", label =>
                {
                    label.SetDescription("Manage labels on a switch.");
                    label.AddCommand<SwitchLabelAddCommand>("add").WithDescription("Add a label to a switch.");
                    label.AddCommand<SwitchLabelRemoveCommand>("remove").WithDescription("Remove a label from a switch.");
                });
            });

            // ----------------------------
            // Routers commands
            // ----------------------------
            config.AddBranch("routers", routers =>
            {
                routers.SetDescription("Manage network routers.");

                routers.AddCommand<RouterReportCommand>("summary")
                    .WithDescription("Show a hardware report for all routers.");

                routers.AddCommand<RouterAddCommand>("add")
                    .WithDescription("Add a new network router to the inventory.");

                routers.AddCommand<RouterGetCommand>("list").WithDescription("List all routers in the system.");

                routers.AddCommand<RouterGetByNameCommand>("get")
                    .WithDescription("Retrieve details of a specific router by name.");

                routers.AddCommand<RouterDescribeCommand>("describe")
                    .WithDescription("Show detailed information about a router.");

                routers.AddCommand<RouterSetCommand>("set").WithDescription("Update properties of a router.");

                routers.AddCommand<RouterDeleteCommand>("del").WithDescription("Delete a router from the inventory.");
                routers.AddBranch("port", port =>
                {
                    port.SetDescription("Manage ports on a router.");

                    port.AddCommand<RouterPortAddCommand>("add").WithDescription("Add a port to a router.");

                    port.AddCommand<RouterPortUpdateCommand>("set").WithDescription("Update a router port.");

                    port.AddCommand<RouterPortRemoveCommand>("del").WithDescription("Remove a port from a router.");
                });

                routers.AddBranch("label", label =>
                {
                    label.SetDescription("Manage labels on a router.");
                    label.AddCommand<RouterLabelAddCommand>("add").WithDescription("Add a label to a router.");
                    label.AddCommand<RouterLabelRemoveCommand>("remove").WithDescription("Remove a label from a router.");
                });
            });

            // ----------------------------
            // Firewalls commands
            // ----------------------------
            config.AddBranch("firewalls", firewalls =>
            {
                firewalls.SetDescription("Manage firewalls.");

                firewalls.AddCommand<FirewallReportCommand>("summary")
                    .WithDescription("Show a hardware report for all firewalls.");

                firewalls.AddCommand<FirewallAddCommand>("add").WithDescription("Add a new firewall to the inventory.");

                firewalls.AddCommand<FirewallGetCommand>("list").WithDescription("List all firewalls in the system.");

                firewalls.AddCommand<FirewallGetByNameCommand>("get")
                    .WithDescription("Retrieve details of a specific firewall by name.");

                firewalls.AddCommand<FirewallDescribeCommand>("describe")
                    .WithDescription("Show detailed information about a firewall.");

                firewalls.AddCommand<FirewallSetCommand>("set").WithDescription("Update properties of a firewall.");

                firewalls.AddCommand<FirewallDeleteCommand>("del")
                    .WithDescription("Delete a firewall from the inventory.");
                firewalls.AddBranch("port", port =>
                {
                    port.SetDescription("Manage ports on a firewall.");

                    port.AddCommand<FirewallPortAddCommand>("add").WithDescription("Add a port to a firewall.");

                    port.AddCommand<FirewallPortUpdateCommand>("set").WithDescription("Update a firewall port.");

                    port.AddCommand<FirewallPortRemoveCommand>("del").WithDescription("Remove a port from a firewall.");
                });

                firewalls.AddBranch("label", label =>
                {
                    label.SetDescription("Manage labels on a firewall.");
                    label.AddCommand<FirewallLabelAddCommand>("add").WithDescription("Add a label to a firewall.");
                    label.AddCommand<FirewallLabelRemoveCommand>("remove").WithDescription("Remove a label from a firewall.");
                });
            });

            // ----------------------------
            // System commands
            // ----------------------------
            config.AddBranch("systems", system =>
            {
                system.SetDescription("Manage systems and their dependencies.");

                system.AddCommand<SystemReportCommand>("summary")
                    .WithDescription("Show a summary report for all systems.");

                system.AddCommand<SystemAddCommand>("add").WithDescription("Add a new system to the inventory.");

                system.AddCommand<SystemGetCommand>("list").WithDescription("List all systems.");

                system.AddCommand<SystemGetByNameCommand>("get").WithDescription("Retrieve a system by name.");

                system.AddCommand<SystemDescribeCommand>("describe")
                    .WithDescription("Display detailed information about a system.");

                system.AddCommand<SystemSetCommand>("set").WithDescription("Update properties of a system.");

                system.AddCommand<SystemDeleteCommand>("del").WithDescription("Delete a system from the inventory.");

                system.AddCommand<SystemTreeCommand>("tree")
                    .WithDescription("Display the dependency tree for a system.");

                system.AddBranch("label", label =>
                {
                    label.SetDescription("Manage labels on a system.");
                    label.AddCommand<SystemLabelAddCommand>("add").WithDescription("Add a label to a system.");
                    label.AddCommand<SystemLabelRemoveCommand>("remove").WithDescription("Remove a label from a system.");
                });
            });

            // ----------------------------
            // Access Points
            // ----------------------------
            config.AddBranch("accesspoints", ap =>
            {
                ap.SetDescription("Manage access points.");

                ap.AddCommand<AccessPointReportCommand>("summary")
                    .WithDescription("Show a hardware report for all access points.");

                ap.AddCommand<AccessPointAddCommand>("add").WithDescription("Add a new access point.");

                ap.AddCommand<AccessPointGetCommand>("list").WithDescription("List all access points.");

                ap.AddCommand<AccessPointGetByNameCommand>("get").WithDescription("Retrieve an access point by name.");

                ap.AddCommand<AccessPointDescribeCommand>("describe")
                    .WithDescription("Show detailed information about an access point.");

                ap.AddCommand<AccessPointSetCommand>("set").WithDescription("Update properties of an access point.");

                ap.AddCommand<AccessPointDeleteCommand>("del").WithDescription("Delete an access point.");

                ap.AddBranch("label", label =>
                {
                    label.SetDescription("Manage labels on an access point.");
                    label.AddCommand<AccessPointLabelAddCommand>("add").WithDescription("Add a label to an access point.");
                    label.AddCommand<AccessPointLabelRemoveCommand>("remove").WithDescription("Remove a label from an access point.");
                });
            });

            // ----------------------------
            // UPS units
            // ----------------------------
            config.AddBranch("ups", ups =>
            {
                ups.SetDescription("Manage UPS units.");

                ups.AddCommand<UpsReportCommand>("summary")
                    .WithDescription("Show a hardware report for all UPS units.");

                ups.AddCommand<UpsAddCommand>("add").WithDescription("Add a new UPS unit.");

                ups.AddCommand<UpsGetCommand>("list").WithDescription("List all UPS units.");

                ups.AddCommand<UpsGetByNameCommand>("get").WithDescription("Retrieve a UPS unit by name.");

                ups.AddCommand<UpsDescribeCommand>("describe")
                    .WithDescription("Show detailed information about a UPS unit.");

                ups.AddCommand<UpsSetCommand>("set").WithDescription("Update properties of a UPS unit.");

                ups.AddCommand<UpsDeleteCommand>("del").WithDescription("Delete a UPS unit.");

                ups.AddBranch("label", label =>
                {
                    label.SetDescription("Manage labels on a UPS unit.");
                    label.AddCommand<UpsLabelAddCommand>("add").WithDescription("Add a label to a UPS unit.");
                    label.AddCommand<UpsLabelRemoveCommand>("remove").WithDescription("Remove a label from a UPS unit.");
                });
            });

            // ----------------------------
            // Desktops
            // ----------------------------
            config.AddBranch("desktops", desktops =>
            {
                desktops.SetDescription("Manage desktop computers and their components.");

                // CRUD
                desktops.AddCommand<DesktopAddCommand>("add").WithDescription("Add a new desktop.");
                desktops.AddCommand<DesktopGetCommand>("list").WithDescription("List all desktops.");
                desktops.AddCommand<DesktopGetByNameCommand>("get").WithDescription("Retrieve a desktop by name.");
                desktops.AddCommand<DesktopDescribeCommand>("describe")
                    .WithDescription("Show detailed information about a desktop.");
                desktops.AddCommand<DesktopSetCommand>("set").WithDescription("Update properties of a desktop.");
                desktops.AddCommand<DesktopDeleteCommand>("del")
                    .WithDescription("Delete a desktop from the inventory.");
                desktops.AddCommand<DesktopReportCommand>("summary")
                    .WithDescription("Show a summarized hardware report for all desktops.");
                desktops.AddCommand<DesktopTreeCommand>("tree")
                    .WithDescription("Display the dependency tree for a desktop.");

                // CPU
                desktops.AddBranch("cpu", cpu =>
                {
                    cpu.SetDescription("Manage CPUs attached to desktops.");
                    cpu.AddCommand<DesktopCpuAddCommand>("add").WithDescription("Add a CPU to a desktop.");
                    cpu.AddCommand<DesktopCpuSetCommand>("set").WithDescription("Update a desktop CPU.");
                    cpu.AddCommand<DesktopCpuRemoveCommand>("del").WithDescription("Remove a CPU from a desktop.");
                });

                // Drives
                desktops.AddBranch("drive", drive =>
                {
                    drive.SetDescription("Manage storage drives attached to desktops.");
                    drive.AddCommand<DesktopDriveAddCommand>("add").WithDescription("Add a drive to a desktop.");
                    drive.AddCommand<DesktopDriveSetCommand>("set").WithDescription("Update a desktop drive.");
                    drive.AddCommand<DesktopDriveRemoveCommand>("del")
                        .WithDescription("Remove a drive from a desktop.");
                });

                // GPUs
                desktops.AddBranch("gpu", gpu =>
                {
                    gpu.SetDescription("Manage GPUs attached to desktops.");
                    gpu.AddCommand<DesktopGpuAddCommand>("add").WithDescription("Add a GPU to a desktop.");
                    gpu.AddCommand<DesktopGpuSetCommand>("set").WithDescription("Update a desktop GPU.");
                    gpu.AddCommand<DesktopGpuRemoveCommand>("del").WithDescription("Remove a GPU from a desktop.");
                });

                // NICs
                desktops.AddBranch("nic", nic =>
                {
                    nic.SetDescription("Manage network interface cards (NICs) for desktops.");
                    nic.AddCommand<DesktopNicAddCommand>("add").WithDescription("Add a NIC to a desktop.");
                    nic.AddCommand<DesktopNicSetCommand>("set").WithDescription("Update a desktop NIC.");
                    nic.AddCommand<DesktopNicRemoveCommand>("del").WithDescription("Remove a NIC from a desktop.");
                });

                desktops.AddBranch("label", label =>
                {
                    label.SetDescription("Manage labels on a desktop.");
                    label.AddCommand<DesktopLabelAddCommand>("add").WithDescription("Add a label to a desktop.");
                    label.AddCommand<DesktopLabelRemoveCommand>("remove").WithDescription("Remove a label from a desktop.");
                });
            });

            // ----------------------------
            // Laptops
            // ----------------------------
            config.AddBranch("laptops", laptops =>
            {
                laptops.SetDescription("Manage Laptop computers and their components.");

                // CRUD
                laptops.AddCommand<LaptopAddCommand>("add").WithDescription("Add a new Laptop.");
                laptops.AddCommand<LaptopGetCommand>("list").WithDescription("List all Laptops.");
                laptops.AddCommand<LaptopGetByNameCommand>("get").WithDescription("Retrieve a Laptop by name.");
                laptops.AddCommand<LaptopDescribeCommand>("describe")
                    .WithDescription("Show detailed information about a Laptop.");
                laptops.AddCommand<LaptopSetCommand>("set").WithDescription("Update properties of a laptop.");
                laptops.AddCommand<LaptopDeleteCommand>("del").WithDescription("Delete a Laptop from the inventory.");
                laptops.AddCommand<LaptopReportCommand>("summary")
                    .WithDescription("Show a summarized hardware report for all Laptops.");
                laptops.AddCommand<LaptopTreeCommand>("tree")
                    .WithDescription("Display the dependency tree for a Laptop.");

                // CPU
                laptops.AddBranch("cpu", cpu =>
                {
                    cpu.SetDescription("Manage CPUs attached to Laptops.");
                    cpu.AddCommand<LaptopCpuAddCommand>("add").WithDescription("Add a CPU to a Laptop.");
                    cpu.AddCommand<LaptopCpuSetCommand>("set").WithDescription("Update a Laptop CPU.");
                    cpu.AddCommand<LaptopCpuRemoveCommand>("del").WithDescription("Remove a CPU from a Laptop.");
                });

                // Drives
                laptops.AddBranch("drives", drives =>
                {
                    drives.SetDescription("Manage storage drives attached to Laptops.");
                    drives.AddCommand<LaptopDriveAddCommand>("add").WithDescription("Add a drive to a Laptop.");
                    drives.AddCommand<LaptopDriveSetCommand>("set").WithDescription("Update a Laptop drive.");
                    drives.AddCommand<LaptopDriveRemoveCommand>("del").WithDescription("Remove a drive from a Laptop.");
                });

                // GPUs
                laptops.AddBranch("gpu", gpu =>
                {
                    gpu.SetDescription("Manage GPUs attached to Laptops.");
                    gpu.AddCommand<LaptopGpuAddCommand>("add").WithDescription("Add a GPU to a Laptop.");
                    gpu.AddCommand<LaptopGpuSetCommand>("set").WithDescription("Update a Laptop GPU.");
                    gpu.AddCommand<LaptopGpuRemoveCommand>("del").WithDescription("Remove a GPU from a Laptop.");
                });

                laptops.AddBranch("label", label =>
                {
                    label.SetDescription("Manage labels on a laptop.");
                    label.AddCommand<LaptopLabelAddCommand>("add").WithDescription("Add a label to a laptop.");
                    label.AddCommand<LaptopLabelRemoveCommand>("remove").WithDescription("Remove a label from a laptop.");
                });
            });

            // ----------------------------
            // Services
            // ----------------------------
            config.AddBranch("services", service =>
            {
                service.SetDescription("Manage services and their configurations.");

                service.AddCommand<ServiceReportCommand>("summary")
                    .WithDescription("Show a summary report for all services.");

                service.AddCommand<ServiceAddCommand>("add").WithDescription("Add a new service.");

                service.AddCommand<ServiceGetCommand>("list").WithDescription("List all services.");

                service.AddCommand<ServiceGetByNameCommand>("get").WithDescription("Retrieve a service by name.");

                service.AddCommand<ServiceDescribeCommand>("describe")
                    .WithDescription("Show detailed information about a service.");

                service.AddCommand<ServiceSetCommand>("set").WithDescription("Update properties of a service.");

                service.AddCommand<ServiceDeleteCommand>("del").WithDescription("Delete a service.");

                service.AddCommand<ServiceSubnetsCommand>("subnets")
                    .WithDescription("List subnets associated with a service, optionally filtered by CIDR.");

                service.AddBranch("label", label =>
                {
                    label.SetDescription("Manage labels on a service.");
                    label.AddCommand<ServiceLabelAddCommand>("add").WithDescription("Add a label to a service.");
                    label.AddCommand<ServiceLabelRemoveCommand>("remove").WithDescription("Remove a label from a service.");
                });
            });
            
            // ----------------------------
            // Ansible
            // ----------------------------
            config.AddBranch("ansible", ansible =>
            {
                ansible.SetDescription("Generate and manage Ansible inventory.");

                ansible.AddCommand<GenerateAnsibleInventoryCommand>("inventory")
                    .WithDescription("Generate an Ansible inventory.");
            });
            
        });
    }

    private static int HandleException(Exception ex, Spectre.Console.Cli.ITypeResolver? arg2)
    {
        switch (ex)
        {
            case ValidationException ve:
                AnsiConsole.MarkupLine($"[yellow]Validation error:[/] {ve.Message}");
                return 2;

            case ConflictException ce:
                AnsiConsole.MarkupLine($"[red]Conflict:[/] {ce.Message}");
                return 3;

            case NotFoundException ne:
                AnsiConsole.MarkupLine($"[red]Not found:[/] {ne.Message}");
                return 4;

            case CommandParseException pe:
                if (_showingHelp) return 1; // suppress errors during help lookup
                AnsiConsole.MarkupLine($"[red]Invalid command:[/] {pe.Message}");
                if (pe.Pretty != null) AnsiConsole.Write(pe.Pretty);
                ShowContextualHelp();
                return 1;

            case CommandRuntimeException re:
                if (_showingHelp) return 1;
                AnsiConsole.MarkupLine($"[red]Error:[/] {re.Message}");
                if (re.Pretty != null) AnsiConsole.Write(re.Pretty);
                ShowContextualHelp();
                return 1;

            default:
                AnsiConsole.MarkupLine("[red]Unexpected error occurred.[/]");
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                return 99;
        }
    }

    private static void ShowContextualHelp()
    {
        if (_lastArgs == null || _app == null || _showingHelp) return;

        _showingHelp = true;
        try
        {
            // Extract command path (args before any --flags)
            var commandPath = _lastArgs.TakeWhile(a => !a.StartsWith("-")).ToList();

            // Try progressively shorter command paths until --help succeeds
            while (commandPath.Count > 0)
            {
                var helpArgs = commandPath.Append("--help").ToArray();
                AnsiConsole.WriteLine();
                var result = _app.Run(helpArgs);
                if (result == 0) return;
                commandPath.RemoveAt(commandPath.Count - 1);
            }
        }
        finally
        {
            _showingHelp = false;
        }
    }
}
