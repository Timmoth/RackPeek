using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.Services.Networking;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Discovery;

/// <summary>Maps swept hosts onto the System resources RackPeek stores. Pure.</summary>
public static class NetworkScanMapper {
    public static List<Resource> ToResources(IReadOnlyList<NetworkHostFact> hosts) {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resources = new List<Resource>();

        foreach ((NetworkHostFact host, IReadOnlyList<string> allIps) in Collapse(hosts)) {
            // The MAC is the only identity a scan can see that survives a DHCP re-lease;
            // when ARP could not provide one (a routed subnet, say) the IP has to do,
            // and the id changes if the address does — documented in the guide.
            var discoveryId = DiscoveryId.Create(
                DiscoveryId.NetworkScheme,
                host.Mac ?? $"ip:{host.Ip}");

            // A PTR record is the network's own answer and wins. Failing that, whatever a
            // service volunteered beats a hash of the MAC — "pve-node-01" over "host-1a2b3c4d".
            var label = DiscoveryNaming.HostLabel(host.Hostname);

            if (string.IsNullOrEmpty(label) && NamesAHost(host.Identity))
                label = DiscoveryNaming.HostLabel(host.Identity!.Name);

            var system = new SystemResource {
                Kind = SystemResource.KindLabel,
                Name = DiscoveryNaming.Unique(
                    DiscoveryNaming.Suggest(
                        label,
                        "host",
                        discoveryId),
                    discoveryId,
                    taken),
                DiscoveryId = discoveryId,
                // Deliberately sparse: a scan sees an address, not an OS or a type, and
                // whatever it wrote here would overwrite the real values on every rescan
                // of a card the user (or an agent collector) has since filled in.
                Ip = host.Ip
            };

            if (host.Mac != null)
                system.Labels["mac"] = host.Mac;

            // Named for what it is: the organisation that owns the NIC's OUI, which is
            // not the same claim as who made the machine — a Proxmox guest's NIC says
            // Proxmox while the box underneath it is a Dell.
            if (host.Vendor != null)
                system.Labels["nic-vendor"] = host.Vendor;

            // Kept even when the name came from somewhere else: it records what the host
            // actually said, which is how someone judges whether the name is trustworthy.
            if (host.Identity != null)
                system.Labels["identified-by"] =
                    $"{Describe(host.Identity.Source)}:{host.Identity.Port} {host.Identity.Name}";

            if (allIps.Count > 1)
                system.Labels["ips"] = string.Join(",", allIps);

            resources.Add(system);

            // Something listening on a port is a service, and a service is a resource in
            // its own right rather than a note on the host. The host says where it is;
            // each port says what it serves.
            IReadOnlyDictionary<int, ServiceIdentity> identified =
                host.Services.ToDictionary(s => s.Port);

            // A port that answered a banner probe is open by definition, so the two lists
            // agree in practice — but an identified service must not go missing if they
            // ever disagree.
            IEnumerable<int> ports = host.OpenPorts
                .Concat(identified.Keys)
                .Distinct();

            foreach (var port in ports) {
                var serviceId = DiscoveryId.Create(
                    DiscoveryId.NetworkScheme,
                    $"{host.Mac ?? $"ip:{host.Ip}"}:{port}");

                // What the service said about itself beats what its port number implies,
                // because a port is a convention and an answer is evidence. The exception
                // is an appliance whose management page just says its own name back: a
                // second card called opnsense tells no one anything, where an opnsense-https
                // sitting on opnsense says exactly what it is.
                var announced = identified.TryGetValue(port, out ServiceIdentity? found)
                    ? DiscoveryNaming.HostLabel(found.Name)
                    : string.Empty;

                var serviceLabel = announced.Length > 0
                                   && !announced.Equals(system.Name, StringComparison.OrdinalIgnoreCase)
                    ? announced
                    : $"{system.Name}-{WellKnownPorts.NameFor(port)}";

                resources.Add(new Service {
                    Kind = Service.KindLabel,
                    Name = DiscoveryNaming.Unique(
                        DiscoveryNaming.Suggest(serviceLabel, "service", serviceId),
                        serviceId,
                        taken),
                    DiscoveryId = serviceId,
                    Network = new Network {
                        Ip = host.Ip,
                        Port = port,
                        Protocol = "TCP"
                    },
                    RunsOn = [system.Name]
                });
            }
        }

        return resources;
    }

    /// <summary>
    ///     Whether an identity is a claim about the machine rather than about something
    ///     running on it. Only a certificate is: an X.509 common name is a host name by
    ///     construction, which is why a Proxmox node's certificate says "pve-node-01".
    ///     <para>
    ///         A page title names an application — and a host may run several, so naming
    ///         the machine after whichever answered first is arbitrary. Those become
    ///         Services instead. An SSH greeting names only the daemon; naming from it
    ///         produced five cards called "openssh" on a real sweep.
    ///     </para>
    /// </summary>
    private static bool NamesAHost(ServiceIdentity? identity) =>
        identity is { Source: IdentitySource.TlsCertificate };

    private static string Describe(IdentitySource source) => source switch {
        IdentitySource.TlsCertificate => "tls",
        IdentitySource.SshBanner => "ssh",
        IdentitySource.Http => "http",
        _ => "dns"
    };

    /// <summary>
    ///     One MAC answering on several addresses — a gateway's VIPs and aliases — is
    ///     still one machine, so it becomes one card: the lowest address as the card's
    ///     ip (deterministic), every address in an "ips" label. Anything else would make
    ///     the machine's identity depend on how many of its addresses happened to answer
    ///     a particular scan, and identity must never move between scans.
    /// </summary>
    private static IEnumerable<(NetworkHostFact Host, IReadOnlyList<string> AllIps)> Collapse(
        IReadOnlyList<NetworkHostFact> hosts) {
        var byMac = new Dictionary<string, List<NetworkHostFact>>(StringComparer.OrdinalIgnoreCase);

        foreach (NetworkHostFact host in hosts)
            if (host.Mac != null) {
                if (!byMac.TryGetValue(host.Mac, out List<NetworkHostFact>? group))
                    byMac[host.Mac] = group = [];

                group.Add(host);
            }

        var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (NetworkHostFact host in hosts) {
            if (host.Mac == null) {
                yield return (host, [host.Ip]);

                continue;
            }

            if (!emitted.Add(host.Mac))
                continue;

            var group = byMac[host.Mac]
                .OrderBy(h => IpHelper.ToUInt32(h.Ip))
                .ToList();

            NetworkHostFact primary = group[0];

            // Any name in the group beats none: a VIP rarely has its own PTR record.
            var hostname = group.Select(h => h.Hostname).FirstOrDefault(n => n != null);

            yield return (
                primary with { Hostname = hostname },
                group.Select(h => h.Ip).ToList());
        }
    }
}
