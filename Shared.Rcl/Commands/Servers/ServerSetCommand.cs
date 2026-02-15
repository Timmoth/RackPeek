using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers;

public class ServerSetSettings : ServerNameSettings
{
    [CommandOption("--ipmi")]
    [Description("Whether the server has IPMI/BMC.")]
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
            settings.Ipmi);

        AnsiConsole.MarkupLine($"[green]Server '{settings.Name}' updated.[/]");
        return 0;
    }
}