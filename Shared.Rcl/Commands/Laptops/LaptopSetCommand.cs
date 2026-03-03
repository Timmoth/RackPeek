using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops;

public class LaptopSetSettings : LaptopNameSettings {
    [CommandOption("--model")] public string? Model { get; set; }
}

public class LaptopSetCommand(IServiceProvider provider)
    : AsyncCommand<LaptopSetSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopSetSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        UpdateLaptopUseCase useCase = scope.ServiceProvider.GetRequiredService<UpdateLaptopUseCase>();

        await useCase.ExecuteAsync(settings.Name, settings.Model);

        AnsiConsole.MarkupLine($"[green]Laptop '{settings.Name}' updated.[/]");
        return 0;
    }
}
