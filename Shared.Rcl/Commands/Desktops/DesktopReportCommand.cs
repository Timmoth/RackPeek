using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RackPeek.Domain.Resources.Hardware.Desktops;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops;

public class DesktopReportCommand(
    ILogger<DesktopReportCommand> logger,
    IServiceProvider serviceProvider
) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DesktopHardwareReportUseCase>();

        var report = await useCase.ExecuteAsync();

        if (report.Desktops.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No desktops found.[/]");
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
            .AddColumn("GPU");

        foreach (var d in report.Desktops)
            table.AddRow(
                d.Name,
                d.CpuSummary,
                $"{d.TotalCores}/{d.TotalThreads}",
                $"{d.RamGb} GB",
                $"{d.TotalStorageGb} GB (SSD {d.SsdStorageGb} / HDD {d.HddStorageGb})",
                d.NicSummary,
                d.GpuSummary
            );

        AnsiConsole.Write(table);
        return 0;
    }
}