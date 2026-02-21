using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Firewalls;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Firewalls;

public class FirewallGetByNameCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<FirewallNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        FirewallNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DescribeFirewallUseCase>();

        var sw = await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine(
            $"[green]{sw.Name}[/]  Model: {sw.Model ?? "Unknown"}, Managed: {(sw.Managed == true ? "Yes" : "No")}, PoE: {(sw.Poe == true ? "Yes" : "No")}");

        return 0;
    }
}