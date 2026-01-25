using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Reports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Server;

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