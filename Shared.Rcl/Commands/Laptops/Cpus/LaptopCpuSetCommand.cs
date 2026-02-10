using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Laptops.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Laptops.Cpus;

public class LaptopCpuSetCommand(IServiceProvider provider)
    : AsyncCommand<LaptopCpuSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopCpuSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateLaptopCpuUseCase>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Index, settings.Model, settings.Cores,
            settings.Threads);

        AnsiConsole.MarkupLine($"[green]CPU #{settings.Index} updated on Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}