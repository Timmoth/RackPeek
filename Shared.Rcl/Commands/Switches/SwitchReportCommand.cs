using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Switches;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Switches;

public class SwitchReportCommand(
    IServiceProvider serviceProvider
) : AsyncCommand {
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken) {
        using IServiceScope scope = serviceProvider.CreateScope();
        SwitchHardwareReportUseCase useCase = scope.ServiceProvider.GetRequiredService<SwitchHardwareReportUseCase>();

        SwitchHardwareReport report = await useCase.ExecuteAsync();

        if (report.Switches.Count == 0) {
            AnsiConsole.MarkupLine("[yellow]No switches found.[/]");
            return 0;
        }

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("Model")
            .AddColumn("Managed")
            .AddColumn("PoE")
            .AddColumn("Ports")
            .AddColumn("Max Speed")
            .AddColumn("Port Summary");

        foreach (SwitchHardwareRow s in report.Switches)
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
