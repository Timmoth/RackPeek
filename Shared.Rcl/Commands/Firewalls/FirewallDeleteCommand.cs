using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Firewalls;

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
        var useCase = scope.ServiceProvider.GetRequiredService<IDeleteResourceUseCase<Firewall>>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Firewall '{settings.Name}' deleted.[/]");
        return 0;
    }
}