using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Laptops;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops;

public class LaptopDescribeCommand(IServiceProvider provider)
    : AsyncCommand<LaptopNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DescribeLaptopUseCase>();

        var result = await useCase.ExecuteAsync(settings.Name);

        var grid = new Grid().AddColumn().AddColumn();

        grid.AddRow("Name:", result.Name);
        grid.AddRow("CPUs:", result.CpuCount.ToString());
        grid.AddRow("RAM:", result.RamSummary ?? "None");
        grid.AddRow("Drives:", result.DriveCount.ToString());
        grid.AddRow("GPUs:", result.GpuCount.ToString());

        AnsiConsole.Write(new Panel(grid).Header("Laptop").Border(BoxBorder.Rounded));

        return 0;
    }
}