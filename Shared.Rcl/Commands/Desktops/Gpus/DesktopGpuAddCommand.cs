using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops;
using RackPeek.Domain.UseCases.Gpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Gpus;

public class DesktopGpuAddCommand(IServiceProvider provider)
    : AsyncCommand<DesktopGpuAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopGpuAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddGpuUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Model, settings.Vram);

        AnsiConsole.MarkupLine($"[green]GPU added to desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}