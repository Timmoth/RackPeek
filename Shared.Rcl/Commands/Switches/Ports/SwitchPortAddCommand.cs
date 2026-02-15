using Spectre.Console.Cli;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Switches.Ports;
using Spectre.Console;

namespace RackPeek.Commands.Switches.Ports;

public class SwitchPortAddSettings : SwitchNameSettings
{
    [CommandOption("--type")]
    [Description("Port type: rj45, sfp, sfp+, sfp28, sfp56, qsfp+, qsfp28, qsfp56, qsfp-dd, osfp, xfp, cx4, mgmt.")]
    public string? Type { get; set; }

    [CommandOption("--speed")]
    [Description("Port speed in Gb/s (e.g. 1, 2.5, 10, 25).")]
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
        var useCase = scope.ServiceProvider.GetRequiredService<AddSwitchPortUseCase>();

        await useCase.ExecuteAsync(s.Name, s.Type, s.Speed, s.Count);

        AnsiConsole.MarkupLine($"[green]Port added to switch '{s.Name}'.[/]");
        return 0;
    }
}