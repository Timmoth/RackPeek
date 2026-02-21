using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Firewalls;
using Shared.Rcl.Commands.Servers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Firewalls;

public class FirewallSetSettings : ServerNameSettings
{
    [CommandOption("--Model")] public string Model { get; set; } = default!;

    [CommandOption("--managed")] public bool Managed { get; set; }

    [CommandOption("--poe")] public bool Poe { get; set; }
}

public class FirewallSetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<FirewallSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        FirewallSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateFirewallUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Model,
            settings.Managed,
            settings.Poe);

        AnsiConsole.MarkupLine($"[green]Firewall '{settings.Name}' updated.[/]");
        return 0;
    }
}