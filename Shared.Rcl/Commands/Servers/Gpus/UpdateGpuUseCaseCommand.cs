using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.UseCases.Gpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers.Gpus;

public class ServerGpuUpdateSettings : ServerNameSettings
{
    [CommandOption("--index <INDEX>")] public int Index { get; set; }

    [CommandOption("--model <MODEL>")] public string Model { get; set; }

    [CommandOption("--vram <VRAM>")] public int Vram { get; set; }
}

public class ServerGpuUpdateCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerGpuUpdateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerGpuUpdateSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateGpuUseCase<Server>>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Index,
            settings.Model,
            settings.Vram);

        AnsiConsole.MarkupLine($"[green]GPU {settings.Index} updated on '{settings.Name}'.[/]");
        return 0;
    }
}