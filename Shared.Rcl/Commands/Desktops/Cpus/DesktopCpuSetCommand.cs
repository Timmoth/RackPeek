using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Cpus;

public class DesktopCpuSetCommand(IServiceProvider provider)
    : AsyncCommand<DesktopCpuSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopCpuSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateCpuUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index, settings.Model, settings.Cores,
            settings.Threads);

        AnsiConsole.MarkupLine($"[green]CPU #{settings.Index} updated on desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}