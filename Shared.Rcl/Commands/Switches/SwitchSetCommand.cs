using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Switches;
using Shared.Rcl.Commands.Servers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Switches;

public class SwitchSetSettings : ServerNameSettings
{
    [CommandOption("--Model")] public string Model { get; set; } = default!;

    [CommandOption("--managed")] public bool Managed { get; set; }

    [CommandOption("--poe")] public bool Poe { get; set; }
}

public class SwitchSetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<SwitchSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SwitchSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateSwitchUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Model,
            settings.Managed,
            settings.Poe);

        AnsiConsole.MarkupLine($"[green]Switch '{settings.Name}' updated.[/]");
        return 0;
    }
}