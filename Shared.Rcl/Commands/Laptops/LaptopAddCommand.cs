using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops;

public class LaptopAddCommand(IServiceProvider provider)
    : AsyncCommand<LaptopNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddResourceUseCase<Laptop>>();

        await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]Laptop '{settings.Name}' added.[/]");
        return 0;
    }
}