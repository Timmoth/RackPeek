using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Devices;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Devices;

public class DeviceDeleteCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<DeviceNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DeviceNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DeleteDeviceUseCase>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Device '{settings.Name}' deleted.[/]");
        return 0;
    }
}
