using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.UseCases.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Systems.Drives;

public class SystemDriveAddSettings : SystemNameSettings
{
    [CommandOption("--type <TYPE>")] public string Type { get; set; } = default!;

    [CommandOption("--size <SIZE>")] public int Size { get; set; }
}

public class SystemDriveAddCommand(IServiceProvider serviceProvider)
    : AsyncCommand<SystemDriveAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SystemDriveAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddDriveUseCase<SystemResource>>();

        await useCase.ExecuteAsync(settings.Name, settings.Type, settings.Size);

        AnsiConsole.MarkupLine($"[green]Drive added to '{settings.Name}'.[/]");
        return 0;
    }
}