using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Server;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Server;

public class ServerGetByNameCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServerNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<GetServerUseCase>();

        var server = await useCase.ExecuteAsync(settings.Name);

        if (server == null)
        {
            AnsiConsole.MarkupLine($"[red]Server '{settings.Name}' not found.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine(
            $"[green]{server.Name}[/]  RAM: {server.Ram?.Size} GB, IPMI: {(server.Ipmi == true ? "yes" : "no")}");

        return 0;
    }
}