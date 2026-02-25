using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;
using UpsUnit = RackPeek.Domain.Resources.UpsUnits.Ups;

namespace Shared.Rcl.Commands.Ups.Labels;

public class UpsLabelRemoveSettings : UpsNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
}

public class UpsLabelRemoveCommand(IServiceProvider serviceProvider) : AsyncCommand<UpsLabelRemoveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpsLabelRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveLabelUseCase<UpsUnit>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' removed from '{settings.Name}'.[/]");
        return 0;
    }
}