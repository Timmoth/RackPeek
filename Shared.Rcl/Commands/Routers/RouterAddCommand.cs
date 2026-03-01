using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Routers;

public class RouterAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;

    [CommandOption("--template|-t")]
    [Description("Create from a known hardware template (e.g. Ubiquiti-ER-4).")]
    public string? Template { get; set; }
}

public class RouterAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<RouterAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        RouterAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        if (!string.IsNullOrWhiteSpace(settings.Template))
        {
            var templateUseCase = scope.ServiceProvider
                .GetRequiredService<IAddResourceFromTemplateUseCase<Router>>();
            var templateId = $"Router/{settings.Template}";
            await templateUseCase.ExecuteAsync(settings.Name, templateId);
        }
        else
        {
            var useCase = scope.ServiceProvider
                .GetRequiredService<IAddResourceUseCase<Router>>();
            await useCase.ExecuteAsync(settings.Name);
        }

        AnsiConsole.MarkupLine($"[green]Router '{settings.Name}' added.[/]");
        return 0;
    }
}
