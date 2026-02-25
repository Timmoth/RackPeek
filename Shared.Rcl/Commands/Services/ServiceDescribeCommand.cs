using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Services.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Services;

public class ServiceDescribeCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServiceNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServiceNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DescribeServiceUseCase>();

        var service = await useCase.ExecuteAsync(settings.Name);

        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap());

        grid.AddRow("Name:", service.Name);
        grid.AddRow("Ip:", service.Ip ?? "Unknown");
        grid.AddRow("Port:", service.Port?.ToString() ?? "Unknown");
        grid.AddRow("Protocol:", service.Protocol ?? "Unknown");
        grid.AddRow("Url:", service.Url ?? "Unknown");
        grid.AddRow("Runs On:",
            ServicesFormatExtensions.FormatRunsOn(string.Join(", ", service.RunsOnSystemHost),
                string.Join(", ", service.RunsOnPhysicalHost)));

        if (service.Labels.Count > 0)
            grid.AddRow("Labels:", string.Join(", ", service.Labels.Select(kvp => $"{kvp.Key}: {kvp.Value}")));

        AnsiConsole.Write(
            new Panel(grid)
                .Header("Service")
                .Border(BoxBorder.Rounded));

        return 0;
    }
}