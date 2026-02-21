using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.Resources.SubResources;
using RackPeek.Domain.UseCases.Gpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Gpus;

public class LaptopGpuSetCommand(IServiceProvider provider)
    : AsyncCommand<LaptopGpuSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopGpuSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateGpuUseCase<Laptop>>();

        var gpu = new Gpu
        {
            Model = settings.Model,
            Vram = settings.Vram
        };

        await useCase.ExecuteAsync(settings.LaptopName, settings.Index, settings.Model, settings.Vram);

        AnsiConsole.MarkupLine($"[green]GPU #{settings.Index} updated on Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}