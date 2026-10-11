using System.Text;
using RackPeek.Domain.Helpers;

namespace RackPeek.Domain.Discovery;

/// <summary>Turns machine-supplied strings into names a human would have typed.</summary>
public static class DiscoveryNaming {
    /// <summary>
    ///     The limit RackPeek's own validation enforces. The import API does not check
    ///     it, so a longer name would be accepted and then be un-editable from the CLI.
    /// </summary>
    public const int MaxNameLength = ThrowIfInvalid.MaxResourceNameLength;

    /// <summary>
    ///     Lowercase, alphanumeric and dashes only. Returns an empty string when the
    ///     input contains nothing usable, so callers can fall back to an id-derived name.
    /// </summary>
    public static string Slug(string? value) {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);

        foreach (var c in value.Trim().ToLowerInvariant())
            if (char.IsAsciiLetterOrDigit(c))
                builder.Append(c);
            else if ((c == '-' || c == '.' || c == '_' || char.IsWhiteSpace(c)) && builder.Length > 0 &&
                     builder[^1] != '-')
                builder.Append('-');

        return builder.ToString().Trim('-');
    }

    /// <summary>
    ///     The first label of a host name: <c>nas01.lan</c> becomes <c>nas01</c>, which is
    ///     what people call the machine.
    /// </summary>
    public static string HostLabel(string? hostname) {
        if (string.IsNullOrWhiteSpace(hostname))
            return string.Empty;

        var dot = hostname.IndexOf('.');

        return dot > 0 ? hostname[..dot] : hostname;
    }

    /// <summary>
    ///     The name a collector proposes: the machine's own name where it has a usable
    ///     one, otherwise derived from the id so it is still deterministic and unique.
    /// </summary>
    public static string Suggest(string? preferred, string kind, string discoveryId) {
        var slug = Slug(preferred);

        return slug.Length > 0
            ? Truncate(slug)
            : $"{Slug(kind)}-{DiscoveryId.ShortSuffix(discoveryId)}";
    }

    /// <summary>Cuts a name down to the allowed length without leaving a trailing dash.</summary>
    public static string Truncate(string name) =>
        name.Length <= MaxNameLength ? name : name[..MaxNameLength].TrimEnd('-');

    /// <summary>
    ///     Appends a disambiguating suffix, shortening the name to make room. Compose
    ///     puts the replica index at the end of a container name, so two long names
    ///     often differ only in the part truncation removes — the suffix is what keeps
    ///     them apart.
    /// </summary>
    public static string WithSuffix(string name, string suffix) {
        var room = MaxNameLength - suffix.Length - 1;
        var head = name.Length <= room ? name : name[..Math.Max(1, room)];

        return $"{head.TrimEnd('-')}-{suffix}";
    }

    /// <summary>
    ///     Returns a name not already in <paramref name="taken" />, adding it to the set.
    ///     Guards the case where two resources in one payload truncate to the same name,
    ///     which the import rejects outright as a duplicate.
    /// </summary>
    public static string Unique(string candidate, string discoveryId, ISet<string> taken) {
        var name = taken.Add(candidate)
            ? candidate
            : WithSuffix(candidate, DiscoveryId.ShortSuffix(discoveryId));

        taken.Add(name);

        return name;
    }
}
