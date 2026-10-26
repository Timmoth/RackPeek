using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Nics;

public class LaptopNicSetCommand(IServiceProvider provider)
    : AsyncCommand<LaptopNicSetSettings> {
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopNicSetSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        IUpdatePortUseCase<Laptop> useCase = scope.ServiceProvider.GetRequiredService<IUpdatePortUseCase<Laptop>>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Index, settings.Type, settings.Speed, settings.Ports);

        AnsiConsole.MarkupLine($"[green]NIC #{settings.Index} updated on Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}
