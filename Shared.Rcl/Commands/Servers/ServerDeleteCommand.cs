using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers;

public class ServerDeleteCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServerNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerNameSettings settings,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        IDeleteResourceUseCase<Server> useCase =
            scope.ServiceProvider.GetRequiredService<IDeleteResourceUseCase<Server>>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Server '{settings.Name}' deleted.[/]");
        return 0;
    }
}
