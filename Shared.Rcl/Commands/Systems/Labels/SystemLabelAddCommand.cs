using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Systems.Labels;

public class SystemLabelAddSettings : SystemNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
    [CommandOption("--value <VALUE>")] public string Value { get; set; } = default!;
}

public class SystemLabelAddCommand(IServiceProvider serviceProvider) : AsyncCommand<SystemLabelAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SystemLabelAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddLabelUseCase<SystemResource>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key, settings.Value);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' added to '{settings.Name}'.[/]");
        return 0;
    }
}