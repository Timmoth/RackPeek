using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.UseCases.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Cpus;

public class LaptopCpuAddSettings : CommandSettings {
    [CommandArgument(0, "<Laptop>")]
    [Description("The Laptop name.")]
    public string LaptopName { get; set; } = default!;

    [CommandOption("--model")]
    [Description("The model name.")]
    public string? Model { get; set; }

    [CommandOption("--cores")]
    [Description("The number of cpu cores.")]
    public int? Cores { get; set; }

    [CommandOption("--threads")]
    [Description("The number of cpu threads.")]
    public int? Threads { get; set; }
}

public class LaptopCpuAddCommand(IServiceProvider provider)
    : AsyncCommand<LaptopCpuAddSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopCpuAddSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        IAddCpuUseCase<Laptop> useCase = scope.ServiceProvider.GetRequiredService<IAddCpuUseCase<Laptop>>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Model, settings.Cores, settings.Threads);

        AnsiConsole.MarkupLine($"[green]CPU added to Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}
