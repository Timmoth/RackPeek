using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Routers;
using Shared.Rcl.Commands.Servers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Routers;

public class RouterSetSettings : ServerNameSettings
{
    [CommandOption("--Model")] public string Model { get; set; } = default!;

    [CommandOption("--managed")] public bool Managed { get; set; }

    [CommandOption("--poe")] public bool Poe { get; set; }
}

public class RouterSetCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<RouterSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        RouterSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateRouterUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Model,
            settings.Managed,
            settings.Poe);

        AnsiConsole.MarkupLine($"[green]Server '{settings.Name}' updated.[/]");
        return 0;
    }
}