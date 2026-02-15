using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers.Cpus;

public class ServerCpuAddSettings : ServerNameSettings
{
    [CommandOption("--model <MODEL>")]
    [Description("CPU model name (max 50 chars).")]
    public string Model { get; set; }

    [CommandOption("--cores <CORES>")]
    [Description("Number of CPU cores.")]
    public int Cores { get; set; }

    [CommandOption("--threads <THREADS>")]
    [Description("Number of CPU threads.")]
    public int Threads { get; set; }
}

public class ServerCpuAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServerCpuAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerCpuAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AddCpuUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Model,
            settings.Cores,
            settings.Threads);

        AnsiConsole.MarkupLine($"[green]CPU added to '{settings.Name}'.[/]");
        return 0;
    }
}