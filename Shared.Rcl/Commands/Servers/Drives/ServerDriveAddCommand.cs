using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.UseCases.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers.Drives;

public class ServerDriveAddSettings : ServerNameSettings
{
    [CommandOption("--type <TYPE>")]
    [Description("The drive type e.g hdd / ssd.")]
    public string Type { get; set; }

    [CommandOption("--size <SIZE>")]
    [Description("The drive capacity in GB.")]
    public int Size { get; set; }
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
        var useCase = scope.ServiceProvider.GetRequiredService<IAddDriveUseCase<Server>>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Type,
            settings.Size);

        AnsiConsole.MarkupLine($"[green]Drive added to '{settings.Name}'.[/]");
        return 0;
    }
}