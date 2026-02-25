using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Labels;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Labels;

public class DesktopLabelRemoveSettings : DesktopNameSettings
{
    [CommandOption("--key <KEY>")] public string Key { get; set; } = default!;
}

public class DesktopLabelRemoveCommand(IServiceProvider serviceProvider) : AsyncCommand<DesktopLabelRemoveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, DesktopLabelRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveLabelUseCase<Desktop>>();
        await useCase.ExecuteAsync(settings.Name, settings.Key);
        AnsiConsole.MarkupLine($"[green]Label '{settings.Key}' removed from '{settings.Name}'.[/]");
        return 0;
    }
}