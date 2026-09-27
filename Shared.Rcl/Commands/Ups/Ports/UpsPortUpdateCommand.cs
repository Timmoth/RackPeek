using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;
using UpsUnit = RackPeek.Domain.Resources.UpsUnits.Ups;

namespace Shared.Rcl.Commands.Ups.Ports;

public class UpsPortUpdateSettings : UpsNameSettings {
    [CommandOption("--index <INDEX>")]
    [Description("The index of the port to update.")]
    public int Index { get; set; }

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

public class UpsPortUpdateCommand(IServiceProvider sp)
    : AsyncCommand<UpsPortUpdateSettings> {
    protected override async Task<int> ExecuteAsync(CommandContext ctx, UpsPortUpdateSettings s, CancellationToken ct) {
        using IServiceScope scope = sp.CreateScope();
        IUpdatePortUseCase<UpsUnit> useCase = scope.ServiceProvider.GetRequiredService<IUpdatePortUseCase<UpsUnit>>();

        await useCase.ExecuteAsync(s.Name, s.Index, s.Type, s.Speed, s.Count);

        AnsiConsole.MarkupLine($"[green]Port #{s.Index} updated on UPS '{s.Name}'.[/]");
        return 0;
    }
}
