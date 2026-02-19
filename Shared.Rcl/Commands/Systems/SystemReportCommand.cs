using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Systems;

public class SystemReportCommand(
    ILogger<SystemReportCommand> logger,
    IServiceProvider serviceProvider
) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<SystemReportUseCase>();

        var report = await useCase.ExecuteAsync();

        if (report.Systems.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No systems found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("Type")
            .AddColumn("OS")
            .AddColumn("Cores")
            .AddColumn("RAM (GB)")
            .AddColumn("Storage (GB)")
            .AddColumn("Runs On");

        foreach (var s in report.Systems)
            table.AddRow(
                s.Name,
                s.Type ?? "Unknown",
                s.Os ?? "Unknown",
                s.Cores.ToString(),
                s.RamGb.ToString(),
                s.TotalStorageGb.ToString(),
                string.Join(", ", s.RunsOn) ?? "Unknown"
            );

        AnsiConsole.Write(table);
        return 0;
    }
}
