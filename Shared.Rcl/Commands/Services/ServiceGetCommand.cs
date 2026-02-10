using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Services.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Services;

public class ServiceGetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken)
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
            table.AddRow(
                s.Name,
                s.Ip ?? "",
                s.Port.ToString() ?? "",
                s.Protocol ?? "",
                s.Url ?? "",
                ServicesFormatExtensions.FormatRunsOn(s.RunsOnSystemHost, s.RunsOnPhysicalHost)
            );

        AnsiConsole.Write(table);
        return 0;
    }
}