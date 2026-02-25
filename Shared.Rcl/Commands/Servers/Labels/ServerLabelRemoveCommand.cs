using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers.Labels;

public class ServerLabelRemoveSettings : ServerNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
}

public class ServerLabelRemoveCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerLabelRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerLabelRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveLabelUseCase<Server>>();

        await useCase.ExecuteAsync(settings.Name, settings.Key);

        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' removed from '{settings.Name}'.[/]");
        return 0;
    }
}