using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Firewalls.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Firewalls.Ports;

public class FirewallPortUpdateSettings : FirewallNameSettings
{
    [CommandOption("--index <INDEX>")] public int Index { get; set; }
    [CommandOption("--type")] public string? Type { get; set; }
    [CommandOption("--speed")] public double? Speed { get; set; }
    [CommandOption("--count")] public int? Count { get; set; }
}

public class FirewallPortUpdateCommand(IServiceProvider sp)
    : AsyncCommand<FirewallPortUpdateSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext ctx, FirewallPortUpdateSettings s, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateFirewallPortUseCase>();

        await useCase.ExecuteAsync(s.Name, s.Index, s.Type, s.Speed, s.Count);

        AnsiConsole.MarkupLine($"[green]Port {s.Index} updated on firewall '{s.Name}'.[/]");
        return 0;
    }
}