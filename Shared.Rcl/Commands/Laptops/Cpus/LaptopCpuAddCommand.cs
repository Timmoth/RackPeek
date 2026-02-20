using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Laptops;
using RackPeek.Domain.UseCases.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Cpus;

public class LaptopCpuAddCommand(IServiceProvider provider)
    : AsyncCommand<LaptopCpuAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopCpuAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddCpuUseCase<Laptop>>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Model, settings.Cores, settings.Threads);

        AnsiConsole.MarkupLine($"[green]CPU added to Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}