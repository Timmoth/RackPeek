using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers.Nics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers.Nics;

public class ServerNicAddSettings : ServerNameSettings
{
    [CommandOption("--type <TYPE>")]
    [Description("NIC type: rj45, sfp, sfp+, sfp28, sfp56, qsfp+, qsfp28, qsfp56, qsfp-dd, osfp, xfp, cx4, mgmt.")]
    public string Type { get; set; }

    [CommandOption("--speed <SPEED>")]
    [Description("Speed in Gb/s (e.g. 1, 2.5, 10, 25).")]
    public double Speed { get; set; }

    [CommandOption("--ports <PORTS>")]
    [Description("Number of ports.")]
    public int Ports { get; set; }
}

public class ServerNicAddCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerNicAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerNicAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AddNicUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Type,
            settings.Speed,
            settings.Ports);

        AnsiConsole.MarkupLine($"[green]NIC added to '{settings.Name}'.[/]");
        return 0;
    }
}