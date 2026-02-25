using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.AccessPoints.Labels;

public class AccessPointLabelRemoveSettings : AccessPointNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
}

public class AccessPointLabelRemoveCommand(IServiceProvider serviceProvider)
    : AsyncCommand<AccessPointLabelRemoveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AccessPointLabelRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveLabelUseCase<AccessPoint>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' removed from '{settings.Name}'.[/]");
        return 0;
    }
}