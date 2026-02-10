using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops.Cpus;

public class DesktopCpuAddCommand(IServiceProvider provider)
    : AsyncCommand<DesktopCpuAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopCpuAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AddDesktopCpuUseCase>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Model, settings.Cores, settings.Threads);

        AnsiConsole.MarkupLine($"[green]CPU added to desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}