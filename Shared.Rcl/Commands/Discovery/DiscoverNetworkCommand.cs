using System.ComponentModel;
using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services.Networking;
using Spectre.Console;
using Spectre.Console.Cli;
using NetworkCidr = RackPeek.Domain.Resources.Services.Networking.Cidr;

namespace Shared.Rcl.Commands.Discovery;

public sealed class DiscoverNetworkSettings : DiscoverSettings {
    /// <summary>Sweeping wider than a /16 is 65k+ hosts — a typo, not a homelab.</summary>
    public const int MinPrefix = 16;

    [CommandOption("--cidr <CIDR>")]
    [Description("Subnet to sweep, e.g. 192.168.1.0/24. Defaults to this machine's own subnet.")]
    public string? Cidr { get; init; }

    [CommandOption("--ports <LIST>")]
    [Description("TCP ports probed to catch hosts that ignore ping, e.g. 22,80,443. " +
                 "Defaults to a curated homelab list.")]
    public string? Ports { get; init; }

    [CommandOption("--timeout <MS>")]
    [Description("Milliseconds to wait on each port probe.")]
    public int Timeout { get; init; } = 500;

    [CommandOption("--parallel <N>")]
    [Description("How many hosts to probe at once.")]
    public int Parallel { get; init; } = 128;

    public IReadOnlyList<int> ResolvedPorts =>
        string.IsNullOrWhiteSpace(Ports) ? WellKnownPorts.Defaults : ParsePorts(Ports)!;

    public override ValidationResult Validate() {
        if (Cidr != null) {
            NetworkCidr parsed;

            try {
                parsed = NetworkCidr.Parse(Cidr);
            }
            catch {
                return ValidationResult.Error(
                    $"'{Cidr}' is not a usable CIDR block. Use e.g. --cidr 192.168.1.0/24");
            }

            if (parsed.Prefix < MinPrefix)
                return ValidationResult.Error(
                    $"/{parsed.Prefix} is more than 65,534 hosts. Narrow the sweep to /{MinPrefix} or smaller.");
        }

        if (Ports != null && ParsePorts(Ports) == null)
            return ValidationResult.Error(
                $"'{Ports}' is not a usable port list. Use e.g. --ports 22,80,443");

        if (Timeout is < 1 or > 60_000)
            return ValidationResult.Error("--timeout must be between 1 and 60000 milliseconds.");

        if (Parallel is < 1 or > 1024)
            return ValidationResult.Error("--parallel must be between 1 and 1024.");

        return base.Validate();
    }

    private static IReadOnlyList<int>? ParsePorts(string list) {
        var ports = new List<int>();

        foreach (var part in list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            if (!int.TryParse(part, out var port) || port is < 1 or > 65_535)
                return null;

            ports.Add(port);
        }

        return ports.Count == 0 ? null : ports;
    }
}

/// <summary>
///     Sweeps a subnet and emits every answering host as a System resource — the
///     collector for machines nothing else can describe: no agent, no API, just an
///     address that answers.
/// </summary>
public sealed class DiscoverNetworkCommand(INetworkProbe probe)
    : AsyncCommand<DiscoverNetworkSettings> {
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        DiscoverNetworkSettings settings,
        CancellationToken cancellationToken) {
        Cidr cidr;

        if (settings.Cidr != null) {
            cidr = Cidr.Parse(settings.Cidr); // Validate() vouched for it
        }
        else {
            Cidr? detected = probe.LocalSubnet();

            if (detected == null) {
                AnsiConsole.MarkupLine(
                    "[red]Could not detect this machine's subnet.[/] Pass --cidr, e.g. --cidr 192.168.1.0/24");

                return 1;
            }

            cidr = detected.Value;
        }

        var options = new NetworkScanOptions {
            Cidr = cidr,
            Ports = settings.ResolvedPorts,
            PortTimeout = TimeSpan.FromMilliseconds(settings.Timeout),
            Concurrency = settings.Parallel
        };

        var targets = NetworkScanner.EnumerateTargets(cidr).Count();

        AnsiConsole.MarkupLine(
            $"[grey]Sweeping {Markup.Escape(cidr.ToString())} — {targets} address(es), " +
            $"ping + {options.Ports.Count} TCP port(s)…[/]");

        IReadOnlyList<NetworkHostFact> hosts = await NetworkScanner.ScanAsync(probe, options, cancellationToken);

        var withoutMac = hosts.Count(h => h.Mac == null);

        if (withoutMac > 0)
            AnsiConsole.MarkupLine(
                $"[grey]{withoutMac} host(s) had no ARP entry, so their identity is seeded on the IP " +
                "address — a DHCP re-lease will make them look like new machines.[/]");

        List<Resource> resources = NetworkScanMapper.ToResources(hosts);

        return await DiscoveryOutput.EmitAsync(resources, settings, cancellationToken);
    }
}
