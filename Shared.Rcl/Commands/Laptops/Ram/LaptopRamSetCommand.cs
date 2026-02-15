using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Laptops.Ram;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Laptops.Ram;

public class LaptopRamSetSettings : CommandSettings
{
    [CommandArgument(0, "<Laptop>")]
    [Description("The Laptop name.")]
    public string LaptopName { get; set; } = default!;

    [CommandOption("--ram <GB>")]
    [Description("RAM capacity in GB.")]
    public int? RamGb { get; set; }

    [CommandOption("--mts <MTs>")]
    [Description("RAM speed in MT/s.")]
    public int? RamMts { get; set; }
}

public class LaptopRamSetCommand(IServiceProvider serviceProvider)
    : AsyncCommand<LaptopRamSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        LaptopRamSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<SetLaptopRamUseCase>();

        await useCase.ExecuteAsync(
            settings.LaptopName,
            settings.RamGb,
            settings.RamMts);

        AnsiConsole.MarkupLine($"[green]RAM updated on '{settings.LaptopName}'.[/]");
        return 0;
    }
}
