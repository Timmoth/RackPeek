using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers.Drives;

public class ServerDriveUpdateSettings : ServerNameSettings
{
    [CommandOption("--index <INDEX>")] public int Index { get; set; }

    [CommandOption("--type <TYPE>")] public string Type { get; set; }

    [CommandOption("--size <SIZE>")] public int Size { get; set; }
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