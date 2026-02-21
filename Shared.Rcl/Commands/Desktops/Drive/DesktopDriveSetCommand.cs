using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Drive;

public class DesktopDriveSetCommand(IServiceProvider provider)
    : AsyncCommand<DesktopDriveSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopDriveSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateDriveUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index, settings.Type, settings.Size);

        AnsiConsole.MarkupLine($"[green]Drive #{settings.Index} updated on desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}