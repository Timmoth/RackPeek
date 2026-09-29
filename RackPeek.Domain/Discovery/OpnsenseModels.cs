using System.Text.Json;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     One neighbour the firewall has seen, from its ARP table.
///     <para>
///         This is the record a sweep cannot produce for anything off its own segment:
///         ARP is link-local, so a host on another subnet gives a scanner an address and
///         nothing else. The firewall routes every subnet, so its table carries the MAC
///         for all of them — and a MAC is the identity that survives a DHCP re-lease.
///     </para>
/// </summary>
public sealed record OpnsenseNeighbour(
    string Ip,
    string Mac,
    string? Hostname,
    string? Manufacturer,
    string? Interface);

public static class OpnsenseResponseParser {
    /// <summary>
    ///     Reads the ARP table. OPNsense answers either a flat array or, through the
    ///     search wrapper, an object with a <c>rows</c> array; both shapes are accepted so
    ///     the caller need not care which endpoint answered.
    /// </summary>
    public static List<OpnsenseNeighbour> ParseArp(string json) {
        var neighbours = new List<OpnsenseNeighbour>();

        using var document = JsonDocument.Parse(json);

        JsonElement root = document.RootElement;

        JsonElement rows = root.ValueKind switch {
            JsonValueKind.Array => root,
            JsonValueKind.Object when root.TryGetProperty("rows", out JsonElement r) => r,
            _ => default
        };

        if (rows.ValueKind != JsonValueKind.Array)
            return neighbours;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement entry in rows.EnumerateArray()) {
            if (entry.ValueKind != JsonValueKind.Object)
                continue;

            // An address the firewall holds itself. Every routed subnet contributes one,
            // and they are all the same box — which is a Firewall, not the handful of
            // Systems this would otherwise invent.
            if (IsTrue(entry, "permanent"))
                continue;

            // The entry is still listed after it ages out; it says where something used
            // to be, which is not evidence that it is there now.
            if (IsTrue(entry, "expired"))
                continue;

            var mac = ArpTableParser.NormaliseMac(Text(entry, "mac"));
            var ip = Text(entry, "ip");

            if (mac == null || string.IsNullOrWhiteSpace(ip))
                continue;

            // Broadcast and multicast are not machines.
            if (mac is "ff:ff:ff:ff:ff:ff" || IsMulticast(mac))
                continue;

            // One row per machine: a host answering on several of the firewall's
            // interfaces is still one machine, and the first row carries its address.
            if (!seen.Add(mac))
                continue;

            neighbours.Add(new OpnsenseNeighbour(
                ip,
                mac,
                Clean(Text(entry, "hostname")),
                Clean(Text(entry, "manufacturer")),
                Clean(Text(entry, "intf_description")) ?? Clean(Text(entry, "intf"))));
        }

        return neighbours;
    }

    /// <summary>
    ///     A locally administered group address — the low bit of the first octet marks
    ///     multicast, which no host owns.
    /// </summary>
    private static bool IsMulticast(string mac) =>
        Convert.ToInt32(mac[..2], 16) % 2 == 1;

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    ///     OPNsense writes these as real booleans in some versions and as the strings
    ///     "1"/"true" in others.
    /// </summary>
    private static bool IsTrue(JsonElement element, string name) {
        if (!element.TryGetProperty(name, out JsonElement value))
            return false;

        return value.ValueKind switch {
            JsonValueKind.True => true,
            JsonValueKind.String => value.GetString() is "1" or "true" or "yes",
            JsonValueKind.Number => value.TryGetInt32(out var number) && number != 0,
            _ => false
        };
    }

    /// <summary>Blank and placeholder values arrive as empty strings or dashes.</summary>
    private static string? Clean(string? value) {
        var trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed) || trimmed is "-" or "(none)" ? null : trimmed;
    }
}
