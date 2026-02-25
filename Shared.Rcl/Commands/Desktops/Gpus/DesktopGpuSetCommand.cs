using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Gpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Gpus;

public class DesktopGpuSetSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the gpu to update.")]
    public int Index { get; set; }

    [CommandOption("--model")]
    [Description("The gpu model name.")]
    public string? Model { get; set; }

    [CommandOption("--vram")]
    [Description("The amount of gpu vram in Gb.")]
    public int? Vram { get; set; }
}

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