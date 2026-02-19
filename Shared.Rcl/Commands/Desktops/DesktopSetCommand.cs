using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops;

public class DesktopSetSettings : DesktopNameSettings
{
    [CommandOption("--model")] public string? Model { get; set; }
}

public class DesktopSetCommand(IServiceProvider provider)
    : AsyncCommand<DesktopSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateDesktopUseCase>();

        await useCase.ExecuteAsync(settings.Name, settings.Model);

        AnsiConsole.MarkupLine($"[green]Desktop '{settings.Name}' updated.[/]");
        return 0;
    }
}