using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops.Drive;

public class DesktopDriveRemoveCommand(IServiceProvider provider)
    : AsyncCommand<DesktopDriveRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopDriveRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RemoveDesktopDriveUseCase>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]Drive #{settings.Index} removed from desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}