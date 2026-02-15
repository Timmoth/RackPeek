using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Devices;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Devices;

public class DeviceGetByNameCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<DeviceNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DeviceNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DescribeDeviceUseCase>();

        var d = await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine(
            $"[green]{d.Name}[/]  Model: {d.Model ?? "Unknown"}");

        return 0;
    }
}
