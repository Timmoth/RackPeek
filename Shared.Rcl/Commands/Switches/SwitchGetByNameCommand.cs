using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Switches;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Switches;

public class SwitchGetByNameCommand(
    IServiceProvider serviceProvider
) : AsyncCommand<SwitchNameSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SwitchNameSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = serviceProvider.CreateScope();
        DescribeSwitchUseCase useCase = scope.ServiceProvider.GetRequiredService<DescribeSwitchUseCase>();

        SwitchDescription sw = await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine(
            $"[green]{sw.Name}[/]  Model: {sw.Model ?? "Unknown"}, Managed: {(sw.Managed == true ? "Yes" : "No")}, PoE: {(sw.Poe == true ? "Yes" : "No")}");

        return 0;
    }
}
