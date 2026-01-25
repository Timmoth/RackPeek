using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RackPeek.Domain.Resources.Hardware.Reports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek;

public class AccessPointReportCommand(
    ILogger<AccessPointReportCommand> logger,
    IServiceProvider serviceProvider
) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AccessPointHardwareReportUseCase>();

        var report = await useCase.ExecuteAsync();

        if (report.AccessPoints.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No access points found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("Model")
            .AddColumn("Speed (Gb)");

        foreach (var ap in report.AccessPoints)
        {
            table.AddRow(
                ap.Name,
                ap.Model,
                $"{ap.SpeedGb}G"
            );
        }

        AnsiConsole.Write(table);
        return 0;
    }
}