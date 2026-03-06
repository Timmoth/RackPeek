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
        using IServiceScope scope = serviceProvider.CreateScope();
        DescribeRouterUseCase useCase = scope.ServiceProvider.GetRequiredService<DescribeRouterUseCase>();

        RouterDescription sw = await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine(
            $"[green]{sw.Name}[/]  Model: {sw.Model ?? "Unknown"}, Managed: {(sw.Managed == true ? "Yes" : "No")}, PoE: {(sw.Poe == true ? "Yes" : "No")}");

        return 0;
    }
}
