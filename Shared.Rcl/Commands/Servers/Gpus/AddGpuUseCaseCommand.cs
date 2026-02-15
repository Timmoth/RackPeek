using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers.Gpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers.Gpus;

public class ServerGpuAddSettings : ServerNameSettings
{
    [CommandOption("--model <MODEL>")]
    [Description("GPU model name (max 50 chars).")]
    public string Model { get; set; }

    [CommandOption("--vram <VRAM>")]
    [Description("GPU VRAM in GB.")]
    public int Vram { get; set; }
}

public class ServerGpuAddCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerGpuAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerGpuAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AddGpuUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Model,
            settings.Vram);

        AnsiConsole.MarkupLine($"[green]GPU added to '{settings.Name}'.[/]");
        return 0;
    }
}