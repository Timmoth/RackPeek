using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers;

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
        var useCase = scope.ServiceProvider.GetRequiredService<IGetResourceByNameUseCase<Server>>();

        var server = await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine(
            $"[green]{server.Name}[/]  RAM: {server.Ram?.Size} GB, IPMI: {(server.Ipmi == true ? "yes" : "no")}");

        return 0;
    }
}