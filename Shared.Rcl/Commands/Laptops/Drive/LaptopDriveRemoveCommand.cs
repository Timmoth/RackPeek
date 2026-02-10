using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Laptops.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Laptops.Drive;

public class LaptopDriveRemoveCommand(IServiceProvider provider)
    : AsyncCommand<LaptopDriveRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopDriveRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RemoveLaptopDriveUseCase>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]Drive #{settings.Index} removed from Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}