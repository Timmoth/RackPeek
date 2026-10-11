using System.Text.Json;

namespace RackPeek.Domain.Discovery;

/// <summary>A published port binding on the host side.</summary>
public sealed record DockerPortBinding(int HostPort, string Protocol, string HostIp = "") {
    /// <summary>
    ///     Bound to a loopback address, so only the host itself can reach it. Docker
    ///     writes the address as given (<c>127.0.0.1</c> mostly, but any 127.x works).
    /// </summary>
    public bool IsLoopback =>
        HostIp.StartsWith("127.", StringComparison.Ordinal) || HostIp == "::1";

    /// <summary>Bound to every interface rather than one address.</summary>
    public bool IsWildcard => HostIp is "" or "0.0.0.0" or "::";
}

/// <summary>
///     A container, reduced to the parts RackPeek models. Everything here comes from a
///     single <c>GET /containers/json</c> call — the per-container inspect adds nothing
///     a Service resource can hold.
/// </summary>
public sealed record DockerContainer {
    public required string Name { get; init; }
    public required string Image { get; init; }
    public string? ComposeProject { get; init; }
    public IReadOnlyList<DockerPortBinding> PublishedPorts { get; init; } = [];
}

/// <summary>Parses the Docker Engine list response. Pure, so it tests from a fixture.</summary>
public static class DockerContainerParser {
    private const string _composeProjectLabel = "com.docker.compose.project";

    public static List<DockerContainer> Parse(string json) {
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
            return [];

        return document.RootElement.EnumerateArray()
            .Select(ParseContainer)
            .OfType<DockerContainer>()
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static DockerContainer? ParseContainer(JsonElement element) {
        var name = ParseName(element);

        if (string.IsNullOrWhiteSpace(name))
            return null;

        return new DockerContainer {
            Name = name,
            Image = GetString(element, "Image") ?? "unknown",
            ComposeProject = GetLabel(element, _composeProjectLabel),
            PublishedPorts = ParsePorts(element)
        };
    }

    private static string? ParseName(JsonElement element) {
        if (!element.TryGetProperty("Names", out JsonElement names) || names.ValueKind != JsonValueKind.Array)
            return GetString(element, "Name")?.TrimStart('/');

        return names.EnumerateArray()
            .Select(n => n.GetString()?.TrimStart('/'))
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
    }

    private static List<DockerPortBinding> ParsePorts(JsonElement element) {
        if (!element.TryGetProperty("Ports", out JsonElement ports) || ports.ValueKind != JsonValueKind.Array)
            return [];

        // Dual-stack publishes show up twice, once per family ("0.0.0.0" and "::");
        // folding wildcards to one spelling keeps that one binding, not two.
        return ports.EnumerateArray()
            .Where(p => p.TryGetProperty("PublicPort", out JsonElement port) && port.ValueKind == JsonValueKind.Number)
            .Select(p => new DockerPortBinding(
                p.GetProperty("PublicPort").GetInt32(),
                (GetString(p, "Type") ?? "tcp").ToUpperInvariant(),
                GetString(p, "IP") ?? ""))
            .Select(p => p.IsWildcard ? p with { HostIp = "" } : p)
            .DistinctBy(p => (p.HostPort, p.Protocol, p.HostIp))
            .OrderBy(p => p.HostPort)
            .ToList();
    }

    private static string? GetLabel(JsonElement element, string label) {
        if (!element.TryGetProperty("Labels", out JsonElement labels) || labels.ValueKind != JsonValueKind.Object)
            return null;

        return labels.TryGetProperty(label, out JsonElement value) ? value.GetString() : null;
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
