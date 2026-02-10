using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers.Drives;

public class ServerDriveAddSettings : ServerNameSettings
{
    [CommandOption("--type <TYPE>")] public string Type { get; set; }

    [CommandOption("--size <SIZE>")] public int Size { get; set; }
}

public class ServerDriveAddCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerDriveAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerDriveAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AddDrivesUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Type,
            settings.Size);

        AnsiConsole.MarkupLine($"[green]Drive added to '{settings.Name}'.[/]");
        return 0;
    }
}