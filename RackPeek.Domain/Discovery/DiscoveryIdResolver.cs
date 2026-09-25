using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Connections;

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

        // Tolerant of a hand-edited file that managed to get two resources of the
        // same name: the first wins, rather than crashing the import.
        var existingByName = new Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);

        foreach (Resource resource in existing)
            existingByName.TryAdd(resource.Name, resource);

        var renames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Resource resource in incomingWithId) {
            var resolved = ResolveName(resource, existingById, existingByName);

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
            if (resource.RunsOn.Count == 0
                || !existingById.TryGetValue(resource.DiscoveryId!, out Resource? stored)
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
        Dictionary<string, Resource> existingByName) {
        // Known id: the stored resource wins on name, whatever the user has renamed it to.
        if (existingById.TryGetValue(resource.DiscoveryId!, out Resource? matched))
            return matched.Name;

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
