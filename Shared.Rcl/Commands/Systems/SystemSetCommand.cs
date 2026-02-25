using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using Shared.Rcl.Commands.Servers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Systems;

public class SystemSetSettings : ServerNameSettings
{
    [CommandOption("--type")] public string? Type { get; set; }

    [CommandOption("--os")] public string? Os { get; set; }

    [CommandOption("--cores")] public int? Cores { get; set; }

    [CommandOption("--ram")] public int? Ram { get; set; }

    [CommandOption("--runs-on <RUNSON>")]
    [Description("The physical machine(s) the service is running on.")]
    public string[]? RunsOn { get; set; }
}

public class SystemSetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<SystemSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SystemSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateSystemUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Type,
            settings.Os,
            settings.Cores,
            settings.Ram,
            settings.RunsOn?.ToList()
        );

        AnsiConsole.MarkupLine($"[green]System '{settings.Name}' updated.[/]");
        return 0;
    }
}