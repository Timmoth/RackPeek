using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.AccessPoints;

public class AccessPointAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    [Description("The access point name.")]
    public string Name { get; set; } = default!;

    [CommandOption("--template|-t")]
    [Description("Create from a known hardware template (e.g. UniFi-U6-Pro).")]
    public string? Template { get; set; }
}

public class AccessPointAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<AccessPointAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        AccessPointAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        if (!string.IsNullOrWhiteSpace(settings.Template))
        {
            var templateUseCase = scope.ServiceProvider
                .GetRequiredService<IAddResourceFromTemplateUseCase<AccessPoint>>();
            var templateId = $"AccessPoint/{settings.Template}";
            await templateUseCase.ExecuteAsync(settings.Name, templateId);
        }
        else
        {
            var useCase = scope.ServiceProvider
                .GetRequiredService<IAddResourceUseCase<AccessPoint>>();
            await useCase.ExecuteAsync(settings.Name);
        }

        AnsiConsole.MarkupLine($"[green]Access Point '{settings.Name}' added.[/]");
        return 0;
    }
}
