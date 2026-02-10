using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers.Drives;

public class ServerDriveRemoveSettings : ServerNameSettings
{
    [CommandOption("--index <INDEX>")] public int Index { get; set; }
}

public class ServerDriveRemoveCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerDriveRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerDriveRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RemoveDriveUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Index);

        AnsiConsole.MarkupLine($"[green]Drive {settings.Index} removed from '{settings.Name}'.[/]");
        return 0;
    }
}