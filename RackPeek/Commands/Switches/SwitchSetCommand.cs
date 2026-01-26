using Microsoft.Extensions.DependencyInjection;
using RackPeek.Commands.Server;
using RackPeek.Domain.Resources.Hardware.Switches;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Switches;

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

        AnsiConsole.MarkupLine($"[green]Server '{settings.Name}' updated.[/]");
        return 0;
    }
}