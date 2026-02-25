using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Switches.Labels;

public class SwitchLabelRemoveSettings : SwitchNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
}

public class SwitchLabelRemoveCommand(IServiceProvider serviceProvider) : AsyncCommand<SwitchLabelRemoveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SwitchLabelRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveLabelUseCase<Switch>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' removed from '{settings.Name}'.[/]");
        return 0;
    }
}