using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Drive;

public class DesktopDriveAddSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The name of the desktop.")]
    public string DesktopName { get; set; } = default!;

    [CommandOption("--type")]
    [Description("The drive type e.g hdd / ssd.")]
    public string? Type { get; set; }

    [CommandOption("--size")]
    [Description("The drive capacity in GB.")]
    public int? Size { get; set; }
}

public class DesktopDriveAddCommand(IServiceProvider provider)
    : AsyncCommand<DesktopDriveAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopDriveAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddDriveUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Type, settings.Size);

        AnsiConsole.MarkupLine($"[green]Drive added to desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}