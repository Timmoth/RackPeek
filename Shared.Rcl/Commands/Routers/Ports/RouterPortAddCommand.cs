using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Routers.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Routers.Ports;

public class RouterPortAddSettings : RouterNameSettings
{
    [CommandOption("--type")] public string? Type { get; set; }
    [CommandOption("--speed")] public double? Speed { get; set; }
    [CommandOption("--count")] public int? Count { get; set; }
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