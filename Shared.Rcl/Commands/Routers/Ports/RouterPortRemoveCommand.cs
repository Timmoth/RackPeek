using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Routers.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Routers.Ports;

public class RouterPortRemoveSettings : RouterNameSettings
{
    [CommandOption("--index <INDEX>")] public int Index { get; set; }
}

public class RouterPortRemoveCommand(IServiceProvider sp)
    : AsyncCommand<RouterPortRemoveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext ctx, RouterPortRemoveSettings s, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RemoveRouterPortUseCase>();

        await useCase.ExecuteAsync(s.Name, s.Index);

        AnsiConsole.MarkupLine($"[green]Port {s.Index} removed from router '{s.Name}'.[/]");
        return 0;
    }
}