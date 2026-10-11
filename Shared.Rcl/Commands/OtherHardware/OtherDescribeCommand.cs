using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.OtherHardware;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.OtherHardware;

public class OtherDescribeCommand(IServiceProvider provider)
    : AsyncCommand<OtherNameSettings> {
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        OtherNameSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        DescribeOtherUseCase useCase = scope.ServiceProvider.GetRequiredService<DescribeOtherUseCase>();

        OtherDescription other = await useCase.ExecuteAsync(settings.Name);

        Grid grid = new Grid()
            .AddColumn()
            .AddColumn();

        grid.AddRow("Name:", other.Name.EscapeMarkup());
        grid.AddRow("Model:", (other.Model ?? "Unknown").EscapeMarkup());
        grid.AddRow("Description:", (other.Description ?? "Unknown").EscapeMarkup());
        grid.AddRow("Ports:", other.PortSummary.EscapeMarkup());

        if (other.Labels.Count > 0)
            grid.AddRow("Labels:", string.Join(", ", other.Labels.Select(kvp => $"{kvp.Key.EscapeMarkup()}: {kvp.Value.EscapeMarkup()}")));

        AnsiConsole.Write(new Panel(grid).Header("Other").Border(BoxBorder.Rounded));

        return 0;
    }
}
