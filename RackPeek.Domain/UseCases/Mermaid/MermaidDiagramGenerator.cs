using System.Text;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases.Mermaid;

public static class MermaidDiagramGenerator
{
    public static MermaidExportResult ToMermaidDiagram(
        this IReadOnlyList<Resource> resources,
        MermaidExportOptions? options = null)
    {
        MermaidExportOptions resolvedOptions = options ?? new MermaidExportOptions();

        var sb = new StringBuilder();
        var warnings = new List<string>();

        sb.AppendLine(resolvedOptions.DiagramType);

        foreach (Resource r in resources.OrderBy(x => x.Name))
        {
            if (resolvedOptions.IncludeTags.Any())
            {
                var tags = r.Tags ?? Array.Empty<string>();

                var match = resolvedOptions
                    .IncludeTags
                    .Any(t => tags.Contains(t, StringComparer.OrdinalIgnoreCase));

                if (!match)
                    continue;
            }

            var nodeId = SanitizeId(r.Name);
            var label = BuildNodeLabel(r, resolvedOptions);

            sb.AppendLine($"    {nodeId}[\"{label}\"]");
        }

        if (sb.Length == 0)
            warnings.Add("No Mermaid diagram entries generated.");

        return new MermaidExportResult(sb.ToString().TrimEnd(), warnings);
    }

    private static string BuildNodeLabel(Resource r, MermaidExportOptions options)
    {
        if (!options.IncludeLabels || r.Labels.Count == 0)
            return r.Name;

        IEnumerable<KeyValuePair<string, string>> filtered;

        if (options.LabelWhitelist is null)
        {
            filtered = r.Labels;
        }
        else
        {
            filtered = r.Labels.Where(kvp =>
                options.LabelWhitelist.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase));
        }

        var labelParts = filtered
            .Select(kvp => $"{kvp.Key}: {kvp.Value}")
            .ToList();

        if (labelParts.Count == 0)
            return r.Name;

        return $"{r.Name}\\n{string.Join("\\n", labelParts)}";
    }

    private static string SanitizeId(string name)
    {
        var sb = new StringBuilder();

        foreach (var ch in name.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                sb.Append(ch);
            }
            else if (ch == '-' || ch == '.' || ch == ' ')
            {
                sb.Append('_');
            }
        }

        return sb.Length == 0 ? "node" : sb.ToString();
    }
}
