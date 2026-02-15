using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Commands.Servers;
using RackPeek.Domain.Resources.Hardware.Devices;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Devices;

public class DeviceSetSettings : ServerNameSettings
{
    [CommandOption("--model")]
    [Description("The device model name.")]
    public string? Model { get; set; }
}

public class DeviceSetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<DeviceSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DeviceSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateDeviceUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Model
        );

        AnsiConsole.MarkupLine($"[green]Device '{settings.Name}' updated.[/]");
        return 0;
    }
}
