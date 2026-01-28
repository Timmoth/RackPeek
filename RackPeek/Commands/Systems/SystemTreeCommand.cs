using RackPeek.Domain.Resources.Services.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Systems;

public sealed class SystemTreeCommand(GetSystemServiceTreeUseCase useCase) : AsyncCommand<SystemNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SystemNameSettings settings,
        CancellationToken cancellationToken)
    {
        var tree = await useCase.ExecuteAsync(settings.Name);

        if (tree is null)
        {
            AnsiConsole.MarkupLine($"[red]System '{settings.Name}' not found.[/]");
            return -1;
        }

        var root = new Tree($"[bold]{tree.System.Name}[/]");

        foreach (var system in tree.Services)
        {
            var systemNode = root.AddNode($"[green]Service:[/] {system.Name}");
        }

        AnsiConsole.Write(root);
        return 0;
    }
}