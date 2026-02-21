using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Firewalls.Ports;

public class FirewallPortRemoveSettings : FirewallNameSettings
{
    [CommandOption("--index <INDEX>")] public int Index { get; set; }
}

public class FirewallPortRemoveCommand(IServiceProvider sp)
    : AsyncCommand<FirewallPortRemoveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext ctx, FirewallPortRemoveSettings s, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemovePortUseCase<Firewall>>();

        await useCase.ExecuteAsync(s.Name, s.Index);

        AnsiConsole.MarkupLine($"[green]Port {s.Index} removed from firewall '{s.Name}'.[/]");
        return 0;
    }
}