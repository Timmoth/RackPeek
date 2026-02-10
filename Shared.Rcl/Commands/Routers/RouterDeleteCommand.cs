using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Routers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Routers;

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
        var useCase = scope.ServiceProvider.GetRequiredService<DeleteRouterUseCase>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Router '{settings.Name}' deleted.[/]");
        return 0;
    }
}