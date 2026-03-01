using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Switches;

public class SwitchAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;

    [CommandOption("--template|-t")]
    [Description("Create from a known hardware template (e.g. UniFi-USW-Enterprise-24).")]
    public string? Template { get; set; }
}

public class SwitchAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<SwitchAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SwitchAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        if (!string.IsNullOrWhiteSpace(settings.Template))
        {
            var templateUseCase = scope.ServiceProvider
                .GetRequiredService<IAddResourceFromTemplateUseCase<Switch>>();
            var templateId = $"Switch/{settings.Template}";
            await templateUseCase.ExecuteAsync(settings.Name, templateId);
        }
        else
        {
            var useCase = scope.ServiceProvider
                .GetRequiredService<IAddResourceUseCase<Switch>>();
            await useCase.ExecuteAsync(settings.Name);
        }

        AnsiConsole.MarkupLine($"[green]Switch '{settings.Name}' added.[/]");
        return 0;
    }
}
