using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Server.Nic;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Server.Nics;

public class ServerNicRemoveSettings : ServerNameSettings
{
    [CommandOption("--index <INDEX>")]
    public int Index { get; set; }
}

public class ServerNicRemoveCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerNicRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerNicRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RemoveNicUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Index);

        AnsiConsole.MarkupLine($"[green]NIC {settings.Index} removed from '{settings.Name}'.[/]");
        return 0;
    }
}