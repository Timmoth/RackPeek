using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Hardware;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers;

public sealed class ServerTreeCommand(GetHardwareSystemTreeUseCase useCase) : AsyncCommand<ServerNameSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerNameSettings settings,
        CancellationToken cancellationToken) {
        HardwareDependencyTree? tree = await useCase.ExecuteAsync(settings.Name);

        if (tree is null) {
            AnsiConsole.MarkupLine($"[red]Server '{settings.Name}' not found.[/]");
            return -1;
        }

        var root = new Tree($"[bold]{tree.Hardware.Name}[/]");

        foreach (SystemDependencyTree system in tree.Systems) {
            TreeNode systemNode = root.AddNode($"[green]System:[/] {system.System.Name}");
            foreach (Resource service in system.ChildResources)
                systemNode.AddNode($"[green]Service:[/] {service.Name}");
        }

        AnsiConsole.Write(root);
        return 0;
    }
}
