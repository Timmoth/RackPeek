using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Drive;

public class DesktopDriveSetSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The drive index to update.")]
    public int Index { get; set; }

    [CommandOption("--type")]
    [Description("The drive type e.g hdd / ssd.")]
    public string? Type { get; set; }

    [CommandOption("--size")]
    [Description("The drive capacity in Gb.")]
    public int? Size { get; set; }
}

public class DesktopDriveSetCommand(IServiceProvider provider)
    : AsyncCommand<DesktopDriveSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopDriveSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateDriveUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index, settings.Type, settings.Size);

        AnsiConsole.MarkupLine($"[green]Drive #{settings.Index} updated on desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}