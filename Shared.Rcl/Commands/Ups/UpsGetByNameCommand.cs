using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.UpsUnits;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Ups;

public class UpsGetByNameCommand(IServiceProvider provider)
    : AsyncCommand<UpsNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpsNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<DescribeUpsUseCase>();

        var ups = await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine(
            $"[green]{ups.Name}[/]  Model: {ups.Model ?? "Unknown"}, VA: {ups.Va?.ToString() ?? "Unknown"}");

        return 0;
    }
}