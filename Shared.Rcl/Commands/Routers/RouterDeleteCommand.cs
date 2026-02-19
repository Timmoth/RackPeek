using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Routers;

public class RouterDeleteCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<RouterNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        RouterNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IDeleteResourceUseCase<Router>>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Router '{settings.Name}' deleted.[/]");
        return 0;
    }
}