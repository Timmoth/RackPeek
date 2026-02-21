using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RackPeek.Domain.Resources.UpsUnits;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Ups;

public class UpsReportCommand(
    ILogger<UpsReportCommand> logger,
    IServiceProvider serviceProvider
) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpsHardwareReportUseCase>();

        var report = await useCase.ExecuteAsync();

        if (report.UpsUnits.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No UPS units found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("Model")
            .AddColumn("VA");

        foreach (var u in report.UpsUnits)
            table.AddRow(
                u.Name,
                u.Model,
                u.Va.ToString()
            );

        AnsiConsole.Write(table);
        return 0;
    }
}