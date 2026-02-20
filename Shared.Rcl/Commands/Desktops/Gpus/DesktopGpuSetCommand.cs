using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops;
using RackPeek.Domain.UseCases.Gpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Gpus;

public class DesktopGpuSetCommand(IServiceProvider provider)
    : AsyncCommand<DesktopGpuSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopGpuSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateGpuUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index, settings.Model, settings.Vram);

        AnsiConsole.MarkupLine($"[green]GPU #{settings.Index} updated on desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}