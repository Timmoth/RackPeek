using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;
using UpsUnit = RackPeek.Domain.Resources.UpsUnits.Ups;

namespace Shared.Rcl.Commands.Ups.Ports;

public class UpsPortRemoveSettings : UpsNameSettings {
    [CommandOption("--index <INDEX>")]
    [Description("The index of the port to remove.")]
    public int Index { get; set; }
}

public class UpsPortRemoveCommand(IServiceProvider sp)
    : AsyncCommand<UpsPortRemoveSettings> {
    protected override async Task<int> ExecuteAsync(CommandContext ctx, UpsPortRemoveSettings s, CancellationToken ct) {
        using IServiceScope scope = sp.CreateScope();
        IRemovePortUseCase<UpsUnit> useCase = scope.ServiceProvider.GetRequiredService<IRemovePortUseCase<UpsUnit>>();

        await useCase.ExecuteAsync(s.Name, s.Index);

        AnsiConsole.MarkupLine($"[green]Port #{s.Index} removed from UPS '{s.Name}'.[/]");
        return 0;
    }
}
