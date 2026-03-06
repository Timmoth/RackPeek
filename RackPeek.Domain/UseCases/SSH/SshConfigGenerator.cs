using System.Text;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases.SSH;

public static class SshConfigGenerator {
    public static SshExportResult ToSshConfig(
        this IReadOnlyList<Resource> resources,
        SshExportOptions? options = null) {
        options ??= new SshExportOptions();

        var sb = new StringBuilder();
        var warnings = new List<string>();

        foreach (Resource r in resources.OrderBy(x => x.Name)) {
            if (options.IncludeTags.Any()) {
                var tags = r.Tags ?? [];
                if (!options.IncludeTags.Any(t =>
                        tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
                    continue;
            }

            var address = GetAddress(r);
            if (string.IsNullOrWhiteSpace(address)) continue;

            var alias = options.UseResourceNameAsAlias
                ? SanitizeAlias(r.Name)
                : address;

            var user = GetLabel(r, "ssh_user")
                       ?? GetLabel(r, "ansible_user")
                       ?? options.DefaultUser;

            var port = GetPort(r) ?? options.DefaultPort;

            var identity = GetLabel(r, "ssh_identity_file")
                           ?? options.DefaultIdentityFile;

            sb.AppendLine($"Host {alias}");
            sb.AppendLine($"  HostName {address}");

            if (!string.IsNullOrWhiteSpace(user))
                sb.AppendLine($"  User {user}");

            if (port != 22)
                sb.AppendLine($"  Port {port}");

            if (!string.IsNullOrWhiteSpace(identity))
                sb.AppendLine($"  IdentityFile {identity}");

            sb.AppendLine();
        }

        if (sb.Length == 0)
            warnings.Add("No SSH entries generated.");

        return new SshExportResult(sb.ToString().TrimEnd(), warnings);
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

    private static string? GetLabel(Resource r, string key) {
        if (r.Labels.TryGetValue(key, out var val) &&
            !string.IsNullOrWhiteSpace(val))
            return val;

        return null;
    }

    private static int? GetPort(Resource r) {
        if (r.Labels.TryGetValue("ssh_port", out var portStr) &&
            int.TryParse(portStr, out var port))
            return port;

        if (r.Labels.TryGetValue("ansible_port", out var ansiblePort) &&
            int.TryParse(ansiblePort, out var aPort))
            return aPort;

        return null;
    }

    private static string SanitizeAlias(string name) {
        var sb = new StringBuilder();

        foreach (var ch in name.Trim().ToLowerInvariant())
            if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-')
                sb.Append(ch);
            else if (ch == '.' || ch == ' ')
                sb.Append('-');

        return sb.Length == 0 ? "host" : sb.ToString();
    }
}
