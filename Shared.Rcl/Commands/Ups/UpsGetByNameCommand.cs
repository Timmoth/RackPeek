using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.UpsUnits;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Ups;

public class UpsGetByNameCommand(IServiceProvider provider)
    : AsyncCommand<UpsNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpsNameSettings settings,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = provider.CreateScope();
        DescribeUpsUseCase useCase = scope.ServiceProvider.GetRequiredService<DescribeUpsUseCase>();

        UpsDescription ups = await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine(
            $"[green]{ups.Name}[/]  Model: {ups.Model ?? "Unknown"}, VA: {ups.Va?.ToString() ?? "Unknown"}");

        return 0;
    }
}
