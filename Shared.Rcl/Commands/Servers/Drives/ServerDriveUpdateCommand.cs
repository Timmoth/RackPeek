using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers.Drives;

public class ServerDriveUpdateSettings : ServerNameSettings
{
    [CommandOption("--index <INDEX>")]
    [Description("Index of the drive to update.")]
    public int Index { get; set; }

    [CommandOption("--type <TYPE>")]
    [Description("Drive type: nvme, ssd, hdd, sas, sata, usb, sdcard, micro-sd.")]
    public string Type { get; set; }

    [CommandOption("--size <SIZE>")]
    [Description("Drive capacity in GB.")]
    public int Size { get; set; }
}

public class ServerDriveUpdateCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerDriveUpdateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerDriveUpdateSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateDriveUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Index,
            settings.Type,
            settings.Size);

        AnsiConsole.MarkupLine($"[green]Drive {settings.Index} updated on '{settings.Name}'.[/]");
        return 0;
    }
}