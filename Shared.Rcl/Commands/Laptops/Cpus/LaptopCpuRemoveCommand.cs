using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.UseCases.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Cpus;

public class LaptopCpuRemoveSettings : CommandSettings {
    [CommandArgument(0, "<Laptop>")]
    [Description("The name of the Laptop.")]
    public string LaptopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the Laptop cpu to remove.")]
    public int Index { get; set; }
}

public class LaptopCpuRemoveCommand(IServiceProvider provider)
    : AsyncCommand<LaptopCpuRemoveSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopCpuRemoveSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        IRemoveCpuUseCase<Laptop> useCase = scope.ServiceProvider.GetRequiredService<IRemoveCpuUseCase<Laptop>>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Index);

        AnsiConsole.MarkupLine($"[green]CPU #{settings.Index} removed from Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}
