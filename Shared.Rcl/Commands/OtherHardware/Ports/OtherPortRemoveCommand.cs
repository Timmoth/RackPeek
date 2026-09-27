using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.OtherHardware;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.OtherHardware.Ports;

public class OtherPortRemoveSettings : OtherNameSettings {
    [CommandOption("--index <INDEX>")]
    [Description("The index of the port to remove.")]
    public int Index { get; set; }
}

public class OtherPortRemoveCommand(IServiceProvider sp)
    : AsyncCommand<OtherPortRemoveSettings> {
    protected override async Task<int> ExecuteAsync(CommandContext ctx, OtherPortRemoveSettings s,
        CancellationToken ct) {
        using IServiceScope scope = sp.CreateScope();
        IRemovePortUseCase<Other> useCase = scope.ServiceProvider.GetRequiredService<IRemovePortUseCase<Other>>();

        await useCase.ExecuteAsync(s.Name, s.Index);

        AnsiConsole.MarkupLine($"[green]Port #{s.Index} removed from other hardware '{s.Name}'.[/]");
        return 0;
    }
}
