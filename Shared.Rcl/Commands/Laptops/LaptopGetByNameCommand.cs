using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Models;
using RackPeek.Domain.UseCases;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops;

public class LaptopGetByNameCommand(IServiceProvider provider)
    : AsyncCommand<LaptopNameSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopNameSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = provider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IGetResourceByNameUseCase<Laptop>>();

        var laptop = await useCase.ExecuteAsync(settings.Name);

        AnsiConsole.MarkupLine($"[green]{laptop.Name}[/]");
        return 0;
    }
}