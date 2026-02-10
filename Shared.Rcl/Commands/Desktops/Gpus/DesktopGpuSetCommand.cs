using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops.Gpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops.Gpus;

public class DesktopGpuSetCommand(IServiceProvider provider)
    : AsyncCommand<DesktopGpuSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopGpuSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateDesktopGpuUseCase>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index, settings.Model, settings.Vram);

        AnsiConsole.MarkupLine($"[green]GPU #{settings.Index} updated on desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}