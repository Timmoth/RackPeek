using RackPeek.Domain.Resources.SubResources;

namespace RackPeek.Domain.Helpers;

public static class PortSummaries {
    public static string Describe(List<Port>? ports) {
        if (ports == null || ports.Count == 0)
            return "None";

        IEnumerable<string> groups = ports
            .GroupBy(p => p.Type ?? "Unknown")
            .Select(g => $"{g.Key}: {g.Sum(p => p.Count ?? 0)}");

        return string.Join(", ", groups);
    }
}
