using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Routers.Labels;

public class RouterLabelAddSettings : RouterNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
    [CommandOption("--value <VALUE>")] public string Value { get; set; } = default!;
}

public class RouterLabelAddCommand(IServiceProvider serviceProvider) : AsyncCommand<RouterLabelAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RouterLabelAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddLabelUseCase<Router>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key, settings.Value);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' added to '{settings.Name}'.[/]");
        return 0;
    }
}