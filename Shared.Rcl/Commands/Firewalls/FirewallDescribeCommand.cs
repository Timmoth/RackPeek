using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Firewalls;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Firewalls;

public class FirewallDescribeCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<FirewallNameSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        FirewallNameSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = serviceProvider.CreateScope();
        DescribeFirewallUseCase useCase = scope.ServiceProvider.GetRequiredService<DescribeFirewallUseCase>();

        FirewallDescription sw = await useCase.ExecuteAsync(settings.Name);

        Grid grid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap());

        grid.AddRow("Name:", sw.Name);
        grid.AddRow("Model:", sw.Model ?? "Unknown");
        grid.AddRow("Managed:", sw.Managed.HasValue ? sw.Managed.Value ? "Yes" : "No" : "Unknown");
        grid.AddRow("PoE:", sw.Poe.HasValue ? sw.Poe.Value ? "Yes" : "No" : "Unknown");
        grid.AddRow("Total Ports:", sw.TotalPorts.ToString());
        grid.AddRow("Total Speed (Gb):", sw.TotalSpeedGb.ToString());
        grid.AddRow("Ports:", sw.PortSummary);

        if (sw.Labels.Count > 0)
            grid.AddRow("Labels:", string.Join(", ", sw.Labels.Select(kvp => $"{kvp.Key}: {kvp.Value}")));

        AnsiConsole.Write(
            new Panel(grid)
                .Header("Firewall")
                .Border(BoxBorder.Rounded));

        return 0;
    }
}
