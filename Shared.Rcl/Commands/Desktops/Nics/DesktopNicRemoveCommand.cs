using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.UseCases.Nics;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Desktops.Nics;

public class DesktopNicRemoveSettings : CommandSettings
{
    [CommandArgument(0, "<desktop>")]
    [Description("The desktop name.")]
    public string DesktopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the nic to remove.")]
    public int Index { get; set; }
}

public class DesktopNicRemoveCommand(IServiceProvider provider)
    : AsyncCommand<DesktopNicRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DesktopNicRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveNicUseCase<Desktop>>();

        await useCase.ExecuteAsync(settings.DesktopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]NIC #{settings.Index} removed from desktop '{settings.DesktopName}'.[/]");
        return 0;
    }
}