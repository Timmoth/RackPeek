using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Firewalls;

public class FirewallAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}

public class FirewallAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<FirewallAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        FirewallAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddResourceUseCase<Firewall>>();

        await useCase.ExecuteAsync(
            settings.Name
        );

        AnsiConsole.MarkupLine($"[green]Firewall '{settings.Name}' added.[/]");
        return 0;
    }
}