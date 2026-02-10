using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Switches;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Switches;

public class SwitchAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}

public class SwitchAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<SwitchAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SwitchAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AddSwitchUseCase>();

        await useCase.ExecuteAsync(
            settings.Name
        );

        AnsiConsole.MarkupLine($"[green]Switch '{settings.Name}' added.[/]");
        return 0;
    }
}