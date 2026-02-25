using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Firewalls.Labels;

public class FirewallLabelAddSettings : FirewallNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
    [CommandOption("--value <VALUE>")] public string Value { get; set; } = default!;
}

public class FirewallLabelAddCommand(IServiceProvider serviceProvider) : AsyncCommand<FirewallLabelAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, FirewallLabelAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddLabelUseCase<Firewall>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key, settings.Value);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' added to '{settings.Name}'.[/]");
        return 0;
    }
}