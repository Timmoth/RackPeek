using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops;

public class DesktopDescribeCommand(IServiceProvider provider)
    : AsyncCommand<DesktopNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DescribeDesktopUseCase>();

        var result = await useCase.ExecuteAsync(settings.Name);

        var grid = new Grid().AddColumn().AddColumn();

        grid.AddRow("Name:", result.Name);
        grid.AddRow("Model:", result.Model ?? "Unknown");
        grid.AddRow("CPUs:", result.CpuCount.ToString());
        grid.AddRow("RAM:", result.RamSummary ?? "None");
        grid.AddRow("Drives:", result.DriveCount.ToString());
        grid.AddRow("NICs:", result.NicCount.ToString());
        grid.AddRow("GPUs:", result.GpuCount.ToString());

        AnsiConsole.Write(new Panel(grid).Header("Desktop").Border(BoxBorder.Rounded));

        return 0;
    }
}