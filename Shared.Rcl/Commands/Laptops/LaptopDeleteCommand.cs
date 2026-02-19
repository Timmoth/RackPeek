using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Laptops;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Laptops;

public class LaptopDeleteCommand(IServiceProvider provider)
    : AsyncCommand<LaptopNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IDeleteResourceUseCase<Laptop>>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Laptop '{settings.Name}' deleted.[/]");
        return 0;
    }
}