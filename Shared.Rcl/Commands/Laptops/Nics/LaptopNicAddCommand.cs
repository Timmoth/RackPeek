using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Nics;

public class LaptopNicAddCommand(IServiceProvider provider)
    : AsyncCommand<LaptopNicAddSettings> {
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopNicAddSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        IAddPortUseCase<Laptop> useCase = scope.ServiceProvider.GetRequiredService<IAddPortUseCase<Laptop>>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Type, settings.Speed, settings.Ports);

        AnsiConsole.MarkupLine($"[green]NIC added to Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}
