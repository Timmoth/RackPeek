using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Systems.Drives;

public class SystemDriveRemoveSettings : SystemNameSettings
{
    [CommandOption("--index <INDEX>")] 
    public int Index { get; set; }
}

public class SystemDriveRemoveCommand(IServiceProvider serviceProvider)
    : AsyncCommand<SystemDriveRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SystemDriveRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RemoveSystemDriveUseCase>();

        await useCase.ExecuteAsync(settings.Name, settings.Index);

        AnsiConsole.MarkupLine($"[green]Drive {settings.Index} removed from '{settings.Name}'.[/]");
        return 0;
    }
}