using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Nics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Nics;

public class DesktopNicAddCommand(IServiceProvider provider)
    : AsyncCommand<DesktopNicAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopNicAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddNicUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Type, settings.Speed, settings.Ports);

        AnsiConsole.MarkupLine($"[green]NIC added to desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}