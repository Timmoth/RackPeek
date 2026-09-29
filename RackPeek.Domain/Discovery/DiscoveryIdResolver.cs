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
    ///     <para>
    ///         The one exception is <paramref name="improveStoredNames" />, which lets a
    ///         stored placeholder nobody chose be replaced by a real name this payload
    ///         knows — see <see cref="CanImproveName" />. That rewrites the stored side, so
    ///         only a caller that is about to persist should ask for it, and it must hand
    ///         over <paramref name="storedConnections" /> for the same reason the incoming
    ///         side hands over its own.
    ///     </para>
    /// </summary>
    public static void ResolveNames(
        IReadOnlyList<Resource> existing,
        IReadOnlyList<Resource> incoming,
        IReadOnlyList<Connection>? connections = null,
        IReadOnlyList<Connection>? storedConnections = null,
        bool improveStoredNames = false) {
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
        Dictionary<string, Resource> existingByIp = BuildIpMap(existing);

        // Tolerant of a hand-edited file that managed to get two resources of the
        // same name: the first wins, rather than crashing the import.
        var existingByName = new Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);

        foreach (Resource resource in existing)
            existingByName.TryAdd(resource.Name, resource);

        var renames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var storedRenames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Resource resource in incomingWithId) {
            // Captured before resolution, which may null the id as part of unifying.
            var offeredName = resource.Name;
            var offeredId = resource.DiscoveryId;

            var resolved = ResolveName(resource, existingById, existingByName, existingByMac, existingByIp);

            if (resolved.Equals(offeredName, StringComparison.OrdinalIgnoreCase))
                continue;

            // The stored card is about to lend its name to this one. If that name is a
            // placeholder nobody chose and this collector has a real one, the better name
            // should win instead — so the card improves as more is learned about it.
            if (improveStoredNames
                && existingByName.TryGetValue(resolved, out Resource? stored)
                && !existingByName.ContainsKey(offeredName)
                && CanImproveName(stored, offeredName, offeredId)) {
                storedRenames[stored.Name] = offeredName;

                // The name index has to follow, or a later card in this same payload would
                // resolve onto a name that no longer exists.
                existingByName.Remove(stored.Name);
                stored.Name = offeredName;
                existingByName[offeredName] = stored;

                continue;
            }

            renames[offeredName] = resolved;
            resource.Name = resolved;
        }

        if (renames.Count > 0) {
            RewriteRunsOn(incoming, renames);
            RewriteConnections(connections, renames);
        }

        // The stored side has its own references to fix up, and its own connections. The
        // incoming side gets the same treatment because a payload may well be a re-push of
        // previously exported YAML, which still names the resource the way it was stored.
        if (storedRenames.Count > 0) {
            RewriteRunsOn(existing, storedRenames);

            // Now that the hosts answer to their new names, the services named after the
            // old ones follow. Done here so the renames below travel together.
            foreach ((var from, var to) in RenameServicesAfterTheirHost(existing, storedRenames, existingByName))
                storedRenames[from] = to;

            RewriteConnections(storedConnections, storedRenames);
            RewriteRunsOn(incoming, storedRenames);
            RewriteConnections(connections, storedRenames);
        }

        PreserveStoredRunsOn(incomingWithId, incoming, existingById, existingByName);
        AnchorRunsOnByIp(existing, incoming, existingByName);
    }

    /// <summary>
    ///     Carries a service's name along when the host it runs on stops being a
    ///     placeholder.
    ///     <para>
    ///         A sweep names what it finds on a port after the host it found it on, so a
    ///         machine it could only call <c>host-1a2b3c4d</c> gets a <c>host-1a2b3c4d-ssh</c>
    ///         beside it. When the firewall or the hypervisor later supplies the real name
    ///         the host becomes <c>forgejo</c> and the service is left announcing a machine
    ///         that no longer exists — the link still resolves, but the name reads as a
    ///         leftover, which is exactly what it is.
    ///     </para>
    ///     <para>
    ///         Only names this collector's own convention produced are touched: the
    ///         service must be named for the old host and must actually run on it, must
    ///         not be a name a person chose, and the name it would take must be free.
    ///     </para>
    /// </summary>
    private static Dictionary<string, string> RenameServicesAfterTheirHost(
        IReadOnlyList<Resource> existing,
        Dictionary<string, string> hostRenames,
        Dictionary<string, Resource> existingByName) {
        var renamed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Service service in existing.OfType<Service>()) {
            if (service.IsUserNamed())
                continue;

            foreach ((var oldHost, var newHost) in hostRenames) {
                if (!service.Name.StartsWith($"{oldHost}-", StringComparison.OrdinalIgnoreCase))
                    continue;

                // runsOn has already been rewritten, so this is the new name by now. A
                // service merely named like the host without running on it is a
                // coincidence, and coincidences are not renamed.
                if (!service.RunsOn.Contains(newHost, StringComparer.OrdinalIgnoreCase))
                    continue;

                var candidate = $"{newHost}{service.Name[oldHost.Length..]}";

                if (existingByName.ContainsKey(candidate))
                    break;

                renamed[service.Name] = candidate;
                existingByName.Remove(service.Name);
                service.Name = candidate;
                existingByName[candidate] = service;

                break;
            }
        }

        return renamed;
    }

    /// <summary>
    ///     Whether a stored card's name is a placeholder that this collector can improve
    ///     on.
    ///     <para>
    ///         A discovered card is named from whatever the collector could see, and when
    ///         that was nothing it falls back to a slug of its own id — <c>host-1a2b3c4d</c>
    ///         says only that something is there. A later run, or a collector that can see
    ///         more, often does know the machine's name: a firewall knows what it handed
    ///         out over DHCP, a hypervisor knows what its guest is called. Keeping the
    ///         placeholder in that case would mean the inventory never improved.
    ///     </para>
    ///     <para>
    ///         Only ever placeholder to real name, and never over a name a person chose.
    ///         Real to real is left alone on purpose: two collectors that each know a
    ///         different name for a machine would otherwise rename it back and forth on
    ///         every run.
    ///     </para>
    /// </summary>
    private static bool CanImproveName(Resource stored, string offeredName, string? offeredId) =>
        !stored.IsUserNamed()
        && IsGeneratedName(stored.Name, stored.DiscoveryId)
        && !IsGeneratedName(offeredName, offeredId);

    /// <summary>
    ///     Whether a name is the slug-of-its-own-id form <see cref="DiscoveryNaming.Suggest" />
    ///     falls back to when the collector had nothing better to offer.
    /// </summary>
    private static bool IsGeneratedName(string name, string? discoveryId) =>
        !string.IsNullOrWhiteSpace(discoveryId)
        && name.EndsWith($"-{DiscoveryId.ShortSuffix(discoveryId)}", StringComparison.OrdinalIgnoreCase);

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
        // Stored systems are gathered separately because they win — see below.
        Dictionary<string, List<string>> storedByIp = IndexByIp(existing);
        Dictionary<string, List<string>> arrivingByIp = IndexByIp(incoming);

        foreach (Service service in services) {
            var ip = service.Network?.Ip;

            if (string.IsNullOrWhiteSpace(ip))
                continue;

            var anchored = service.RunsOn.Any(name =>
                existingByName.ContainsKey(name) || incomingNames.Contains(name));

            if (anchored)
                continue;

            // A stored system beats one arriving in this payload when both claim the
            // address. They are usually the same machine seen twice — a hypervisor knows
            // its guest by name and specification, a sweep only found something
            // answering — and the stored card is the one a person recognises.
            List<string>? candidates =
                storedByIp.TryGetValue(ip, out List<string>? stored) ? stored
                : arrivingByIp.TryGetValue(ip, out List<string>? arriving) ? arriving
                : null;

            if (candidates is not [var host])
                continue;

            service.RunsOn = [host];
        }
    }

    private static Dictionary<string, List<string>> IndexByIp(IReadOnlyList<Resource> resources) {
        var byIp = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (SystemResource system in resources.OfType<SystemResource>()) {
            if (string.IsNullOrWhiteSpace(system.Ip))
                continue;

            if (!byIp.TryGetValue(system.Ip, out List<string>? names))
                byIp[system.Ip] = names = [];

            if (!names.Contains(system.Name, StringComparer.OrdinalIgnoreCase))
                names.Add(system.Name);
        }

        return byIp;
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
        Dictionary<string, Resource> existingByMac,
        Dictionary<string, Resource> existingByIp) {
        // Known id: the stored resource wins on name, whatever the user has renamed it to.
        if (existingById.TryGetValue(resource.DiscoveryId!, out Resource? matched))
            return matched.Name;

        // Unknown id, but a MAC in common with exactly one stored card: the same
        // physical machine seen by two collectors, unified onto the stored card.
        if (TryUnifyByMac(resource, existingByMac, out var unifiedName))
            return unifiedName;

        if (TryUnifyByIp(resource, existingByIp, out unifiedName))
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

    /// <summary>
    ///     The bridge for machines a sweep cannot identify by MAC at all: ARP is
    ///     link-local, so a host on any subnet but the scanner's own yields no MAC and
    ///     its identity falls back to its address. Once a hypervisor reports its guests'
    ///     addresses, that same address is the only thing tying the sweep's find to the
    ///     guest the inventory describes in full.
    ///     <para>
    ///         Which of the two arrived first must not matter, so this reads the same in
    ///         both directions: one scan-grade card that produced no MAC, one agent-grade
    ///         card, one address, same kind. The agent-grade identity always wins — it is
    ///         dropped from the incoming card when the incoming card is the scan (so the
    ///         merge cannot downgrade the stored one) and kept when the incoming card is
    ///         the agent (so the merge upgrades the stored one).
    ///     </para>
    ///     <para>
    ///         Two scan cards can bridge as well, but only when exactly one of them saw a
    ///         MAC. A firewall's neighbour table gives an address <em>and</em> the NIC
    ///         answering at it; a sweep of a subnet it does not sit on gives an address
    ///         and nothing else. Those are not two stand-ins — one is a direct observation
    ///         of a specific interface and the other is "something replied" — so the
    ///         MAC-bearing card wins the identity and the address-only card folds into it.
    ///     </para>
    ///     <para>
    ///         Narrow on purpose. Against an agent-grade card the scan side must have no
    ///         MAC at all: one that has a MAC either unified through the MAC bridge
    ///         already or genuinely disagrees, and disagreement is not evidence. Two cards
    ///         of equal standing never bridge — both agent-grade, both scan-grade with a
    ///         MAC, or both scan-grade without one — because an address adds nothing when
    ///         neither side can better it. And the address must be claimed by exactly one
    ///         stored card, which <see cref="BuildIpMap" /> guarantees: two cards on one
    ///         address is a conflict or an overlapping subnet, neither of which is
    ///         evidence of anything.
    ///     </para>
    /// </summary>
    private static bool TryUnifyByIp(
        Resource resource,
        Dictionary<string, Resource> existingByIp,
        out string unifiedName) {
        unifiedName = string.Empty;

        if (resource is not SystemResource { Ip: { } ip } || string.IsNullOrWhiteSpace(ip))
            return false;

        if (!existingByIp.TryGetValue(ip, out Resource? stored)
            || stored.GetType() != resource.GetType())
            return false;

        var incomingIsNet = DiscoveryId.Scheme(resource.DiscoveryId) == DiscoveryId.NetworkScheme;
        var storedIsNet = DiscoveryId.Scheme(stored.DiscoveryId) == DiscoveryId.NetworkScheme;

        var incomingHasMac = MacsOf(resource).Any();
        var storedHasMac = MacsOf(stored).Any();

        // Which side holds the weaker identity, and so folds into the other. Null means
        // the two are of equal standing and the address settles nothing.
        bool? incomingIsWeaker =
            incomingIsNet != storedIsNet
                // Agent grade against scan grade. The scan is the weaker one, but only
                // when it saw no MAC of its own — one that did either unified through the
                // MAC bridge already or disagrees with the card it would be folded into.
                ? (incomingIsNet ? incomingHasMac : storedHasMac) ? null : incomingIsNet
            : !incomingIsNet
                // Two agent-grade identities. A guest and the machine-id of the OS inside
                // it are two cards on purpose; sharing an address does not change that.
                ? null
                // Two scan-grade cards: a MAC beats an address, and nothing beats nothing.
                : incomingHasMac == storedHasMac ? null : !incomingHasMac;

        if (incomingIsWeaker is not { } weaker)
            return false;

        // Same as the MAC bridge: the weaker identity is dropped so the merge cannot
        // downgrade the stronger one. Where the stronger card is the one arriving, its id
        // survives and the merge stamps it onto the stored card instead.
        if (weaker)
            resource.DiscoveryId = null;

        unifiedName = stored.Name;

        return true;
    }

    /// <summary>
    ///     Systems by address, excluding any address more than one of them claims — an
    ///     ambiguous address is not evidence.
    /// </summary>
    private static Dictionary<string, Resource> BuildIpMap(IReadOnlyList<Resource> existing) {
        var byIp = new Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);
        var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (SystemResource system in existing.OfType<SystemResource>()) {
            if (string.IsNullOrWhiteSpace(system.Ip))
                continue;

            if (!byIp.TryAdd(system.Ip, system))
                ambiguous.Add(system.Ip);
        }

        foreach (var ip in ambiguous)
            byIp.Remove(ip);

        return byIp;
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
