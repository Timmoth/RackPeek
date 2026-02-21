using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Routers;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Routers;

public class RouterGetByNameCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<RouterNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        RouterNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DescribeRouterUseCase>();

        var sw = await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine(
            $"[green]{sw.Name}[/]  Model: {sw.Model ?? "Unknown"}, Managed: {(sw.Managed == true ? "Yes" : "No")}, PoE: {(sw.Poe == true ? "Yes" : "No")}");

        return 0;
    }
}