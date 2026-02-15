using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Devices;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Devices;

public class DeviceAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")]
    [Description("The device name.")]
    public string Name { get; set; } = default!;
}

public class DeviceAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<DeviceAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DeviceAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddResourceUseCase<Device>>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Device '{settings.Name}' added.[/]");
        return 0;
    }
}
