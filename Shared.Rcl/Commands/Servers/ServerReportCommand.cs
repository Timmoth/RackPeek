using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Servers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers;

public class ServerReportCommand(IServiceProvider serviceProvider)
    : AsyncCommand {
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken) {
        using IServiceScope scope = serviceProvider.CreateScope();
        ServerHardwareReportUseCase useCase = scope.ServiceProvider.GetRequiredService<ServerHardwareReportUseCase>();

        ServerHardwareReport report = await useCase.ExecuteAsync();

        if (report.Servers.Count == 0) {
            AnsiConsole.MarkupLine("[yellow]No servers found.[/]");
            return 0;
        }

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("CPU")
            .AddColumn("C/T")
            .AddColumn("RAM")
            .AddColumn("Storage")
            .AddColumn("NICs")
            .AddColumn("GPUs")
            .AddColumn("IPMI");

        foreach (ServerHardwareRow s in report.Servers)
            table.AddRow(
                s.Name,
                s.CpuSummary,
                $"{s.TotalCores}/{s.TotalThreads}",
                $"{s.RamGb} GB",
                $"{s.TotalStorageGb} GB (SSD {s.SsdStorageGb} / HDD {s.HddStorageGb})",
                s.NicSummary,
                s.GpuCount == 0
                    ? "[grey]none[/]"
                    : $"{s.GpuSummary} ({s.TotalGpuVramGb} GB VRAM)",
                s.Ipmi ? "[green]yes[/]" : "[red]no[/]"
            );

        AnsiConsole.Write(table);
        return 0;
    }
}
