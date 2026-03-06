using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware;
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
        SystemDependencyTree tree = await useCase.ExecuteAsync(settings.Name);

        var root = new Tree($"[bold]{tree.System.Name}[/]");

        foreach (Resource system in tree.ChildResources)
        {
            TreeNode systemNode = root.AddNode($"[green]Service:[/] {system.Name}");
        }

        AnsiConsole.Write(root);
        return 0;
    }
}
