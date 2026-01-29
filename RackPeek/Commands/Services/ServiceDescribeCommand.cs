using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Services.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Services;

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

        if (service == null)
        {
            AnsiConsole.MarkupLine($"[red]Service '{settings.Name}' not found.[/]");
            return 1;
        }

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
            ServicesFormatExtensions.FormatRunsOn(service.RunsOnSystemHost, service.RunsOnPhysicalHost));

        AnsiConsole.Write(
            new Panel(grid)
                .Header("Service")
                .Border(BoxBorder.Rounded));

        return 0;
    }
}