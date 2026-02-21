using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.UseCases.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Cpus;

public class LaptopCpuRemoveCommand(IServiceProvider provider)
    : AsyncCommand<LaptopCpuRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopCpuRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveCpuUseCase<Laptop>>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]CPU #{settings.Index} removed from Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}