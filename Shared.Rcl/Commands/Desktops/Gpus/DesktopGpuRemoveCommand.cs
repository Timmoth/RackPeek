using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops;
using RackPeek.Domain.UseCases.Gpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Gpus;

public class DesktopGpuRemoveCommand(IServiceProvider provider)
    : AsyncCommand<DesktopGpuRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopGpuRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveGpuUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]GPU #{settings.Index} removed from desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}