using System.Text;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases.Ansible;

public enum InventoryFormat {
    Ini,
    Yaml
}

public sealed record InventoryOptions {
    /// <summary>
    ///     Output format (default: INI)
    /// </summary>
    public InventoryFormat Format { get; init; } = InventoryFormat.Ini;

    /// <summary>
    ///     If set, create groups based on these tags.
    ///     Example: ["prod", "staging"] -> [prod], [staging]
    /// </summary>
    public IReadOnlyList<string> GroupByTags { get; init; } = [];

    /// <summary>
    ///     If set, create groups based on these label keys.
    ///     Example: ["env"] -> [env_prod]
    /// </summary>
    public IReadOnlyList<string> GroupByLabelKeys { get; init; } = [];

    /// <summary>
    ///     If set, emitted under [all:vars] (INI) or all.vars (YAML).
    /// </summary>
    public IDictionary<string, string> GlobalVars { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed record InventoryResult(string InventoryText, IReadOnlyList<string> Warnings);

public static class AnsibleInventoryGenerator {
    public static InventoryResult ToAnsibleInventory(
        this IReadOnlyList<Resource> resources,
        InventoryOptions? options = null) {
        options ??= new InventoryOptions();

        InventoryModel model = BuildInventoryModel(resources, options);

        return options.Format switch {
            InventoryFormat.Yaml => RenderYaml(model, options),
            _ => RenderIni(model, options)
        };
    }

    private static InventoryModel BuildInventoryModel(
        IReadOnlyList<Resource> resources,
        InventoryOptions options) {
        var warnings = new List<string>();
        var hosts = new List<HostEntry>();

        foreach (Resource r in resources) {
            var address = GetAddress(r);

            if (string.IsNullOrWhiteSpace(address))
                continue;

            Dictionary<string, string> vars = BuildHostVars(r, address);
            hosts.Add(new HostEntry(r.Name, vars, r));
        }

        var groupToHosts =
            new Dictionary<string, List<HostEntry>>(StringComparer.OrdinalIgnoreCase);

        void AddToGroup(string groupName, HostEntry h) {
            if (string.IsNullOrWhiteSpace(groupName))
                return;

            groupName = SanitizeGroup(groupName);

            if (!groupToHosts.TryGetValue(groupName, out List<HostEntry>? list))
                groupToHosts[groupName] = list = new List<HostEntry>();

            if (!list.Any(x => string.Equals(x.Name, h.Name, StringComparison.OrdinalIgnoreCase)))
                list.Add(h);
        }

        foreach (HostEntry h in hosts) {
            // Tag-based groups
            var matchingTags = options.GroupByTags
                .Intersect(h.Resource.Tags ?? [])
                .ToArray();

            foreach (var tag in matchingTags)
                AddToGroup(tag, h);

            // Label-based groups
            foreach (var key in options.GroupByLabelKeys)
                if (h.Resource.Labels.TryGetValue(key, out var val)
                    && !string.IsNullOrWhiteSpace(val))
                    AddToGroup($"{key}_{val}", h);
        }

        return new InventoryModel(groupToHosts, warnings);
    }

    private static InventoryResult RenderIni(
        InventoryModel model,
        InventoryOptions options) {
        var sb = new StringBuilder();

        if (options.GlobalVars.Count > 0) {
            sb.AppendLine("[all:vars]");

            foreach (KeyValuePair<string, string> kvp in options.GlobalVars
                         .OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"{kvp.Key}={EscapeIniValue(kvp.Value)}");

            sb.AppendLine();
        }

        foreach (var group in model.Groups.Keys
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) {
            sb.AppendLine($"[{group}]");

            foreach (HostEntry host in model.Groups[group]
                         .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)) {
                sb.Append(host.Name);

                foreach (KeyValuePair<string, string> kvp in host.Vars
                             .OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                    sb.Append($" {kvp.Key}={EscapeIniValue(kvp.Value)}");

                sb.AppendLine();
            }

            sb.AppendLine();
        }

        return new InventoryResult(sb.ToString().TrimEnd(), model.Warnings);
    }

    private static InventoryResult RenderYaml(
        InventoryModel model,
        InventoryOptions options) {
        var sb = new StringBuilder();

        sb.AppendLine("all:");

        if (options.GlobalVars.Count > 0) {
            sb.AppendLine("  vars:");
            foreach (KeyValuePair<string, string> kvp in options.GlobalVars
                         .OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"    {kvp.Key}: {kvp.Value}");
        }

        sb.AppendLine("  children:");

        foreach (var group in model.Groups.Keys
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)) {
            sb.AppendLine($"    {group}:");
            sb.AppendLine("      hosts:");

            foreach (HostEntry host in model.Groups[group]
                         .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)) {
                sb.AppendLine($"        {host.Name}:");

                foreach (KeyValuePair<string, string> kvp in host.Vars
                             .OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                    sb.AppendLine($"          {kvp.Key}: {kvp.Value}");
            }
        }

        return new InventoryResult(sb.ToString().TrimEnd(), model.Warnings);
    }

    private static string? GetAddress(Resource r) {
        if (r.Labels.TryGetValue("ansible_host", out var ah) && !string.IsNullOrWhiteSpace(ah))
            return ah;

        if (r.Labels.TryGetValue("ip", out var ip) && !string.IsNullOrWhiteSpace(ip))
            return ip;

        if (r.Labels.TryGetValue("hostname", out var hn) && !string.IsNullOrWhiteSpace(hn))
            return hn;

        return null;
    }

    private static Dictionary<string, string> BuildHostVars(Resource r, string address) {
        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["ansible_host"] = address
        };

        foreach ((var k, var v) in r.Labels) {
            if (string.IsNullOrWhiteSpace(k) || string.IsNullOrWhiteSpace(v))
                continue;

            if (k.StartsWith("ansible_", StringComparison.OrdinalIgnoreCase))
                vars[k] = v;
        }

        return vars;
    }

    private static string SanitizeGroup(string s) {
        var sb = new StringBuilder();

        foreach (var ch in s.Trim().ToLowerInvariant())
            if (char.IsLetterOrDigit(ch) || ch == '_')
                sb.Append(ch);
            else if (ch == '-' || ch == '.' || ch == ' ')
                sb.Append('_');

        var result = sb.ToString();
        return string.IsNullOrWhiteSpace(result) ? "group" : result;
    }

    private static string EscapeIniValue(string value) {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        var needsQuotes = value.Any(ch =>
            char.IsWhiteSpace(ch) || ch is '"' or '\'' or '=');

        if (!needsQuotes)
            return value;

        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private sealed record HostEntry(
        string Name,
        Dictionary<string, string> Vars,
        Resource Resource);

    private sealed record InventoryModel(
        Dictionary<string, List<HostEntry>> Groups,
        List<string> Warnings);
}
