
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Switches;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Switches.Ports;

public class SwitchPortAddSettings : SwitchNameSettings
{
    [CommandOption("--type")]
    [Description("The port type (e.g., rj45, sfp+).")]
    public string? Type { get; set; }

    [CommandOption("--speed")]
    [Description("The port speed (e.g., 1, 2.5, 10).")]
    public double? Speed { get; set; }

    [CommandOption("--count")]
    [Description("Number of ports of this type.")]
    public int? Count { get; set; }
}

public class SwitchPortAddCommand(IServiceProvider sp)
    : AsyncCommand<SwitchPortAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext ctx, SwitchPortAddSettings s, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddPortUseCase<Switch>>();

        await useCase.ExecuteAsync(s.Name, s.Type, s.Speed, s.Count);

        AnsiConsole.MarkupLine($"[green]Port added to switch '{s.Name}'.[/]");
        return 0;
    }
}