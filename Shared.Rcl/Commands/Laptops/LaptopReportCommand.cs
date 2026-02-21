using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RackPeek.Domain.Resources.Laptops;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops;

public class LaptopReportCommand(
    ILogger<LaptopReportCommand> logger,
    IServiceProvider serviceProvider
) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<LaptopHardwareReportUseCase>();

        var report = await useCase.ExecuteAsync();

        if (report.Laptops.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No Laptops found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("CPU")
            .AddColumn("C/T")
            .AddColumn("RAM")
            .AddColumn("Storage")
            .AddColumn("GPU");

        foreach (var d in report.Laptops)
            table.AddRow(
                d.Name,
                d.CpuSummary,
                $"{d.TotalCores}/{d.TotalThreads}",
                $"{d.RamGb} GB",
                $"{d.TotalStorageGb} GB (SSD {d.SsdStorageGb} / HDD {d.HddStorageGb})",
                d.GpuSummary
            );

        AnsiConsole.Write(table);
        return 0;
    }
}