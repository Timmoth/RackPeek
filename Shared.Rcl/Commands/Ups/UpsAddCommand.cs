using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Ups;

public class UpsAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;

    [CommandOption("--template|-t")]
    [Description("Create from a known hardware template (e.g. APC-SmartUPS-2200).")]
    public string? Template { get; set; }
}

public class UpsAddCommand(IServiceProvider provider)
    : AsyncCommand<UpsAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpsAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();

        if (!string.IsNullOrWhiteSpace(settings.Template))
        {
            var templateUseCase = scope.ServiceProvider
                .GetRequiredService<IAddResourceFromTemplateUseCase<RackPeek.Domain.Resources.UpsUnits.Ups>>();
            var templateId = $"Ups/{settings.Template}";
            await templateUseCase.ExecuteAsync(settings.Name, templateId);
        }
        else
        {
            var useCase = scope.ServiceProvider
                .GetRequiredService<IAddResourceUseCase<RackPeek.Domain.Resources.UpsUnits.Ups>>();
            await useCase.ExecuteAsync(settings.Name);
        }

        AnsiConsole.MarkupLine($"[green]UPS '{settings.Name}' added.[/]");
        return 0;
    }
}
