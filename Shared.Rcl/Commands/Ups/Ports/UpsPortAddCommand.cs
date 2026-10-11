using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;
using UpsUnit = RackPeek.Domain.Resources.UpsUnits.Ups;

namespace Shared.Rcl.Commands.Ups.Ports;

public class UpsPortAddSettings : UpsNameSettings {
    [CommandOption("--type")]
    [Description("The port type (e.g., rj45, usb).")]
    public string? Type { get; set; }

    [CommandOption("--speed")]
    [Description("The port speed (e.g., 0.1, 1).")]
    public double? Speed { get; set; }

    [CommandOption("--count")]
    [Description("Number of ports of this type.")]
    public int? Count { get; set; }
}

public class UpsPortAddCommand(IServiceProvider sp)
    : AsyncCommand<UpsPortAddSettings> {
    protected override async Task<int> ExecuteAsync(CommandContext ctx, UpsPortAddSettings s, CancellationToken ct) {
        using IServiceScope scope = sp.CreateScope();
        IAddPortUseCase<UpsUnit> useCase = scope.ServiceProvider.GetRequiredService<IAddPortUseCase<UpsUnit>>();

        await useCase.ExecuteAsync(s.Name, s.Type, s.Speed, s.Count);

        AnsiConsole.MarkupLine($"[green]Port added to UPS '{s.Name}'.[/]");
        return 0;
    }
}
