using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops;

public class LaptopReportCommand(
    IServiceProvider serviceProvider
) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        LaptopHardwareReportUseCase useCase = scope.ServiceProvider.GetRequiredService<LaptopHardwareReportUseCase>();

        LaptopHardwareReport report = await useCase.ExecuteAsync();

        if (report.Laptops.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No Laptops found.[/]");
            return 0;
        }

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("CPU")
            .AddColumn("C/T")
            .AddColumn("RAM")
            .AddColumn("Storage")
            .AddColumn("GPU");

        foreach (LaptopHardwareRow d in report.Laptops)
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
