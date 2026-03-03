using System.Text;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases.Hosts;

public sealed record HostsExportOptions {
    /// <summary>
    ///     Only include resources that have at least one of these tags.
    /// </summary>
    public IReadOnlyList<string> IncludeTags { get; init; } = [];

    /// <summary>
    ///     If true, append domain suffix (e.g. .home.local)
    /// </summary>
    public string? DomainSuffix { get; init; }

    /// <summary>
    ///     If true, include localhost defaults at top.
    /// </summary>
    public bool IncludeLocalhostDefaults { get; init; } = true;
}

public sealed record HostsExportResult(
    string HostsText,
    IReadOnlyList<string> Warnings);

public static class HostsFileGenerator {
    public static HostsExportResult ToHostsFile(
        this IReadOnlyList<Resource> resources,
        HostsExportOptions? options = null) {
        options ??= new HostsExportOptions();

        var sb = new StringBuilder();
        var warnings = new List<string>();

        if (options.IncludeLocalhostDefaults) {
            sb.AppendLine("127.0.0.1 localhost");
            sb.AppendLine("::1 localhost");
            sb.AppendLine();
        }

        foreach (Resource r in resources.OrderBy(x => x.Name)) {
            if (options.IncludeTags.Any()) {
                var tags = r.Tags ?? [];
                if (!options.IncludeTags.Any(t =>
                        tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
                    continue;
            }

            var address = GetAddress(r);
            if (string.IsNullOrWhiteSpace(address)) continue;

            var hostName = SanitizeHostName(r.Name);

            if (!string.IsNullOrWhiteSpace(options.DomainSuffix))
                hostName = $"{hostName}.{options.DomainSuffix.Trim('.')}";

            sb.AppendLine($"{address} {hostName}");
        }

        if (sb.Length == 0)
            warnings.Add("No host entries generated.");

        return new HostsExportResult(sb.ToString().TrimEnd(), warnings);
    }

    private static string? GetAddress(Resource r) {
        if (r.Labels.TryGetValue("ip", out var ip) && !string.IsNullOrWhiteSpace(ip))
            return ip;

        if (r.Labels.TryGetValue("hostname", out var hn) && !string.IsNullOrWhiteSpace(hn))
            return hn;

        if (r.Labels.TryGetValue("ansible_host", out var ah) && !string.IsNullOrWhiteSpace(ah))
            return ah;

        return null;
    }

    private static string SanitizeHostName(string name) {
        var sb = new StringBuilder();

        foreach (var ch in name.Trim().ToLowerInvariant())
            if (char.IsLetterOrDigit(ch) || ch == '-')
                sb.Append(ch);
            else if (ch == '_' || ch == ' ')
                sb.Append('-');

        return sb.Length == 0 ? "host" : sb.ToString();
    }
}

public class HostsFileExportUseCase(IResourceCollection repository) : IUseCase {
    public async Task<HostsExportResult?> ExecuteAsync(HostsExportOptions options) {
        IReadOnlyList<Resource> resources = await repository.GetAllOfTypeAsync<Resource>();
        return resources.ToHostsFile(options);
    }
}
