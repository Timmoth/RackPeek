using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Crud;
using RackPeek.Domain.Resources.Hardware.Reports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands;

public class ServerNameSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    public string Name { get; set; } = default!;
}

public class ServerAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    public string Name { get; set; } = default!;

    [CommandOption("--cpu <MODEL>")]
    public string? CpuModel { get; set; } = default!;

    [CommandOption("--cores <CORES>")]
    public int? Cores { get; set; }

    [CommandOption("--threads <THREADS>")]
    public int? Threads { get; set; }

    [CommandOption("--ram <GB>")]
    public int? RamGb { get; set; }

    [CommandOption("--ipmi")]
    public bool? Ipmi { get; set; }
}

public class ServerAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServerAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AddServerUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.CpuModel,
            settings.Cores,
            settings.Threads,
            settings.RamGb,
            settings.Ipmi
        );

        AnsiConsole.MarkupLine($"[green]Server '{settings.Name}' added.[/]");
        return 0;
    }
}

public class ServerGetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ServerHardwareReportUseCase>();

        var report = await useCase.ExecuteAsync();

        if (report.Servers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No servers found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("CPU")
            .AddColumn("C/T")
            .AddColumn("RAM")
            .AddColumn("Storage")
            .AddColumn("NICs")
            .AddColumn("IPMI");

        foreach (var s in report.Servers)
        {
            table.AddRow(
                s.Name,
                s.CpuSummary,
                $"{s.TotalCores}/{s.TotalThreads}",
                $"{s.RamGb} GB",
                $"{s.TotalStorageGb} GB",
                $"{s.TotalNicPorts}×{s.MaxNicSpeedGb}G",
                s.Ipmi ? "[green]yes[/]" : "[red]no[/]"
            );
        }

        AnsiConsole.Write(table);
        return 0;
    }
}

public class ServerGetByNameCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServerNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<GetServerUseCase>();

        var server = await useCase.ExecuteAsync(settings.Name);

        if (server == null)
        {
            AnsiConsole.MarkupLine($"[red]Server '{settings.Name}' not found.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine(
            $"[green]{server.Name}[/]  RAM: {server.Ram?.Size} GB, IPMI: {(server.Ipmi == true ? "yes" : "no")}");

        return 0;
    }
}

public class ServerDescribeCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServerNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<GetServerUseCase>();

        var server = await useCase.ExecuteAsync(settings.Name);

        if (server == null)
        {
            AnsiConsole.MarkupLine($"[red]Server '{settings.Name}' not found.[/]");
            return 1;
        }

        var grid = new Grid()
            .AddColumn()
            .AddColumn();

        grid.AddRow("Name", server.Name);
        grid.AddRow("IPMI", server.Ipmi == true ? "yes" : "no");
        grid.AddRow("RAM", $"{server.Ram?.Size ?? 0} GB");

        if (server.Cpus != null)
        {
            foreach (var cpu in server.Cpus)
                grid.AddRow("CPU", $"{cpu.Model} ({cpu.Cores}/{cpu.Threads})");
        }

        AnsiConsole.Write(
            new Panel(grid)
                .Header("Server")
                .Border(BoxBorder.Rounded));

        return 0;
    }
}

public class ServerSetSettings : ServerNameSettings
{
    [CommandOption("--cpu <MODEL>")]
    public string CpuModel { get; set; } = default!;

    [CommandOption("--cores <CORES>")]
    public int Cores { get; set; }

    [CommandOption("--threads <THREADS>")]
    public int Threads { get; set; }

    [CommandOption("--ram <GB>")]
    public int RamGb { get; set; }

    [CommandOption("--ipmi")]
    public bool Ipmi { get; set; }
}

public class ServerSetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServerSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateServerUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.RamGb,
            settings.Ipmi,
            settings.CpuModel,
            settings.Cores,
            settings.Threads);

        AnsiConsole.MarkupLine($"[green]Server '{settings.Name}' updated.[/]");
        return 0;
    }
}

public class ServerDeleteCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServerNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DeleteServerUseCase>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Server '{settings.Name}' deleted.[/]");
        return 0;
    }
}



