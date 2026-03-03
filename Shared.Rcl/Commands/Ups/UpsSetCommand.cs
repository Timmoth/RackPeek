using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.UpsUnits;
using Shared.Rcl.Commands.Servers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Ups;

public class UpsSetSettings : ServerNameSettings {
    [CommandOption("--model")] public string? Model { get; set; }
    [CommandOption("--va")] public int? Va { get; set; }
}

public class UpsSetCommand(IServiceProvider provider)
    : AsyncCommand<UpsSetSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpsSetSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        UpdateUpsUseCase useCase = scope.ServiceProvider.GetRequiredService<UpdateUpsUseCase>();

        await useCase.ExecuteAsync(settings.Name, settings.Model, settings.Va);

        AnsiConsole.MarkupLine($"[green]UPS '{settings.Name}' updated.[/]");
        return 0;
    }
}
