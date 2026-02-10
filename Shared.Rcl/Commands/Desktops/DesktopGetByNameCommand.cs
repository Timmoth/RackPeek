using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Desktops;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Desktops;

public class DesktopGetByNameCommand(IServiceProvider provider)
    : AsyncCommand<DesktopNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<GetDesktopUseCase>();

        var desktop = await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]{desktop.Name}[/] (Model: {desktop.Model ?? "Unknown"})");
        return 0;
    }
}