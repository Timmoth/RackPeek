namespace RackPeek.Domain.Resources.Services;

/// <summary>
///     Where a service answers, and whether a browser can do anything with it.
/// </summary>
public static class ServiceEndpoint {
    /// <summary>
    ///     Ports a browser opens over TLS. Proxmox serves its management UI on 8006 and
    ///     redirects plain HTTP, so guessing http there costs the user a round trip.
    /// </summary>
    private static readonly HashSet<int> _https = [443, 8006, 8443, 9443];

    /// <summary>Ports a browser opens in the clear.</summary>
    private static readonly HashSet<int> _http = [
        80, 631, 3000, 5000, 7860, 8000, 8080, 8096, 8123, 9000, 9090, 11434, 32400
    ];

    /// <summary>
    ///     The address a service answers on — <c>10.0.50.105:3000</c> — for showing to a
    ///     person. Empty when there is no address to show. This is display text and never
    ///     a link: see <see cref="BrowsableUrl" /> for that.
    /// </summary>
    public static string Describe(Network? network) {
        if (string.IsNullOrWhiteSpace(network?.Ip))
            return string.Empty;

        return network.Port.HasValue
            ? $"{network.Ip}:{network.Port.Value}"
            : network.Ip;
    }

    /// <summary>
    ///     A link a browser can actually follow, or null when it cannot.
    ///     <para>
    ///         A URL somebody typed always wins. Failing that the scheme has to be
    ///         inferred, and a port number is a convention rather than a promise — so this
    ///         answers only where the convention is a web one. SSH, SMB, MQTT, DNS and
    ///         anything uncurated get no link at all, which is more useful than an
    ///         <c>http://</c> that cannot load: a dead link invites a click and wastes it.
    ///     </para>
    ///     <para>
    ///         <c>protocol</c> is only consulted when it names a scheme. Discovery writes
    ///         the transport there — <c>TCP</c> — which says nothing about what rides on
    ///         top of it.
    ///     </para>
    /// </summary>
    public static string? BrowsableUrl(Network? network, string? fallbackIp = null) {
        if (network == null)
            return null;

        if (!string.IsNullOrWhiteSpace(network.Url))
            return network.Url;

        var ip = !string.IsNullOrWhiteSpace(network.Ip) ? network.Ip : fallbackIp;

        if (string.IsNullOrWhiteSpace(ip) || network.Port is not { } port)
            return null;

        var scheme = SchemeFor(port, network.Protocol);

        if (scheme == null)
            return null;

        try {
            return new UriBuilder(scheme, ip) { Port = port }.Uri.ToString();
        }
        catch (UriFormatException) {
            // Whatever is in the address field, a person put it there by hand or a
            // collector read it off a device, and neither is obliged to produce something
            // a URL can be built from. A missing link costs a click; letting this escape
            // would take down every page that renders the resource.
            return null;
        }
    }

    private static string? SchemeFor(int port, string? protocol) {
        var stated = protocol?.Trim().ToLowerInvariant();

        if (stated is "http" or "https")
            return stated;

        if (_https.Contains(port))
            return "https";

        return _http.Contains(port) ? "http" : null;
    }
}
