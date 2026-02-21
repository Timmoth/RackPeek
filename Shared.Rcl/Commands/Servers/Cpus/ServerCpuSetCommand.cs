using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.UseCases.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers.Cpus;

public class ServerCpuSetSettings : ServerNameSettings
{
    [CommandOption("--index <INDEX>")] public int Index { get; set; }

    [CommandOption("--model <MODEL>")] public string Model { get; set; }

    [CommandOption("--cores <CORES>")] public int Cores { get; set; }

    [CommandOption("--threads <THREADS>")] public int Threads { get; set; }
}

public class ServerCpuSetCommand(IServiceProvider serviceProvider) : AsyncCommand<ServerCpuSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerCpuSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateCpuUseCase<Server>>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Index,
            settings.Model,
            settings.Cores,
            settings.Threads);

        AnsiConsole.MarkupLine($"[green]CPU {settings.Index} updated on '{settings.Name}'.[/]");
        return 0;
    }
}