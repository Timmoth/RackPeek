using Spectre.Console.Cli;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Firewalls.Ports;
using Spectre.Console;

namespace RackPeek.Commands.Firewalls.Ports;

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
        var useCase = scope.ServiceProvider.GetRequiredService<RemoveFirewallPortUseCase>();

        await useCase.ExecuteAsync(s.Name, s.Index);

        AnsiConsole.MarkupLine($"[green]Port {s.Index} removed from firewall '{s.Name}'.[/]");
        return 0;
    }
}