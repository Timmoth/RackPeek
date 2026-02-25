using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Gpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Gpus;

public class DesktopGpuRemoveSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the Gpu to remove.")]
    public int Index { get; set; }
}

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