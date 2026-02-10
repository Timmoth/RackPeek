using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.AccessPoints;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.AccessPoints;

public class AccessPointGetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken)
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
            .AddColumn("Speed (Gbps)");

        foreach (var ap in report.AccessPoints)
            table.AddRow(
                ap.Name,
                ap.Model,
                ap.SpeedGb.ToString()
            );


        AnsiConsole.Write(table);
        return 0;
    }
}