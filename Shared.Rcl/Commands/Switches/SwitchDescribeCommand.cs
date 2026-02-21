using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Switches;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Switches;

public class SwitchDescribeCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<SwitchNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SwitchNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DescribeSwitchUseCase>();

        var sw = await useCase.ExecuteAsync(settings.Name);

        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap());

        grid.AddRow("Name:", sw.Name);
        grid.AddRow("Model:", sw.Model ?? "Unknown");
        grid.AddRow("Managed:", sw.Managed.HasValue ? sw.Managed.Value ? "Yes" : "No" : "Unknown");
        grid.AddRow("PoE:", sw.Poe.HasValue ? sw.Poe.Value ? "Yes" : "No" : "Unknown");
        grid.AddRow("Total Ports:", sw.TotalPorts.ToString());
        grid.AddRow("Total Speed (Gb):", sw.TotalSpeedGb.ToString());
        grid.AddRow("Ports:", sw.PortSummary);

        AnsiConsole.Write(
            new Panel(grid)
                .Header("Switch")
                .Border(BoxBorder.Rounded));

        return 0;
    }
}