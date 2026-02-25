using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Servers.Labels;

public class ServerLabelAddSettings : ServerNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;

    [CommandOption("--value <VALUE>")] public string Value { get; set; } = default!;
}

public class ServerLabelAddCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerLabelAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerLabelAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddLabelUseCase<Server>>();

        await useCase.ExecuteAsync(settings.Name, settings.Key, settings.Value);

        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' added to '{settings.Name}'.[/]");
        return 0;
    }
}