using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Routers;

public class RouterAddSettings : CommandSettings {
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}

public class RouterAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<RouterAddSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        RouterAddSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = serviceProvider.CreateScope();
        IAddResourceUseCase<Router> useCase = scope.ServiceProvider.GetRequiredService<IAddResourceUseCase<Router>>();

        await useCase.ExecuteAsync(
            settings.Name
        );

        AnsiConsole.MarkupLine($"[green]Router '{settings.Name}' added.[/]");
        return 0;
    }
}
