using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers.Nics;

public class ServerNicUpdateSettings : ServerNameSettings {
    [CommandOption("--index <INDEX>")] public int Index { get; set; }

    [CommandOption("--type <TYPE>")] public string? Type { get; set; }

    [CommandOption("--speed <SPEED>")] public double Speed { get; set; }

    [CommandOption("--ports <PORTS>")] public int Ports { get; set; }
}

public class ServerNicUpdateCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerNicUpdateSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerNicUpdateSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = serviceProvider.CreateScope();
        IUpdatePortUseCase<Server> useCase = scope.ServiceProvider.GetRequiredService<IUpdatePortUseCase<Server>>();

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
