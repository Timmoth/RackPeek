using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers.Gpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers.Gpus;

public class ServerGpuUpdateSettings : ServerNameSettings
{
    [CommandOption("--index <INDEX>")]
    [Description("Index of the GPU to update.")]
    public int Index { get; set; }

    [CommandOption("--model <MODEL>")]
    [Description("GPU model name (max 50 chars).")]
    public string Model { get; set; }

    [CommandOption("--vram <VRAM>")]
    [Description("GPU VRAM in GB.")]
    public int Vram { get; set; }
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
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateGpuUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Index,
            settings.Model,
            settings.Vram);

        AnsiConsole.MarkupLine($"[green]GPU {settings.Index} updated on '{settings.Name}'.[/]");
        return 0;
    }
}