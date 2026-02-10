using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.AccessPoints;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.AccessPoints;

public class AccessPointDescribeCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<AccessPointNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        AccessPointNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DescribeAccessPointUseCase>();

        var ap = await useCase.ExecuteAsync(settings.Name);

        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap());

        grid.AddRow("Name:", ap.Name);
        grid.AddRow("Model:", ap.Model ?? "Unknown");
        grid.AddRow("Speed (Gbps):", ap.Speed?.ToString() ?? "Unknown");

        AnsiConsole.Write(
            new Panel(grid)
                .Header("Access Point")
                .Border(BoxBorder.Rounded));

        return 0;
    }
}