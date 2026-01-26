using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Switches;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Switches;

public class SwitchGetByNameCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<SwitchNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SwitchNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DescribeSwitchUseCase>();

        var sw = await useCase.ExecuteAsync(settings.Name);

        if (sw == null)
        {
            AnsiConsole.MarkupLine($"[red]Switch '{settings.Name}' not found.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine(
            $"[green]{sw.Name}[/]  Model: {sw.Model ?? "Unknown"}, Managed: {(sw.Managed == true ? "Yes" : "No")}, PoE: {(sw.Poe == true ? "Yes" : "No")}");

        return 0;
    }
}