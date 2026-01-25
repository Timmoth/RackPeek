using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain.Resources.Hardware.Server.Cpu;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RackPeek.Commands.Server.Cpus;
public class ServerCpuRemoveSettings : ServerNameSettings
{
    [CommandOption("--index <INDEX>")]
    public int Index { get; set; }
}
public class ServerCpuRemoveCommand(IServiceProvider serviceProvider) : AsyncCommand<ServerCpuRemoveSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ServerCpuRemoveSettings settings,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RemoveCpuUseCase>();

        await useCase.ExecuteAsync(
            settings.Name,
            settings.Index);

        AnsiConsole.MarkupLine($"[green]CPU {settings.Index} removed from '{settings.Name}'.[/]");
        return 0;
    }
}