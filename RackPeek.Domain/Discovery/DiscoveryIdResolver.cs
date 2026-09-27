using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Connections;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     Reconciles incoming discovered resources against what is already stored, by
///     <see cref="Resource.DiscoveryId" /> rather than by name.
///     <para>
///         Runs immediately before the merge, and only ever rewrites the *incoming*
///         names. The invariant it exists to protect: discovery never renames a
///         resource the user already has. Names are user-owned, ids are machine-owned.
///     </para>
/// </summary>
public static class DiscoveryIdResolver {
    /// <summary>
    ///     Rewrites <paramref name="incoming" /> in place so that its names line up with
    ///     the stored resources the ids point at. Also rewrites <c>runsOn</c> references
    ///     between incoming resources — and the payload's <paramref name="connections" />,
    ///     which name resources the same way — so a rename does not break the tree.
    /// </summary>
    public static void ResolveNames(
        IReadOnlyList<Resource> existing,
        IReadOnlyList<Resource> incoming,
        IReadOnlyList<Connection>? connections = null) {
        var incomingWithId = incoming
            .Where(r => !string.IsNullOrWhiteSpace(r.DiscoveryId))
            .ToList();

        if (incomingWithId.Count == 0)
            return;

        GuardAgainstDuplicates(incomingWithId, "payload");
        GuardAgainstDuplicates(existing.Where(r => !string.IsNullOrWhiteSpace(r.DiscoveryId)), "inventory");

        var existingById = existing
            .Where(r => !string.IsNullOrWhiteSpace(r.DiscoveryId))
            .ToDictionary(r => r.DiscoveryId!, r => r, StringComparer.OrdinalIgnoreCase);

        Dictionary<string, Resource> existingByMac = BuildMacMap(existing);

        // Tolerant of a hand-edited file that managed to get two resources of the
        // same name: the first wins, rather than crashing the import.
        var existingByName = new Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);

        foreach (Resource resource in existing)
            existingByName.TryAdd(resource.Name, resource);

        var renames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Resource resource in incomingWithId) {
            var resolved = ResolveName(resource, existingById, existingByName, existingByMac);

            if (resolved.Equals(resource.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            renames[resource.Name] = resolved;
            resource.Name = resolved;
        }

        if (renames.Count > 0) {
            RewriteRunsOn(incoming, renames);
            RewriteConnections(connections, renames);
        }

        PreserveStoredRunsOn(incomingWithId, incoming, existingById, existingByName);
        AnchorRunsOnByIp(existing, incoming, existingByName);
    }

    /// <summary>
    ///     Gives a service whose <c>runsOn</c> names nothing the host it is plainly
    ///     running on: the system at its own address.
    ///     <para>
    ///         A collector that cannot see the machine it is talking to has to guess the
    ///         host's name — docker over TCP sends whatever the engine calls itself, which
    ///         need not match any resource — and the link then dangles. The address is
    ///         evidence the guess is not: a service answering on 192.0.2.57 is running on
    ///         whatever owns 192.0.2.57.
    ///     </para>
    ///     <para>
    ///         Deliberately conservative. It only fills a link that resolves to nothing,
    ///         only when exactly one system claims that address, and never across a
    ///         resource that already has a working parent — an ambiguous address is no
    ///         evidence at all, and a wrong parent is worse than a missing one.
    ///     </para>
    /// </summary>
    private static void AnchorRunsOnByIp(
        IReadOnlyList<Resource> existing,
        IReadOnlyList<Resource> incoming,
        Dictionary<string, Resource> existingByName) {
        var services = incoming.OfType<Service>().ToList();

        if (services.Count == 0)
            return;

        var incomingNames = new HashSet<string>(
            incoming.Select(r => r.Name),
            StringComparer.OrdinalIgnoreCase);

        // Both sides count: the host may have arrived in this very payload (discover
        // docker emits it alongside its services) or be sitting in the inventory already.
        var systemsByIp = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (SystemResource system in existing.OfType<SystemResource>().Concat(incoming.OfType<SystemResource>())) {
            if (string.IsNullOrWhiteSpace(system.Ip))
                continue;

            if (!systemsByIp.TryGetValue(system.Ip, out List<string>? names))
                systemsByIp[system.Ip] = names = [];

            if (!names.Contains(system.Name, StringComparer.OrdinalIgnoreCase))
                names.Add(system.Name);
        }

        foreach (Service service in services) {
            var ip = service.Network?.Ip;

            if (string.IsNullOrWhiteSpace(ip))
                continue;

            var anchored = service.RunsOn.Any(name =>
                existingByName.ContainsKey(name) || incomingNames.Contains(name));

            if (anchored)
                continue;

            if (!systemsByIp.TryGetValue(ip, out List<string>? candidates) || candidates.Count != 1)
                continue;

            service.RunsOn = [candidates[0]];
        }
    }

    /// <summary>
    ///     A collector that cannot see its host — docker discovery over TCP — sends
    ///     <c>runsOn</c> as a bare hostname it cannot reconcile after the user renames
    ///     that host. When an update's runsOn points at nothing at all while the stored
    ///     resource already points at something real, the stored link is the user's truth
    ///     and re-discovery must not tear it up. A runsOn that resolves — even to a
    ///     resource arriving in the same payload — is left alone: that is a genuine move.
    /// </summary>
    private static void PreserveStoredRunsOn(
        IReadOnlyList<Resource> incomingWithId,
        IReadOnlyList<Resource> incoming,
        Dictionary<string, Resource> existingById,
        Dictionary<string, Resource> existingByName) {
        var incomingNames = new HashSet<string>(incoming.Select(r => r.Name), StringComparer.OrdinalIgnoreCase);

        foreach (Resource resource in incomingWithId) {
            // MAC unification may have nulled a scan card's id so the merge cannot
            // downgrade the stored identity — such a card has nothing to look up here.
            if (resource.RunsOn.Count == 0
                || string.IsNullOrWhiteSpace(resource.DiscoveryId)
                || !existingById.TryGetValue(resource.DiscoveryId, out Resource? stored)
                || stored.RunsOn.Count == 0)
                continue;

            var anchored = resource.RunsOn.Any(name =>
                existingByName.ContainsKey(name) || incomingNames.Contains(name));

            if (!anchored)
                resource.RunsOn = [.. stored.RunsOn];
        }
    }

    private static string ResolveName(
        Resource resource,
        Dictionary<string, Resource> existingById,
        Dictionary<string, Resource> existingByName,
        Dictionary<string, Resource> existingByMac) {
        // Known id: the stored resource wins on name, whatever the user has renamed it to.
        if (existingById.TryGetValue(resource.DiscoveryId!, out Resource? matched))
            return matched.Name;

        // Unknown id, but a MAC in common with exactly one stored card: the same
        // physical machine seen by two collectors, unified onto the stored card.
        if (TryUnifyByMac(resource, existingByMac, out var unifiedName))
            return unifiedName;

        // Unknown id and the name is free: nothing to reconcile.
        if (!existingByName.TryGetValue(resource.Name, out Resource? sameName))
            return resource.Name;

        // Taken by something carrying a different id — another machine's resource.
        var belongsToAnotherMachine = !string.IsNullOrWhiteSpace(sameName.DiscoveryId)
                                      && !sameName.DiscoveryId.Equals(
                                          resource.DiscoveryId,
                                          StringComparison.OrdinalIgnoreCase);

        // Taken by a different kind of thing. Very common: the box is documented as a
        // Server by hand and discovery reports the operating system on it as a System.
        // The merge replaces on a type change, so adopting here would delete the
        // hardware the user wrote.
        var describesSomethingElse = sameName.GetType() != resource.GetType();

        if (belongsToAnotherMachine || describesSomethingElse)
            return DiscoveryNaming.WithSuffix(
                resource.Name,
                DiscoveryId.ShortSuffix(resource.DiscoveryId!));

        // Same kind, no competing id: this is the adoption case, where the merge stamps
        // the id onto the resource the user already wrote and keeps everything in it.
        return resource.Name;
    }

    /// <summary>
    ///     The bridge between collectors that cannot derive each other's ids: the agent
    ///     records the machine's MACs (a "macs" label), the scan identifies it by one (a
    ///     "mac" label). A shared MAC on a stored card of the same kind means the same
    ///     box — the incoming card adopts the stored card's name so the merge lands on
    ///     it, and the stronger identity wins: an agent id replaces a scan id, a scan id
    ///     never replaces anything (it is nulled here so the merge cannot downgrade).
    ///     Ids from two agent-grade collectors sharing a MAC (cloned VMs, or Proxmox's
    ///     view of a guest) are never unified — that is what machine-ids are for.
    /// </summary>
    private static bool TryUnifyByMac(
        Resource resource,
        Dictionary<string, Resource> existingByMac,
        out string unifiedName) {
        unifiedName = string.Empty;

        foreach (var mac in MacsOf(resource)) {
            if (!existingByMac.TryGetValue(mac, out Resource? stored))
                continue;

            // The box the user documented as a Server and the OS a scan saw on it are
            // different cards on purpose; unification is for same-kind cards only.
            if (stored.GetType() != resource.GetType())
                continue;

            var incomingIsNet = DiscoveryId.Scheme(resource.DiscoveryId) == DiscoveryId.NetworkScheme;

            // A stored card with a MAC but no id yet: adoption, same as the name-based
            // adoption case — the incoming id gets stamped onto it by the merge.
            if (string.IsNullOrWhiteSpace(stored.DiscoveryId)) {
                unifiedName = stored.Name;

                return true;
            }

            var storedIsNet = DiscoveryId.Scheme(stored.DiscoveryId) == DiscoveryId.NetworkScheme;

            // Both scan-grade or both agent-grade: not safe to unify on a MAC alone.
            if (incomingIsNet == storedIsNet)
                continue;

            if (incomingIsNet)
                resource.DiscoveryId = null;

            unifiedName = stored.Name;

            return true;
        }

        return false;
    }

    /// <summary>The MACs a resource claims, from its "mac" and "macs" labels, normalised.</summary>
    private static IEnumerable<string> MacsOf(Resource resource) {
        IEnumerable<string?> raw = [
            resource.Labels.GetValueOrDefault("mac"),
            .. (resource.Labels.GetValueOrDefault("macs") ?? string.Empty).Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ];

        return raw
            .Select(ArpTableParser.NormaliseMac)
            .Where(mac => mac != null)
            .Select(mac => mac!)
            .Distinct();
    }

    /// <summary>
    ///     mac → the one stored resource claiming it. A MAC claimed by two stored
    ///     resources identifies nothing and is dropped: ambiguity never unifies.
    /// </summary>
    private static Dictionary<string, Resource> BuildMacMap(IReadOnlyList<Resource> existing) {
        var map = new Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);
        var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Resource resource in existing)
            foreach (var mac in MacsOf(resource))
                if (!ambiguous.Contains(mac) && !map.TryAdd(mac, resource) && !ReferenceEquals(map[mac], resource)) {
                    map.Remove(mac);
                    ambiguous.Add(mac);
                }

        return map;
    }

    private static void RewriteRunsOn(IReadOnlyList<Resource> incoming, Dictionary<string, string> renames) {
        foreach (Resource resource in incoming)
            for (var i = 0; i < resource.RunsOn.Count; i++)
                if (renames.TryGetValue(resource.RunsOn[i], out var renamed))
                    resource.RunsOn[i] = renamed;
    }

    private static void RewriteConnections(IReadOnlyList<Connection>? connections, Dictionary<string, string> renames) {
        if (connections == null)
            return;

        foreach (Connection connection in connections) {
            if (connection.A?.Resource != null && renames.TryGetValue(connection.A.Resource, out var a))
                connection.A.Resource = a;

            if (connection.B?.Resource != null && renames.TryGetValue(connection.B.Resource, out var b))
                connection.B.Resource = b;
        }
    }

    private static void GuardAgainstDuplicates(IEnumerable<Resource> resources, string scope) {
        IGrouping<string, Resource>? duplicate = resources
            .GroupBy(r => r.DiscoveryId!, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate == null)
            return;

        var names = string.Join(", ", duplicate.Select(r => r.Name));

        throw new ValidationException(
            $"Duplicate discoveryId '{duplicate.Key}' in the {scope} ({names}). " +
            "Machines cloned from a VM template often share /etc/machine-id; " +
            "run 'systemd-machine-id-setup' on the clones to give them distinct identities.");
    }
}
