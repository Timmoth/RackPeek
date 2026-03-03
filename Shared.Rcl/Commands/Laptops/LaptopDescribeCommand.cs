using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops;

public class LaptopDescribeCommand(IServiceProvider provider)
    : AsyncCommand<LaptopNameSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopNameSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        DescribeLaptopUseCase useCase = scope.ServiceProvider.GetRequiredService<DescribeLaptopUseCase>();

        LaptopDescription result = await useCase.ExecuteAsync(settings.Name);

        Grid grid = new Grid().AddColumn().AddColumn();

        grid.AddRow("Name:", result.Name);
        grid.AddRow("CPUs:", result.CpuCount.ToString());
        grid.AddRow("RAM:", result.RamSummary ?? "None");
        grid.AddRow("Drives:", result.DriveCount.ToString());
        grid.AddRow("GPUs:", result.GpuCount.ToString());

        if (result.Labels.Count > 0)
            grid.AddRow("Labels:", string.Join(", ", result.Labels.Select(kvp => $"{kvp.Key}: {kvp.Value}")));

        AnsiConsole.Write(new Panel(grid).Header("Laptop").Border(BoxBorder.Rounded));

        return 0;
    }
}
