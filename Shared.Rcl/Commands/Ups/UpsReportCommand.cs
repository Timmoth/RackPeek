using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.UpsUnits;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Ups;

public class UpsReportCommand(
    IServiceProvider serviceProvider
) : AsyncCommand {
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken) {
        using IServiceScope scope = serviceProvider.CreateScope();
        UpsHardwareReportUseCase useCase = scope.ServiceProvider.GetRequiredService<UpsHardwareReportUseCase>();

        UpsHardwareReport report = await useCase.ExecuteAsync();

        if (report.UpsUnits.Count == 0) {
            AnsiConsole.MarkupLine("[yellow]No UPS units found.[/]");
            return 0;
        }

        Table table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("Model")
            .AddColumn("VA");

        foreach (UpsHardwareRow u in report.UpsUnits)
            table.AddRow(
                u.Name,
                u.Model,
                u.Va.ToString()
            );

        AnsiConsole.Write(table);
        return 0;
    }
}
