using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.UseCases.Drives;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Drive;

public class LaptopDriveSetCommand(IServiceProvider provider)
    : AsyncCommand<LaptopDriveSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopDriveSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateDriveUseCase<Laptop>>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Index, settings.Type, settings.Size);

        AnsiConsole.MarkupLine($"[green]Drive #{settings.Index} updated on Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}