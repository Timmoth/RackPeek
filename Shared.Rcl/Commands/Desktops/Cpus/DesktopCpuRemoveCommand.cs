using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Cpus;

public class DesktopCpuRemoveSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The name of the desktop.")]
    public string DesktopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the desktop cpu to remove.")]
    public int Index { get; set; }
}

public class DesktopCpuRemoveCommand(IServiceProvider provider)
    : AsyncCommand<DesktopCpuRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopCpuRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveCpuUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]CPU #{settings.Index} removed from desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}