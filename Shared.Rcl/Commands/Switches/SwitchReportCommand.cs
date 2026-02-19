using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RackPeek.Domain.Resources.Hardware.Switches;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Switches;

public class SwitchReportCommand(
    ILogger<SwitchReportCommand> logger,
    IServiceProvider serviceProvider
) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<SwitchHardwareReportUseCase>();

        var report = await useCase.ExecuteAsync();

        if (report.Switches.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No switches found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("Model")
            .AddColumn("Managed")
            .AddColumn("PoE")
            .AddColumn("Ports")
            .AddColumn("Max Speed")
            .AddColumn("Port Summary");

        foreach (var s in report.Switches)
            table.AddRow(
                s.Name,
                s.Model,
                s.Managed ? "[green]yes[/]" : "[red]no[/]",
                s.Poe ? "[green]yes[/]" : "[red]no[/]",
                s.TotalPorts.ToString(),
                $"{s.MaxPortSpeedGb}G",
                s.PortSummary
            );

        AnsiConsole.Write(table);
        return 0;
    }
}