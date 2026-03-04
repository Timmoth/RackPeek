using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.AccessPoints;

public class AccessPointGetByNameCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<AccessPointNameSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        AccessPointNameSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = serviceProvider.CreateScope();
        IGetResourceByNameUseCase<AccessPoint> useCase =
            scope.ServiceProvider.GetRequiredService<IGetResourceByNameUseCase<AccessPoint>>();

        AccessPoint ap = await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine(
            $"[green]{ap.Name}[/]  Model: {ap.Model ?? "Unknown"}, Speed: {ap.Speed?.ToString() ?? "Unknown"}Gbps");

        return 0;
    }
}
