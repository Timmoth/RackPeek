using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers.Nics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers.Nics;

public class ServerNicUpdateSettings : ServerNameSettings
{
    [CommandOption("--index <INDEX>")]
    [Description("Index of the NIC to update.")]
    public int Index { get; set; }

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

public class ServerNicUpdateCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerNicUpdateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerNicUpdateSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateNicUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Index,
            settings.Type,
            settings.Speed,
            settings.Ports);

        AnsiConsole.MarkupLine($"[green]NIC {settings.Index} updated on '{settings.Name}'.[/]");
        return 0;
    }
}