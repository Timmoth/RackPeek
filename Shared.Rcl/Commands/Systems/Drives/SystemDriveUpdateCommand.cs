using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.UseCases.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Systems.Drives;

public class SystemDriveUpdateSettings : SystemNameSettings
{
    [CommandOption("--index <INDEX>")] public int Index { get; set; }

    [CommandOption("--type <TYPE>")] public string Type { get; set; } = default!;

    [CommandOption("--size <SIZE>")] public int Size { get; set; }
}

public class SystemDriveUpdateCommand(IServiceProvider serviceProvider)
    : AsyncCommand<SystemDriveUpdateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SystemDriveUpdateSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateDriveUseCase<SystemResource>>();

        await useCase.ExecuteAsync(settings.Name, settings.Index, settings.Type, settings.Size);

        AnsiConsole.MarkupLine($"[green]Drive {settings.Index} updated on '{settings.Name}'.[/]");
        return 0;
    }
}