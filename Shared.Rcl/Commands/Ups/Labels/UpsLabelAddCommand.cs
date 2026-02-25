using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;
using UpsUnit = RackPeek.Domain.Resources.UpsUnits.Ups;

namespace Shared.Rcl.Commands.Ups.Labels;

public class UpsLabelAddSettings : UpsNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
    [CommandOption("--value <VALUE>")] public string Value { get; set; } = default!;
}

public class UpsLabelAddCommand(IServiceProvider serviceProvider) : AsyncCommand<UpsLabelAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpsLabelAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddLabelUseCase<UpsUnit>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key, settings.Value);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' added to '{settings.Name}'.[/]");
        return 0;
    }
}