using System.Text.Json;

namespace RackPeek.Domain.Discovery;

/// <summary>
///     The engine host's own account of itself, from <c>GET /info</c>. This is what makes
///     a remote engine discoverable without user intervention: the daemon id gives the
///     services an identity seed that does not depend on which machine ran the command,
///     and the name is the hostname <c>rpk discover system</c> would report on that box.
/// </summary>
public sealed record DockerEngineInfo {
    /// <summary>
    ///     The daemon's persisted unique id (kept under <c>/var/lib/docker</c>). Survives
    ///     reboots; changes only on an engine reinstall — the same failure mode
    ///     <c>/etc/machine-id</c> has for local discovery.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>The engine host's hostname.</summary>
    public string? Hostname { get; init; }
}

/// <summary>Parses the Docker Engine <c>GET /info</c> response. Pure, so it tests from a fixture.</summary>
public static class DockerEngineInfoParser {
    public static DockerEngineInfo? Parse(string json) {
        try {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            return new DockerEngineInfo {
                Id = GetString(document.RootElement, "ID"),
                Hostname = GetString(document.RootElement, "Name")
            };
        }
        catch (JsonException) {
            return null;
        }
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()
            : null;
}
