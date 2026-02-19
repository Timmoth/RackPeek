using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Laptops.Gpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Gpus;

public class LaptopGpuRemoveCommand(IServiceProvider provider)
    : AsyncCommand<LaptopGpuRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopGpuRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RemoveLaptopGpuUseCase>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]GPU #{settings.Index} removed from Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}