using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops.Cpus;

public class DesktopCpuRemoveCommand(IServiceProvider provider)
    : AsyncCommand<DesktopCpuRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopCpuRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RemoveDesktopCpuUseCase>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]CPU #{settings.Index} removed from desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}