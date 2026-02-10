using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Laptops.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Laptops.Cpus;

public class LaptopCpuRemoveCommand(IServiceProvider provider)
    : AsyncCommand<LaptopCpuRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopCpuRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RemoveLaptopCpuUseCase>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]CPU #{settings.Index} removed from Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}