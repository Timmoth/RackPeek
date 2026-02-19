using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers.Gpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers.Gpus;

public class ServerGpuRemoveSettings : ServerNameSettings
{
    [CommandOption("--index <INDEX>")] public int Index { get; set; }
}

public class ServerGpuRemoveCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerGpuRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerGpuRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RemoveGpuUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Index);

        AnsiConsole.MarkupLine($"[green]GPU {settings.Index} removed from '{settings.Name}'.[/]");
        return 0;
    }
}