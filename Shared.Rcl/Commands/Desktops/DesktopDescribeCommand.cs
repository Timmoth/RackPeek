using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
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
        using IServiceScope scope = provider.CreateScope();
        DescribeDesktopUseCase useCase = scope.ServiceProvider.GetRequiredService<DescribeDesktopUseCase>();

        DesktopDescription result = await useCase.ExecuteAsync(settings.Name);

        Grid grid = new Grid().AddColumn().AddColumn();

        grid.AddRow("Name:", result.Name);
        grid.AddRow("Model:", result.Model ?? "Unknown");
        grid.AddRow("CPUs:", result.CpuCount.ToString());
        grid.AddRow("RAM:", result.RamSummary ?? "None");
        grid.AddRow("Drives:", result.DriveCount.ToString());
        grid.AddRow("NICs:", result.NicCount.ToString());
        grid.AddRow("GPUs:", result.GpuCount.ToString());

        if (result.Labels.Count > 0)
            grid.AddRow("Labels:", string.Join(", ", result.Labels.Select(kvp => $"{kvp.Key}: {kvp.Value}")));

        AnsiConsole.Write(new Panel(grid).Header("Desktop").Border(BoxBorder.Rounded));

        return 0;
    }
}
