using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Firewalls.Ports;

public class FirewallPortAddSettings : FirewallNameSettings
{
    [CommandOption("--type")] public string? Type { get; set; }
    [CommandOption("--speed")] public double? Speed { get; set; }
    [CommandOption("--count")] public int? Count { get; set; }
}

public class FirewallPortAddCommand(IServiceProvider sp)
    : AsyncCommand<FirewallPortAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext ctx, FirewallPortAddSettings s, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddPortUseCase<Firewall>>();

        await useCase.ExecuteAsync(s.Name, s.Type, s.Speed, s.Count);

        AnsiConsole.MarkupLine($"[green]Port added to firewall '{s.Name}'.[/]");
        return 0;
    }
}