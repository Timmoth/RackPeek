using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Nics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Nics;

public class DesktopNicSetSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the nic to remove.")]
    public int Index { get; set; }

    [CommandOption("--type")]
    [Description("The nic port type e.g rj45 / sfp+")]
    public string? Type { get; set; }

    [CommandOption("--speed")]
    [Description("The speed of the nic in Gb/s.")]
    public int? Speed { get; set; }

    [CommandOption("--ports")]
    [Description("The number of ports.")]
    public int? Ports { get; set; }
}

public class DesktopNicSetCommand(IServiceProvider provider)
    : AsyncCommand<DesktopNicSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopNicSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateNicUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index, settings.Type, settings.Speed, settings.Ports);

        AnsiConsole.MarkupLine($"[green]NIC #{settings.Index} updated on desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}