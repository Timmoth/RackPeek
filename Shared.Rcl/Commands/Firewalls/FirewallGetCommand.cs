using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Firewalls;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Firewalls;

public class FirewallGetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken) {
        using IServiceScope scope = serviceProvider.CreateScope();
        FirewallHardwareReportUseCase useCase =
            scope.ServiceProvider.GetRequiredService<FirewallHardwareReportUseCase>();

        FirewallHardwareReport report = await useCase.ExecuteAsync();

        if (report.Firewalls.Count == 0) {
            AnsiConsole.MarkupLine("[yellow]No Firewalls found.[/]");
            return 0;
        }

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("Model")
            .AddColumn("Managed")
            .AddColumn("PoE")
            .AddColumn("Ports")
            .AddColumn("Port Summary");

        foreach (FirewallHardwareRow s in report.Firewalls)
            table.AddRow(
                s.Name,
                s.Model ?? "Unknown",
                s.Managed ? "[green]yes[/]" : "[red]no[/]",
                s.Poe ? "[green]yes[/]" : "[red]no[/]",
                s.TotalPorts.ToString(),
                s.PortSummary
            );

        AnsiConsole.Write(table);
        return 0;
    }
}
