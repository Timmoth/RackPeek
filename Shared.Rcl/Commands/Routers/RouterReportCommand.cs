using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RackPeek.Domain.Resources.Hardware.Routers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Routers;

public class RouterReportCommand(
    ILogger<RouterReportCommand> logger,
    IServiceProvider serviceProvider
) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RouterHardwareReportUseCase>();

        var report = await useCase.ExecuteAsync();

        if (report.Routers.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No Routers found.[/]");
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

        foreach (var s in report.Routers)
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