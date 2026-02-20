using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops;
using RackPeek.Domain.UseCases.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Drive;

public class DesktopDriveRemoveCommand(IServiceProvider provider)
    : AsyncCommand<DesktopDriveRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopDriveRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveDriveUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]Drive #{settings.Index} removed from desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}