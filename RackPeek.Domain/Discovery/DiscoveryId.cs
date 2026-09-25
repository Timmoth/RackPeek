using System.Security.Cryptography;
using System.Text;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     Deterministic, globally unique identity for a discovered resource.
///     Format: <c>rpk1:{scheme}:{16 hex chars}</c>
///     The hash keeps low-value identifiers (machine-id, MAC) out of a config file
///     that is frequently committed to a public repository. Note this is
///     obfuscation rather than secrecy: the salt is public, so a low entropy seed
///     such as a MAC address remains recoverable by brute force.
/// </summary>
public static class DiscoveryId {
    public const string Prefix = "rpk1";
    public const string SystemScheme = "sys";
    public const string DockerScheme = "docker";

    public static string Create(string scheme, string seed) {
        if (string.IsNullOrWhiteSpace(scheme))
            throw new ArgumentException("Scheme is required.", nameof(scheme));

        if (string.IsNullOrWhiteSpace(seed))
            throw new ArgumentException("Seed is required.", nameof(seed));

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{Prefix}:{scheme}:{seed}"));

        return $"{Prefix}:{scheme}:{Convert.ToHexString(hash, 0, 8).ToLowerInvariant()}";
    }

    /// <summary>Short, stable fragment used to disambiguate generated names.</summary>
    public static string ShortSuffix(string discoveryId) {
        var lastColon = discoveryId.LastIndexOf(':');
        var hash = lastColon >= 0 ? discoveryId[(lastColon + 1)..] : discoveryId;

        return hash.Length <= 8 ? hash : hash[..8];
    }
}
