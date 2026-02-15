using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Commands.Servers;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Systems;

public class SystemSetSettings : ServerNameSettings
{
    [CommandOption("--type")]
    [Description("System type: baremetal, hypervisor, vm, container, embedded, cloud, other.")]
    public string? Type { get; set; }

    [CommandOption("--os")]
    [Description("Operating system name.")]
    public string? Os { get; set; }

    [CommandOption("--cores")]
    [Description("Number of CPU cores.")]
    public int? Cores { get; set; }

    [CommandOption("--ram")]
    [Description("RAM capacity in GB.")]
    public int? Ram { get; set; }

    [CommandOption("--runs-on")]
    [Description("Name of the host this system runs on.")]
    public string? RunsOn { get; set; }
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
            settings.RunsOn
        );

        AnsiConsole.MarkupLine($"[green]System '{settings.Name}' updated.[/]");
        return 0;
    }
}