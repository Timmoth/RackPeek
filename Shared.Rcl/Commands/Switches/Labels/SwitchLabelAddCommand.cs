using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Switches.Labels;

public class SwitchLabelAddSettings : SwitchNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
    [CommandOption("--value <VALUE>")] public string Value { get; set; } = default!;
}

public class SwitchLabelAddCommand(IServiceProvider serviceProvider) : AsyncCommand<SwitchLabelAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SwitchLabelAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddLabelUseCase<Switch>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key, settings.Value);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' added to '{settings.Name}'.[/]");
        return 0;
    }
}