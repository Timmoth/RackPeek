using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Devices;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Devices;

public class DeviceGetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DeviceHardwareReportUseCase>();

        var report = await useCase.ExecuteAsync();

        if (report.Devices.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No devices found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Name")
            .AddColumn("Model");

        foreach (var d in report.Devices)
            table.AddRow(
                d.Name,
                d.Model
            );

        AnsiConsole.Write(table);
        return 0;
    }
}
