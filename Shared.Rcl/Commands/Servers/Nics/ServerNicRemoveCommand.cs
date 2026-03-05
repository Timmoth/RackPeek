using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers.Nics;

public class ServerNicRemoveSettings : ServerNameSettings {
    [CommandOption("--index <INDEX>")] public int Index { get; set; }
}

public class ServerNicRemoveCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerNicRemoveSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerNicRemoveSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = serviceProvider.CreateScope();
        IRemovePortUseCase<Server> useCase = scope.ServiceProvider.GetRequiredService<IRemovePortUseCase<Server>>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Index);

        AnsiConsole.MarkupLine($"[green]NIC {settings.Index} removed from '{settings.Name}'.[/]");
        return 0;
    }
}
