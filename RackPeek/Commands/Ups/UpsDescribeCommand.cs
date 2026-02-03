using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.UpsUnits;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Ups;

public class UpsDescribeCommand(IServiceProvider provider)
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

        var grid = new Grid()
            .AddColumn()
            .AddColumn();

        grid.AddRow("Name:", ups.Name);
        grid.AddRow("Model:", ups.Model ?? "Unknown");
        grid.AddRow("VA:", ups.Va?.ToString() ?? "Unknown");

        AnsiConsole.Write(new Panel(grid).Header("UPS").Border(BoxBorder.Rounded));

        return 0;
    }
}