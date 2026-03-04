using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.UseCases.Cpus;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Laptops.Cpus;

public class LaptopCpuSetSettings : CommandSettings {
    [CommandArgument(0, "<Laptop>")]
    [Description("The Laptop name.")]
    public string LaptopName { get; set; } = default!;

    [CommandArgument(1, "<index>")]
    [Description("The index of the Laptop cpu.")]
    public int Index { get; set; }

    [CommandOption("--model")]
    [Description("The cpu model.")]
    public string? Model { get; set; }

    [CommandOption("--cores")]
    [Description("The number of cpu cores.")]
    public int? Cores { get; set; }

    [CommandOption("--threads")]
    [Description("The number of cpu threads.")]
    public int? Threads { get; set; }
}

public class LaptopCpuSetCommand(IServiceProvider provider)
    : AsyncCommand<LaptopCpuSetSettings> {
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopCpuSetSettings settings,
        CancellationToken cancellationToken) {
        using IServiceScope scope = provider.CreateScope();
        IUpdateCpuUseCase<Laptop> useCase = scope.ServiceProvider.GetRequiredService<IUpdateCpuUseCase<Laptop>>();

        await useCase.ExecuteAsync(settings.LaptopName, settings.Index, settings.Model, settings.Cores,
            settings.Threads);

        AnsiConsole.MarkupLine($"[green]CPU #{settings.Index} updated on Laptop '{settings.LaptopName}'.[/]");
        return 0;
    }
}
