using Spectre.Console.Cli;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Routers.Ports;
using Spectre.Console;

namespace RackPeek.Commands.Routers.Ports;

public class RouterPortUpdateSettings : RouterNameSettings
{
    [CommandOption("--index <INDEX>")]
    [Description("Index of the port to update.")]
    public int Index { get; set; }

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

public class RouterPortUpdateCommand(IServiceProvider sp)
    : AsyncCommand<RouterPortUpdateSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext ctx, RouterPortUpdateSettings s, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateRouterPortUseCase>();

        await useCase.ExecuteAsync(s.Name, s.Index, s.Type, s.Speed, s.Count);

        AnsiConsole.MarkupLine($"[green]Port {s.Index} updated on router '{s.Name}'.[/]");
        return 0;
    }
}