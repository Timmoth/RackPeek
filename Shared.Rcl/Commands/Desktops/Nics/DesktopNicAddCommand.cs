using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Nics;

public class DesktopNicAddSettings : CommandSettings {
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandOption("--type")]
    [Description("The nic port type e.g rj45 / sfp+")]
    public string? Type { get; set; }

    [CommandOption("--speed")]
    [Description("The port speed.")]
    public double? Speed { get; set; }

    [CommandOption("--ports")]
    [Description("The number of ports.")]
    public int? Ports { get; set; }
}

public class DesktopNicAddCommand(IServiceProvider provider)
    : AsyncCommand<DesktopNicAddSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopNicAddSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        IAddPortUseCase<Desktop> useCase = scope.ServiceProvider.GetRequiredService<IAddPortUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Type, settings.Speed, settings.Ports);

        AnsiConsole.MarkupLine($"[green]NIC added to desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}
