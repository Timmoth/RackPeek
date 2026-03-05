using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RackPeek.Domain.Resources;

namespace RackPeek.Domain.UseCases.Mermaid
{
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

            // Group resources by Kind
            IOrderedEnumerable<IGrouping<string, Resource>> grouped = resources
                .Where(r => resolvedOptions.IncludeTags.Count == 0
                            || (r.Tags != null && r.Tags.Any(t => resolvedOptions.IncludeTags.Contains(t, StringComparer.OrdinalIgnoreCase))))
                .GroupBy(r => r.Kind)
                .OrderBy(g => g.Key);

            foreach (IGrouping<string, Resource> group in grouped)
            {
                sb.AppendLine($"  subgraph {SanitizeId(group.Key)}");
                foreach (Resource r in group.OrderBy(x => x.Name))
                {
                    var nodeId = SanitizeId(r.Name);
                    var label = BuildNodeLabel(r, resolvedOptions);
                    sb.AppendLine($"    {nodeId}[\"{label}\"]");
                }
                sb.AppendLine("  end");
            }

            // Map RunsOn relationships if requested
            if (resolvedOptions.IncludeEdges)
            {
                foreach (Resource r in resources)
                {
                    var nodeId = SanitizeId(r.Name);
                    foreach (var depName in r.RunsOn ?? new List<string>())
                    {
                        var depId = SanitizeId(depName);
                        sb.AppendLine($"  {nodeId} --> {depId}");
                    }
                }
            }

            if (sb.Length == 0)
                warnings.Add("No Mermaid diagram entries generated.");

            return new MermaidExportResult(sb.ToString().TrimEnd(), warnings);
        }

        private static string BuildNodeLabel(Resource r, MermaidExportOptions options)
        {
            if (!options.IncludeLabels || r.Labels.Count == 0)
                return r.Name;

            IEnumerable<KeyValuePair<string, string>> filtered = options.LabelWhitelist is null
                ? r.Labels
                : r.Labels.Where(kvp => options.LabelWhitelist.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase));

            var labelParts = filtered.Select(kvp => $"{kvp.Key}: {kvp.Value}").ToList();
            return labelParts.Count == 0 ? r.Name : $"{r.Name}\\n{string.Join("\\n", labelParts)}";
        }

        private static string SanitizeId(string name)
        {
            var sb = new StringBuilder();
            foreach (var ch in name.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                    sb.Append(ch);
                else if (ch == '-' || ch == '.' || ch == ' ')
                    sb.Append('_');
            }
            return sb.Length == 0 ? "node" : sb.ToString();
        }
    }
}
