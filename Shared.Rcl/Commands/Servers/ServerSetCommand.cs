using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Servers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers;

public class ServerSetSettings : ServerNameSettings
{
    [CommandOption("--ram <GB>")] public int RamGb { get; set; }
    [CommandOption("--ram_mts <MTs>")] public int RamMts { get; set; }

    [CommandOption("--ipmi")] public bool Ipmi { get; set; }
}

public class ServerSetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServerSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateServerUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.RamGb,
            settings.RamMts,
            settings.Ipmi);

        AnsiConsole.MarkupLine($"[green]Server '{settings.Name}' updated.[/]");
        return 0;
    }
}