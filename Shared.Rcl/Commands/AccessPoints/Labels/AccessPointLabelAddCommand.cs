using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.AccessPoints.Labels;

public class AccessPointLabelAddSettings : AccessPointNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
    [CommandOption("--value <VALUE>")] public string Value { get; set; } = default!;
}

public class AccessPointLabelAddCommand(IServiceProvider serviceProvider) : AsyncCommand<AccessPointLabelAddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AccessPointLabelAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddLabelUseCase<AccessPoint>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key, settings.Value);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' added to '{settings.Name}'.[/]");
        return 0;
    }
}