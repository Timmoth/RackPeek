using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.UseCases.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Drive;

public class LaptopDriveAddCommand(IServiceProvider provider)
    : AsyncCommand<LaptopDriveAddSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopDriveAddSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddDriveUseCase<Laptop>>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Type, settings.Size);

        AnsiConsole.MarkupLine($"[green]Drive added to Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}