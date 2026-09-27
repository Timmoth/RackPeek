using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.OtherHardware;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.OtherHardware.Ports;

public class OtherPortAddSettings : OtherNameSettings {
    [CommandOption("--type")]
    [Description("The port type (e.g., rj45, sfp+).")]
    public string? Type { get; set; }

    [CommandOption("--speed")]
    [Description("The port speed (e.g., 1, 2.5, 10).")]
    public double? Speed { get; set; }

    [CommandOption("--count")]
    [Description("Number of ports of this type.")]
    public int? Count { get; set; }
}

public class OtherPortAddCommand(IServiceProvider sp)
    : AsyncCommand<OtherPortAddSettings> {
    protected override async Task<int> ExecuteAsync(CommandContext ctx, OtherPortAddSettings s, CancellationToken ct) {
        using IServiceScope scope = sp.CreateScope();
        IAddPortUseCase<Other> useCase = scope.ServiceProvider.GetRequiredService<IAddPortUseCase<Other>>();

        await useCase.ExecuteAsync(s.Name, s.Type, s.Speed, s.Count);

        AnsiConsole.MarkupLine($"[green]Port added to other hardware '{s.Name}'.[/]");
        return 0;
    }
}
