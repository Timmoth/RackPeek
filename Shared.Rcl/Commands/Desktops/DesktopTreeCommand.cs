using RackPeek.Domain.Resources.Hardware;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops;

public sealed class DesktopTreeCommand(GetHardwareSystemTreeUseCase useCase)
    : AsyncCommand<DesktopNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopNameSettings settings,
        CancellationToken cancellationToken)
    {
        var tree = await useCase.ExecuteAsync(settings.Name);

        var root = new Tree($"[bold]{tree.Hardware.Name}[/]");

        foreach (var system in tree.Systems)
        {
            var systemNode = root.AddNode($"[green]System:[/] {system.System.Name}");
            foreach (var service in system.Services)
                systemNode.AddNode($"[green]Service:[/] {service.Name}");
        }

        AnsiConsole.Write(root);
        return 0;
    }
}