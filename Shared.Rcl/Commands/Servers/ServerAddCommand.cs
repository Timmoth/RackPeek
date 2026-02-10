using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers;

public class ServerAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}

public class ServerAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<ServerAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AddServerUseCase>();

        await useCase.ExecuteAsync(
            settings.Name
        );

        AnsiConsole.MarkupLine($"[green]Server '{settings.Name}' added.[/]");
        return 0;
    }
}