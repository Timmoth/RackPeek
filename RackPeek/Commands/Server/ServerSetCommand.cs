using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Server;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Server;
public class ServerSetSettings : ServerNameSettings
{
    [CommandOption("--ram <GB>")]
    public int RamGb { get; set; }

    [CommandOption("--ipmi")]
    public bool Ipmi { get; set; }
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
            settings.Ipmi);

        AnsiConsole.MarkupLine($"[green]Server '{settings.Name}' updated.[/]");
        return 0;
    }
}