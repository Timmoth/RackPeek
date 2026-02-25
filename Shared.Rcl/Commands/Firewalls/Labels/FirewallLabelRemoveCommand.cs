using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Firewalls.Labels;

public class FirewallLabelRemoveSettings : FirewallNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
}

public class FirewallLabelRemoveCommand(IServiceProvider serviceProvider) : AsyncCommand<FirewallLabelRemoveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, FirewallLabelRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveLabelUseCase<Firewall>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' removed from '{settings.Name}'.[/]");
        return 0;
    }
}