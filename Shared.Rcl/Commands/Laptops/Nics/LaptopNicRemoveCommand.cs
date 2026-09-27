using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.UseCases.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Nics;

public class LaptopNicRemoveCommand(IServiceProvider provider)
    : AsyncCommand<LaptopNicRemoveSettings> {
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopNicRemoveSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        IRemovePortUseCase<Laptop> useCase = scope.ServiceProvider.GetRequiredService<IRemovePortUseCase<Laptop>>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]NIC #{settings.Index} removed from Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}
