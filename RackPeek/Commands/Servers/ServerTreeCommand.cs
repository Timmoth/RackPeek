using RackPeek.Domain.Resources.Hardware.Servers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers;

public sealed class ServerTreeCommand(GetServerSystemTreeUseCase useCase) : AsyncCommand<ServerNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerNameSettings settings,
        CancellationToken cancellationToken)
    {
        var tree = await useCase.ExecuteAsync(settings.Name);

        if (tree is null)
        {
            AnsiConsole.MarkupLine($"[red]Server '{settings.Name}' not found.[/]");
            return -1;
        }

        var root = new Tree($"[bold]{tree.Hardware.Name}[/]");

        foreach (var system in tree.Systems)
        {
            var systemNode = root.AddNode($"[green]System:[/] {system.System.Name}");
            foreach (var service in system.Services) systemNode.AddNode($"[green]Service:[/] {service.Name}");
        }

        AnsiConsole.Write(root);
        return 0;
    }
}