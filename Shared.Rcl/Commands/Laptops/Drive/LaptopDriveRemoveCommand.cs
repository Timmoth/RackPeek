using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Laptops;
using RackPeek.Domain.UseCases.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Drive;

public class LaptopDriveRemoveCommand(IServiceProvider provider)
    : AsyncCommand<LaptopDriveRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopDriveRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRemoveDriveUseCase<Laptop>>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]Drive #{settings.Index} removed from Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}