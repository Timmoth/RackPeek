using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Nics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Nics;

public class DesktopNicRemoveCommand(IServiceProvider provider)
    : AsyncCommand<DesktopNicRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopNicRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveNicUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]NIC #{settings.Index} removed from desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}