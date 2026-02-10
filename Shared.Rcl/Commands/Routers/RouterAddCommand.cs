using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Routers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Routers;

public class RouterAddSettings : CommandSettings
{
    [CommandArgument(0, "<name>")] public string Name { get; set; } = default!;
}

public class RouterAddCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<RouterAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        RouterAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<AddRouterUseCase>();

        await useCase.ExecuteAsync(
            settings.Name
        );

        AnsiConsole.MarkupLine($"[green]Router '{settings.Name}' added.[/]");
        return 0;
    }
}