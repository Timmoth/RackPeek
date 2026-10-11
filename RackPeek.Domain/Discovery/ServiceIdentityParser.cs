using System.Text.RegularExpressions;

namespace RackPeek.Domain.Discovery;

/// <summary>Where a host's name came from, best evidence first.</summary>
public enum IdentitySource {
    /// <summary>A PTR record — the network's own answer, so it wins.</summary>
    ReverseDns,

    /// <summary>The Common Name on the certificate a TLS port presented.</summary>
    TlsCertificate,

    /// <summary>The greeting an SSH server sends before anything is asked of it.</summary>
    SshBanner,

    /// <summary>An HTTP <c>Server</c> header or page title.</summary>
    Http
}

/// <summary>A name a service volunteered, and what volunteered it.</summary>
public sealed record ServiceIdentity(string Name, IdentitySource Source, int Port);

/// <summary>
///     Reads a host's name out of what its services say when you connect to them. This
///     is the identification half of <c>nmap -sV</c>, reduced to the three banners that
///     actually name homelab gear: a TLS certificate's CN, an SSH greeting, and an HTTP
///     <c>Server</c> header or page title. Pure — <see cref="INetworkProbe" /> does the
///     talking, everything here just reads what came back.
/// </summary>
public static class ServiceIdentityParser {
    /// <summary>
    ///     Names that identify software rather than a machine, or are placeholders the
    ///     installer never changed. Keeping them would label every appliance of a kind
    ///     with the same name, which is worse than no name at all.
    /// </summary>
    private static readonly HashSet<string> _uselessNames = new(StringComparer.OrdinalIgnoreCase) {
        "localhost",
        "localhost.localdomain",
        "example.com",
        "www.example.com",
        "default",
        "changeme",
        "server",
        "ubuntu",
        "debian",
        "raspberrypi",
        "openwrt",
        "*"
    };

    /// <summary>
    ///     The Common Name from a certificate's subject. Handles both the OpenSSL-style
    ///     "CN=host, O=org" and the .NET "CN=host, O=org" orderings, and tolerates the
    ///     slash-separated form some tools print.
    /// </summary>
    public static string? ParseTlsSubject(string? subject) {
        if (string.IsNullOrWhiteSpace(subject))
            return null;

        Match match = Regex.Match(
            subject,
            @"CN\s*=\s*(?<cn>[^,/]+)",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));

        if (!match.Success)
            return null;

        var cn = match.Groups["cn"].Value.Trim().Trim('"');

        // A wildcard certificate names a domain, not this machine.
        if (cn.StartsWith("*.", StringComparison.Ordinal))
            return null;

        return Clean(cn);
    }

    /// <summary>
    ///     The software an SSH server announces, e.g. "SSH-2.0-OpenSSH_9.6" -> "OpenSSH".
    ///     This names what the host runs rather than the host itself, which is still worth
    ///     having: "dropbear" says embedded appliance, "OpenSSH" says general-purpose box.
    /// </summary>
    public static string? ParseSshBanner(string? banner) {
        if (string.IsNullOrWhiteSpace(banner))
            return null;

        Match match = Regex.Match(
            banner,
            @"^SSH-\d+\.\d+-(?<software>[^\s\r\n]+)",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));

        if (!match.Success)
            return null;

        var software = match.Groups["software"].Value;

        // Trim the version: "OpenSSH_9.6p1" and "OpenSSH_8.4p1" are the same answer to
        // "what is this", and a version in a resource name goes stale on the next patch.
        var cut = software.IndexOfAny(['_', '-']);

        if (cut > 0)
            software = software[..cut];

        return Clean(software);
    }

    /// <summary>
    ///     A name from an HTTP response head: the page title if it says something, else
    ///     the <c>Server</c> header. Titles win because "Home Assistant" identifies a box
    ///     far better than "nginx" does.
    /// </summary>
    public static string? ParseHttpIdentity(string? responseHead) {
        if (string.IsNullOrWhiteSpace(responseHead))
            return null;

        // An error page's title describes the error, not the host — "HTTP Status 400 –
        // Bad Request" is a name no one would recognise. The Server header below is
        // still trustworthy on an error response, so only the title is gated.
        if (IsSuccessful(responseHead)) {
            Match title = Regex.Match(
                responseHead,
                @"<title[^>]*>(?<title>[^<]{1,120})</title>",
                RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(1));

            if (title.Success) {
                var cleaned = Clean(LeadingPhrase(title.Groups["title"].Value));

                if (cleaned != null)
                    return cleaned;
            }
        }

        Match server = Regex.Match(
            responseHead,
            @"^Server:\s*(?<server>[^\r\n]{1,80})",
            RegexOptions.IgnoreCase | RegexOptions.Multiline,
            TimeSpan.FromSeconds(1));

        if (!server.Success)
            return null;

        var value = server.Groups["server"].Value;

        // "nginx/1.24.0 (Ubuntu)" -> "nginx": the version is noise in a name.
        var slash = value.IndexOf('/');

        if (slash > 0)
            value = value[..slash];

        return Clean(value);
    }

    /// <summary>
    ///     The part of a page title before its first separator. Titles are written for
    ///     people and routinely carry a tagline or a page name after the product —
    ///     "Forgejo: Beyond coding. We Forge." names a machine far worse than "Forgejo"
    ///     does. The remainder is dropped only when what precedes it can stand alone.
    /// </summary>
    private static string LeadingPhrase(string title) {
        var cut = title.IndexOfAny([':', '|', '–', '—', '·', '»']);

        if (cut <= 0)
            return title;

        var lead = title[..cut].Trim();

        // "RackPeek" splits usefully; ": the homelab tool" does not, and a two-character
        // lead is more likely a stray colon than a product name.
        return lead.Length >= 3 ? lead : title;
    }

    /// <summary>
    ///     Whether the response's status line is a 2xx or 3xx. A head with no recognisable
    ///     status line is treated as unsuccessful: the title of something that is not
    ///     plainly a working page is not worth naming a machine after.
    /// </summary>
    private static bool IsSuccessful(string responseHead) {
        Match status = Regex.Match(
            responseHead,
            @"^HTTP/\d(?:\.\d)?\s+(?<code>\d{3})",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));

        return status.Success
               && int.TryParse(status.Groups["code"].Value, out var code)
               && code is >= 200 and < 400;
    }

    /// <summary>
    ///     Collapses whitespace, drops anything that is only punctuation or digits, and
    ///     rejects the placeholder names that would label half a rack identically.
    /// </summary>
    private static string? Clean(string? raw) {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var value = Regex.Replace(raw.Trim(), @"\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(1));

        if (value.Length is 0 or > 120)
            return null;

        // A CN that is a bare number (some appliances ship a serial as the CN) or pure
        // punctuation names nothing a person would recognise.
        if (!value.Any(char.IsLetter))
            return null;

        return _uselessNames.Contains(value) ? null : value;
    }
}
