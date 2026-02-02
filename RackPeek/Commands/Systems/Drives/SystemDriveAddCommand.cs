using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Systems.Drives;

public class SystemDriveAddCommand(IServiceProvider serviceProvider)
    : AsyncCommand<SystemDriveAddCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<systemName>")]
        public string SystemName { get; set; } = default!;

        [CommandArgument(1, "<driveName>")]
        public string DriveName { get; set; } = default!;

        [CommandArgument(2, "<sizeGb>")]
        public int SizeGb { get; set; }
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AddSystemDriveUseCase>();

        await useCase.ExecuteAsync(settings.SystemName, settings.DriveName, settings.SizeGb);

        AnsiConsole.MarkupLine(
            $"[green]Drive '{settings.DriveName}' added to system '{settings.SystemName}'.[/]");

        return 0;
    }
}