using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers;

public class ServerAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;

    [CommandOption("--template|-t")]
    [Description("Create from a known hardware template (e.g. Intel-NUC-13-Pro).")]
    public string? Template { get; set; }
}

public class ServerAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServerAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        if (!string.IsNullOrWhiteSpace(settings.Template))
        {
            var templateUseCase = scope.ServiceProvider
                .GetRequiredService<IAddResourceFromTemplateUseCase<Server>>();
            var templateId = $"Server/{settings.Template}";
            await templateUseCase.ExecuteAsync(settings.Name, templateId);
        }
        else
        {
            var useCase = scope.ServiceProvider
                .GetRequiredService<IAddResourceUseCase<Server>>();
            await useCase.ExecuteAsync(settings.Name);
        }

        AnsiConsole.MarkupLine($"[green]Server '{settings.Name}' added.[/]");
        return 0;
    }
}
