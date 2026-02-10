using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops.Nics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops.Nics;

public class DesktopNicSetCommand(IServiceProvider provider)
    : AsyncCommand<DesktopNicSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopNicSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateDesktopNicUseCase>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index, settings.Type, settings.Speed, settings.Ports);

        AnsiConsole.MarkupLine($"[green]NIC #{settings.Index} updated on desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}