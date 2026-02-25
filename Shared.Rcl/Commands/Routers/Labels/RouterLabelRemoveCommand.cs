using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Routers.Labels;

public class RouterLabelRemoveSettings : RouterNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
}

public class RouterLabelRemoveCommand(IServiceProvider serviceProvider) : AsyncCommand<RouterLabelRemoveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RouterLabelRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveLabelUseCase<Router>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' removed from '{settings.Name}'.[/]");
        return 0;
    }
}