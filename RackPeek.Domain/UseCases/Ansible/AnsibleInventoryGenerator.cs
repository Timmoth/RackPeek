using System.Text;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases.Ansible;

public sealed record InventoryOptions
{
    /// <summary>
    /// If set, create groups based on these tags.
    /// Example: ["env", "site"] -> [env], [site]
    /// </summary>
    public IReadOnlyList<string> GroupByTags { get; init; } = [];

    /// <summary>
    /// If set, create groups based on these label keys.
    /// Example: ["env", "site"] -> [env_prod], [site_london]
    /// </summary>
    public IReadOnlyList<string> GroupByLabelKeys { get; init; } = [];

    /// <summary>
    /// If set, emitted under [all:vars].
    /// </summary>
    public IDictionary<string, string> GlobalVars { get; init; } = new Dictionary<string, string>();
}

public sealed record InventoryResult(string InventoryText, IReadOnlyList<string> Warnings);

public static class AnsibleInventoryGenerator
{
    /// <summary>
    /// Generate an Ansible inventory in INI format from RackPeek resources.
    /// </summary>
    public static InventoryResult ToAnsibleInventoryIni(
        this IReadOnlyList<Resource> resources,
        InventoryOptions? options = null)
    {
        options ??= new InventoryOptions();

        var warnings = new List<string>();
        var hosts = new List<HostEntry>();

        // Build host entries (only resources that look addressable)
        foreach (var r in resources)
        {
            var address = GetAddress(r);

            if (string.IsNullOrWhiteSpace(address))
            {
                continue;
            }

            var hostVars = BuildHostVars(r, address);
            hosts.Add(new HostEntry(r, hostVars));
        }

        // Groups: kind + tags + label-based
        var groupToHosts = new Dictionary<string, List<HostEntry>>(StringComparer.OrdinalIgnoreCase);

        void AddToGroup(string groupName, HostEntry h)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return;
            groupName = SanitizeGroup(groupName);

            if (!groupToHosts.TryGetValue(groupName, out var list))
                groupToHosts[groupName] = list = new List<HostEntry>();

            // avoid duplicates if multiple rules add the same host
            if (!list.Any(x => string.Equals(x.Resource.Name, h.Resource.Name, StringComparison.OrdinalIgnoreCase)))
                list.Add(h);
        }

        foreach (var h in hosts)
        {
            // Tag groups
            var tags = options.GroupByTags.Intersect(h.Resource.Tags).ToArray();
            foreach (var tag in tags)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                AddToGroup(tag, h);
            }

            // Label-based groups: e.g. env=prod -> [env_prod]
            foreach (var key in options.GroupByLabelKeys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;

                if (h.Resource.Labels.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                {
                    AddToGroup($"{key}_{val}", h);
                }
            }
        }

        // Build output
        var sb = new StringBuilder();

        // [all:vars]
        if (options.GlobalVars.Count > 0)
        {
            sb.AppendLine("[all:vars]");
            foreach (var kvp in options.GlobalVars.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"{kvp.Key}={EscapeIniValue(kvp.Value)}");
            sb.AppendLine();
        }

        // Emit groups sorted, hosts sorted
        foreach (var group in groupToHosts.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine($"[{group}]");

            foreach (var h in groupToHosts[group].OrderBy(x => x.Resource.Name, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append(h.Resource.Name);

                // host vars (inline)
                foreach (var kvp in h.HostVars.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                    sb.Append($" {kvp.Key}={EscapeIniValue(kvp.Value)}");

                sb.AppendLine();
            }

            sb.AppendLine();
        }

        return new InventoryResult(sb.ToString().TrimEnd(), warnings);
    }

    // ---------- helpers ----------

    private sealed record HostEntry(Resource Resource, Dictionary<string, string> HostVars);

    private static string? GetAddress(Resource r)
    {
        // Preferred: ansible_host, else ip, else hostname
        if (r.Labels.TryGetValue("ansible_host", out var ah) && !string.IsNullOrWhiteSpace(ah))
            return ah;

        if (r.Labels.TryGetValue("ip", out var ip) && !string.IsNullOrWhiteSpace(ip))
            return ip;

        if (r.Labels.TryGetValue("hostname", out var hn) && !string.IsNullOrWhiteSpace(hn))
            return hn;

        return null;
    }

    private static Dictionary<string, string> BuildHostVars(Resource r, string address)
    {
        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ansible_host"] = address
        };

        // Copy any labels prefixed with ansible_
        foreach (var (k, v) in r.Labels)
        {
            if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(v)) continue;

            if (k.StartsWith("ansible_", StringComparison.OrdinalIgnoreCase))
            {
                // don't overwrite ansible_host we already derived unless explicitly present
                if (string.Equals(k, "ansible_host", StringComparison.OrdinalIgnoreCase))
                    vars["ansible_host"] = v;
                else
                    vars[k] = v;
            }
        }

        return vars;
    }

    private static string SanitizeGroup(string s)
    {
        // Ansible group names: letters/digits/underscore
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
                sb.Append(ch);
            else if (ch == '-' || ch == '.' || ch == ' ')
                sb.Append('_');
            // drop everything else
        }

        var result = sb.ToString();
        return string.IsNullOrWhiteSpace(result) ? "group" : result;
    }

    private static string EscapeIniValue(string value)
    {
        // quote if it contains spaces or special chars
        if (string.IsNullOrEmpty(value)) return "\"\"";

        var needsQuotes = value.Any(ch => char.IsWhiteSpace(ch) || ch is '"' or '\'' or '=');
        if (!needsQuotes) return value;

        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}