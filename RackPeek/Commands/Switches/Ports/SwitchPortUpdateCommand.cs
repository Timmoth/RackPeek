using Spectre.Console.Cli;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Switches.Ports;
using Spectre.Console;

namespace RackPeek.Commands.Switches.Ports;

public class SwitchPortUpdateSettings : SwitchNameSettings
{
    [CommandOption("--index <INDEX>")]
    public int Index { get; set; }

    [CommandOption("--type")]
    public string? Type { get; set; }

    [CommandOption("--speed")]
    public double? Speed { get; set; }

    [CommandOption("--count")]
    public int? Count { get; set; }
}

public class SwitchPortUpdateCommand(IServiceProvider sp)
    : AsyncCommand<SwitchPortUpdateSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext ctx, SwitchPortUpdateSettings s, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateSwitchPortUseCase>();

        await useCase.ExecuteAsync(s.Name, s.Index, s.Type, s.Speed, s.Count);

        AnsiConsole.MarkupLine($"[green]Port {s.Index} updated on switch '{s.Name}'.[/]");
        return 0;
    }
}