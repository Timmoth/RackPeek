using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RackPeek.Domain.Resources.Services.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Services;

public class ServiceReportCommand(
    ILogger<ServiceReportCommand> logger,
    IServiceProvider serviceProvider
) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ServiceReportUseCase>();

        var report = await useCase.ExecuteAsync();

        if (report.Services.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No Services found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("Ip")
            .AddColumn("Port")
            .AddColumn("Protocol")
            .AddColumn("Url")
            .AddColumn("Runs On");

        foreach (var s in report.Services)
        {
            string? sys = null;
            string? phys = null;

            if (s.RunsOnSystemHost?.Count > 0) sys = string.Join(", ", s.RunsOnSystemHost);
            if (s.RunsOnPhysicalHost?.Count > 0) phys = string.Join(", ", s.RunsOnPhysicalHost);

            table.AddRow(
                s.Name,
                s.Ip ?? "",
                s.Port.ToString() ?? "",
                s.Protocol ?? "",
                s.Url ?? "",
                ServicesFormatExtensions.FormatRunsOn(sys, phys)
            );
        }

        AnsiConsole.Write(table);
        return 0;
    }
}