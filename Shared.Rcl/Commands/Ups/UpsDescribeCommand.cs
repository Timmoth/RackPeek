using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.UpsUnits;
using Shared.Rcl.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Ups;

public class UpsDescribeCommand(IServiceProvider provider)
    : AsyncCommand<UpsNameSettings> {
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        UpsNameSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        DescribeUpsUseCase useCase = scope.ServiceProvider.GetRequiredService<DescribeUpsUseCase>();

        UpsDescription ups = await useCase.ExecuteAsync(settings.Name);

        Grid grid = new Grid()
            .AddColumn()
            .AddColumn();

        grid.AddRow("Name:", ups.Name.EscapeMarkup());
        grid.AddRow("Model:", (ups.Model ?? "Unknown").EscapeMarkup());
        grid.AddRow("VA:", ups.Va?.ToString() ?? "Unknown");
        grid.AddRow("Ports:", ups.PortSummary.EscapeMarkup());

        if (ups.Labels.Count > 0)
            grid.AddRow("Labels:", string.Join(", ", ups.Labels.Select(kvp => $"{kvp.Key.EscapeMarkup()}: {kvp.Value.EscapeMarkup()}")));

        AnsiConsole.Write(new Panel(grid).Header("UPS").Border(BoxBorder.Rounded));

        return 0;
    }
}
