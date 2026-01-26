using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.AccessPoints;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.AccessPoints;

public class AccessPointGetByNameCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<AccessPointNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        AccessPointNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DescribeAccessPointUseCase>();

        var ap = await useCase.ExecuteAsync(settings.Name);

        if (ap == null)
        {
            AnsiConsole.MarkupLine($"[red]Access point '{settings.Name}' not found.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine(
            $"[green]{ap.Name}[/]  Model: {ap.Model ?? "Unknown"}, Speed: {(ap.Speed?.ToString() ?? "Unknown")} Gbps");

        return 0;
    }
}