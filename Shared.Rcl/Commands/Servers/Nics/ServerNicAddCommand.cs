using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers.Nics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers.Nics;

public class ServerNicAddSettings : ServerNameSettings
{
    [CommandOption("--type <TYPE>")] public string Type { get; set; }

    [CommandOption("--speed <SPEED>")] public int Speed { get; set; }

    [CommandOption("--ports <PORTS>")] public int Ports { get; set; }
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