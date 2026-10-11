using RackPeek.Domain.Resources.Services;

namespace RackPeek.Domain.Discovery;

/// <summary>Maps containers onto the Service resources RackPeek stores. Pure.</summary>
public static class DockerServiceMapper {
    /// <summary>
    ///     Containers nothing outside the host can reach are skipped — no published port,
    ///     or every binding on a loopback address. They are not services anyone would put
    ///     on an inventory, and a Service is required to carry an address.
    /// </summary>
    /// <param name="hostSeed">
    ///     Identity of the machine the containers run on — the host's machine-id where
    ///     there is one. Part of the container's own id, so the same container name on
    ///     two different hosts stays two different resources.
    /// </param>
    public static List<Service> ToResources(
        IReadOnlyList<DockerContainer> containers,
        string hostSeed,
        string hostName,
        string? hostIp) {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return containers
            .Where(c => c.PublishedPorts.Any(p => !p.IsLoopback))
            .Select(c => ToResource(c, hostSeed, hostName, hostIp, taken))
            .ToList();
    }

    private static Service ToResource(
        DockerContainer container,
        string hostSeed,
        string hostName,
        string? hostIp,
        ISet<string> taken) {
        var discoveryId = DiscoveryId.Create(
            DiscoveryId.DockerScheme,
            $"{hostSeed}/{container.Name}");

        // A wildcard binding is reachable on the host's own address; a binding pinned to
        // one interface is only reachable there, so that address wins over the host's.
        DockerPortBinding port = container.PublishedPorts
            .Where(p => !p.IsLoopback)
            .OrderBy(p => p.IsWildcard ? 0 : 1)
            .ThenBy(p => p.HostPort)
            .First();

        return new Service {
            Kind = Service.KindLabel,
            Name = DiscoveryNaming.Unique(
                DiscoveryNaming.Suggest(container.Name, "service", discoveryId),
                discoveryId,
                taken),
            DiscoveryId = discoveryId,
            Network = new Network {
                Ip = ServiceIp(port, hostIp),
                Port = port.HostPort,
                Protocol = port.Protocol
            },
            Notes = container.Image,
            Tags = string.IsNullOrWhiteSpace(container.ComposeProject)
                ? []
                : [DiscoveryNaming.Slug(container.ComposeProject)],
            RunsOn = [hostName]
        };
    }

    /// <summary>
    ///     The inventory schema holds IPv4 only, so a binding pinned to an IPv6 address
    ///     falls back to the host's address — the right machine, if not the exact socket.
    /// </summary>
    private static string? ServiceIp(DockerPortBinding port, string? hostIp) =>
        !port.IsWildcard && System.Net.IPAddress.TryParse(port.HostIp, out System.Net.IPAddress? ip)
        && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
            ? port.HostIp
            : hostIp;
}
