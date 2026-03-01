using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Firewalls;

public class FirewallAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;

    [CommandOption("--template|-t")]
    [Description("Create from a known hardware template (e.g. Netgate-6100).")]
    public string? Template { get; set; }
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

        if (!string.IsNullOrWhiteSpace(settings.Template))
        {
            var templateUseCase = scope.ServiceProvider
                .GetRequiredService<IAddResourceFromTemplateUseCase<Firewall>>();
            var templateId = $"Firewall/{settings.Template}";
            await templateUseCase.ExecuteAsync(settings.Name, templateId);
        }
        else
        {
            var useCase = scope.ServiceProvider
                .GetRequiredService<IAddResourceUseCase<Firewall>>();
            await useCase.ExecuteAsync(settings.Name);
        }

        AnsiConsole.MarkupLine($"[green]Firewall '{settings.Name}' added.[/]");
        return 0;
    }
}
