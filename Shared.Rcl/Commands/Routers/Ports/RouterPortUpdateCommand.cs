using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Routers.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Routers.Ports;

public class RouterPortUpdateSettings : RouterNameSettings
{
    [CommandOption("--index <INDEX>")] public int Index { get; set; }
    [CommandOption("--type")] public string? Type { get; set; }
    [CommandOption("--speed")] public double? Speed { get; set; }
    [CommandOption("--count")] public int? Count { get; set; }
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