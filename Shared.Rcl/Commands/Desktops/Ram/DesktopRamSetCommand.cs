using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops.Ram;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops.Ram;

public class DesktopRamSetSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandOption("--ram <GB>")]
    [Description("RAM capacity in GB.")]
    public int? RamGb { get; set; }

    [CommandOption("--mts <MTs>")]
    [Description("RAM speed in MT/s.")]
    public int? RamMts { get; set; }
}

public class DesktopRamSetCommand(IServiceProvider serviceProvider)
    : AsyncCommand<DesktopRamSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopRamSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<SetDesktopRamUseCase>();

        await useCase.ExecuteAsync(
            settings.DesktopName,
            settings.RamGb,
            settings.RamMts);

        AnsiConsole.MarkupLine($"[green]RAM updated on '{settings.DesktopName}'.[/]");
        return 0;
    }
}
