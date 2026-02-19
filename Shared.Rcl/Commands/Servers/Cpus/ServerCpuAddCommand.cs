using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.UseCases.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers.Cpus;

public class ServerCpuAddSettings : ServerNameSettings
{
    [CommandOption("--model <MODEL>")] public string Model { get; set; }

    [CommandOption("--cores <CORES>")] public int Cores { get; set; }

    [CommandOption("--threads <THREADS>")] public int Threads { get; set; }
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
        var useCase = scope.ServiceProvider.GetRequiredService<IAddCpuUseCase<Server>>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Model,
            settings.Cores,
            settings.Threads);

        AnsiConsole.MarkupLine($"[green]CPU added to '{settings.Name}'.[/]");
        return 0;
    }
}