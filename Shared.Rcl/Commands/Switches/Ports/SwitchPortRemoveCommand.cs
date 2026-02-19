using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Switches.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Switches.Ports;

public class SwitchPortRemoveSettings : SwitchNameSettings
{
    [CommandOption("--index <INDEX>")]
    public int Index { get; set; }
}

public class SwitchPortRemoveCommand(IServiceProvider sp)
    : AsyncCommand<SwitchPortRemoveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext ctx, SwitchPortRemoveSettings s, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RemoveSwitchPortUseCase>();

        await useCase.ExecuteAsync(s.Name, s.Index);

        AnsiConsole.MarkupLine($"[green]Port {s.Index} removed from switch '{s.Name}'.[/]");
        return 0;
    }
}