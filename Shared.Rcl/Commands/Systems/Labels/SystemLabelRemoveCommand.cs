using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Systems.Labels;

public class SystemLabelRemoveSettings : SystemNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
}

public class SystemLabelRemoveCommand(IServiceProvider serviceProvider) : AsyncCommand<SystemLabelRemoveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SystemLabelRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveLabelUseCase<SystemResource>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' removed from '{settings.Name}'.[/]");
        return 0;
    }
}