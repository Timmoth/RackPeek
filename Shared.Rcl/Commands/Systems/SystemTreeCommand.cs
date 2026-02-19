using RackPeek.Domain.Resources.SystemResources.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Systems;

public sealed class SystemTreeCommand(GetSystemServiceTreeUseCase useCase) : AsyncCommand<SystemNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SystemNameSettings settings,
        CancellationToken cancellationToken)
    {
        var tree = await useCase.ExecuteAsync(settings.Name);

        var root = new Tree($"[bold]{tree.System.Name}[/]");

        foreach (var system in tree.Services)
        {
            var systemNode = root.AddNode($"[green]Service:[/] {system.Name}");
        }

        AnsiConsole.Write(root);
        return 0;
    }
}