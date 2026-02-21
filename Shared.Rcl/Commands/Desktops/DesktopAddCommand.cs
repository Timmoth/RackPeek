using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops;

public class DesktopAddCommand(IServiceProvider provider)
    : AsyncCommand<DesktopNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddResourceUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Desktop '{settings.Name}' added.[/]");
        return 0;
    }
}