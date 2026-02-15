using Spectre.Console.Cli;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Routers.Ports;
using Spectre.Console;

namespace RackPeek.Commands.Routers.Ports;

public class RouterPortAddSettings : RouterNameSettings
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

public class RouterPortAddCommand(IServiceProvider sp)
    : AsyncCommand<RouterPortAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext ctx, RouterPortAddSettings s, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AddRouterPortUseCase>();

        await useCase.ExecuteAsync(s.Name, s.Type, s.Speed, s.Count);

        AnsiConsole.MarkupLine($"[green]Port added to router '{s.Name}'.[/]");
        return 0;
    }
}