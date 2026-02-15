using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Devices;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Devices;

public class DeviceDescribeCommand(
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

        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap());

        grid.AddRow("Name:", d.Name);
        grid.AddRow("Model:", d.Model ?? "Unknown");

        AnsiConsole.Write(
            new Panel(grid)
                .Header("Device")
                .Border(BoxBorder.Rounded));

        return 0;
    }
}
