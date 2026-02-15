using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Servers.Ram;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Servers.Ram;

public class ServerRamSetSettings : ServerNameSettings
{
    [CommandOption("--ram <GB>")]
    [Description("RAM capacity in GB.")]
    public int? RamGb { get; set; }

    [CommandOption("--mts <MTs>")]
    [Description("RAM speed in MT/s.")]
    public int? RamMts { get; set; }
}

public class ServerRamSetCommand(IServiceProvider serviceProvider)
    : AsyncCommand<ServerRamSetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerRamSetSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<SetServerRamUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.RamGb,
            settings.RamMts);

        AnsiConsole.MarkupLine($"[green]RAM updated on '{settings.Name}'.[/]");
        return 0;
    }
}
