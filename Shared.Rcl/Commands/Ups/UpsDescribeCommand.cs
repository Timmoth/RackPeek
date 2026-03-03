using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.UpsUnits;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Ups;

public class UpsDescribeCommand(IServiceProvider provider)
    : AsyncCommand<UpsNameSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpsNameSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        DescribeUpsUseCase useCase = scope.ServiceProvider.GetRequiredService<DescribeUpsUseCase>();

        UpsDescription ups = await useCase.ExecuteAsync(settings.Name);

        Grid grid = new Grid()
            .AddColumn()
            .AddColumn();

        grid.AddRow("Name:", ups.Name);
        grid.AddRow("Model:", ups.Model ?? "Unknown");
        grid.AddRow("VA:", ups.Va?.ToString() ?? "Unknown");

        if (ups.Labels.Count > 0)
            grid.AddRow("Labels:", string.Join(", ", ups.Labels.Select(kvp => $"{kvp.Key}: {kvp.Value}")));

        AnsiConsole.Write(new Panel(grid).Header("UPS").Border(BoxBorder.Rounded));

        return 0;
    }
}
