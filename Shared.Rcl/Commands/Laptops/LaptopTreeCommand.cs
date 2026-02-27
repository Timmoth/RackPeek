using RackPeek.Domain.Resources.Hardware;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops;

public sealed class LaptopTreeCommand(GetHardwareSystemTreeUseCase useCase)
    : AsyncCommand<LaptopNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopNameSettings settings,
        CancellationToken cancellationToken)
    {
        var tree = await useCase.ExecuteAsync(settings.Name);

        var root = new Tree($"[bold]{tree.Hardware.Name}[/]");

        foreach (var system in tree.Systems)
        {
            var systemNode = root.AddNode($"[green]System:[/] {system.System.Name}");
            foreach (var service in system.ChildResources)
                systemNode.AddNode($"[green]Service:[/] {service.Name}");
        }

        AnsiConsole.Write(root);
        return 0;
    }
}