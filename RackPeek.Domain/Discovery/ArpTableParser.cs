using System.Net;
using System.Net.Sockets;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     Reads an ARP table into ip → MAC, from either format the probe can produce:
///     Linux's <c>/proc/net/arp</c> or BSD/macOS <c>arp -an</c> output. MACs are
///     normalised (lowercase, zero-padded octets) because macOS prints <c>1:0:5e:…</c>
///     where Linux prints <c>01:00:5e:…</c> — and the MAC seeds the discovery id, so
///     the same machine must hash the same from every workstation. Pure; never throws.
/// </summary>
public static class ArpTableParser {
    public static IReadOnlyDictionary<string, string> Parse(string? text) {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(text))
            return result;

        foreach (var line in text.Split('\n')) {
            (string Ip, string Mac)? entry = ParseLine(line.Trim());

            if (entry != null)
                result.TryAdd(entry.Value.Ip, entry.Value.Mac);
        }

        return result;
    }

    private static (string Ip, string Mac)? ParseLine(string line) {
        if (line.Length == 0)
            return null;

        // BSD/macOS: "? (192.168.1.1) at a4:91:b1:4e:3c:20 on en0 ifscope [ethernet]"
        var open = line.IndexOf('(');
        var close = line.IndexOf(')');

        if (open >= 0 && close > open) {
            var ip = line[(open + 1)..close];
            var at = line.IndexOf(" at ", close, StringComparison.Ordinal);

            if (at < 0 || !IsIpv4(ip))
                return null;

            var rest = line[(at + 4)..];
            var end = rest.IndexOf(' ');
            var mac = NormaliseMac(end > 0 ? rest[..end] : rest);

            return mac == null ? null : (ip, mac);
        }

        var columns = line.Split(' ', '\t', StringSplitOptions.RemoveEmptyEntries);

        if (columns.Length < 2 || !IsIpv4(columns[0]))
            return null;

        // Linux /proc/net/arp: "192.168.1.1  0x1  0x2  a4:91:b1:4e:3c:20  *  eth0"
        if (columns.Length >= 4 && columns[1].StartsWith("0x", StringComparison.Ordinal)) {
            // Flags 0x0 marks an entry the kernel gave up resolving.
            if (columns[2] == "0x0")
                return null;

            var linuxMac = NormaliseMac(columns[3]);

            return linuxMac == null ? null : (columns[0], linuxMac);
        }

        // Windows arp -a: "192.168.1.1           a4-91-b1-4e-3c-20     dynamic"
        var windowsMac = NormaliseMac(columns[1]);

        return windowsMac == null ? null : (columns[0], windowsMac);
    }

    /// <summary>
    ///     Lowercase, colon-separated, zero-padded — or null for anything that is not a
    ///     usable MAC. Accepts Windows' dash separators so the same machine hashes the
    ///     same from every platform's ARP output.
    /// </summary>
    public static string? NormaliseMac(string? raw) {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var parts = raw.Trim().Split(':', '-');

        if (parts.Length != 6)
            return null;

        var octets = new string[6];

        for (var i = 0; i < 6; i++) {
            var part = parts[i];

            if (part.Length is 0 or > 2 || !part.All(Uri.IsHexDigit))
                return null;

            octets[i] = part.Length == 1 ? "0" + char.ToLowerInvariant(part[0]) : part.ToLowerInvariant();
        }

        var mac = string.Join(':', octets);

        // All-zero means the neighbour never answered — no identity there.
        return mac == "00:00:00:00:00:00" ? null : mac;
    }

    private static bool IsIpv4(string value) =>
        IPAddress.TryParse(value, out IPAddress? ip) && ip.AddressFamily == AddressFamily.InterNetwork;
}
