using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Server;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Server;

public class ServerDeleteCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServerNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DeleteServerUseCase>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Server '{settings.Name}' deleted.[/]");
        return 0;
    }
}