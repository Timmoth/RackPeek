using System.ComponentModel;
using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.SystemResources;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shared.Rcl.Commands.Discovery;

public sealed class DiscoverSystemSettings : DiscoverSettings {
    [CommandOption("-n|--name <NAME>")]
    [Description("Name for this machine. Defaults to its hostname. Recommended when running from a timer.")]
    public string? Name { get; init; }
}

/// <summary>Inspects the machine it is running on and emits it as a System resource.</summary>
public sealed class DiscoverSystemCommand(IEnumerable<ISystemProbe> probes)
    : AsyncCommand<DiscoverSystemSettings> {
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        DiscoverSystemSettings settings,
        CancellationToken cancellationToken) {
        SystemFacts? facts = await SystemProbes.TryReadHostAsync(probes, cancellationToken);

        if (facts == null) {
            AnsiConsole.MarkupLine(
                "[red]No probe for this platform.[/] System discovery currently supports Linux and macOS.");

            return 1;
        }

        if (facts.MachineId == null)
            AnsiConsole.MarkupLine(
                "[yellow]Warning:[/] no machine id available, so the hostname is being used as this " +
                "machine's identity. Renaming the host will look like a new machine.");

        SystemResource resource = SystemResourceMapper.ToResource(facts, settings.Name);

        return await DiscoveryOutput.EmitAsync([resource], settings, cancellationToken);
    }
}
