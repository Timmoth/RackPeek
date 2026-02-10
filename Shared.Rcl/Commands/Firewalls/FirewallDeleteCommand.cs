using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Firewalls;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Firewalls;

public class FirewallDeleteCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<FirewallNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        FirewallNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DeleteFirewallUseCase>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Firewall '{settings.Name}' deleted.[/]");
        return 0;
    }
}