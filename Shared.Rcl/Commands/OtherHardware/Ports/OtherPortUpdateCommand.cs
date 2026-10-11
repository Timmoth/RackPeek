using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.OtherHardware;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.OtherHardware.Ports;

public class OtherPortUpdateSettings : OtherNameSettings {
    [CommandOption("--index <INDEX>")]
    [Description("The index of the port to update.")]
    public int Index { get; set; }

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

public class OtherPortUpdateCommand(IServiceProvider sp)
    : AsyncCommand<OtherPortUpdateSettings> {
    protected override async Task<int> ExecuteAsync(CommandContext ctx, OtherPortUpdateSettings s,
        CancellationToken ct) {
        using IServiceScope scope = sp.CreateScope();
        IUpdatePortUseCase<Other> useCase = scope.ServiceProvider.GetRequiredService<IUpdatePortUseCase<Other>>();

        await useCase.ExecuteAsync(s.Name, s.Index, s.Type, s.Speed, s.Count);

        AnsiConsole.MarkupLine($"[green]Port #{s.Index} updated on other hardware '{s.Name}'.[/]");
        return 0;
    }
}
