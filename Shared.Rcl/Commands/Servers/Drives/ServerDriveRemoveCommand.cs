using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.UseCases.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers.Drives;

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
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveDriveUseCase<Server>>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Index);

        AnsiConsole.MarkupLine($"[green]Drive {settings.Index} removed from '{settings.Name}'.[/]");
        return 0;
    }
}