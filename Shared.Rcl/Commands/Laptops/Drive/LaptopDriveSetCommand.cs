using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Laptops.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Laptops.Drive;

public class LaptopDriveSetCommand(IServiceProvider provider)
    : AsyncCommand<LaptopDriveSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopDriveSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<UpdateLaptopDriveUseCase>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Index, settings.Type, settings.Size);

        AnsiConsole.MarkupLine($"[green]Drive #{settings.Index} updated on Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}