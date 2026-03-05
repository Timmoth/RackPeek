namespace RackPeek.Domain.UseCases.Mermaid;

public sealed record MermaidExportOptions {
    /// <summary>
    /// Only include resources with these tags (optional)
    /// </summary>
    public IReadOnlyList<string> IncludeTags { get; init; } = [];

    /// <summary>
    /// Diagram type: "flowchart", "sequence", "class", "er", etc.
    /// Default: flowchart TD
    /// </summary>
    public string DiagramType { get; init; } = "flowchart TD";

    /// <summary>
    /// Whether to include resource labels as annotations
    /// </summary>
    public bool IncludeLabels { get; init; } = true;

    /// <summary>
    /// Whether to include relationships (edges)
    /// </summary>
    public bool IncludeEdges { get; init; } = true;

    /// <summary>
    /// Optional label keys to include (null = include all)
    /// </summary>
    public IReadOnlyList<string>? LabelWhitelist { get; init; }
}

public sealed record MermaidExportResult(
    string DiagramText,
    IReadOnlyList<string> Warnings);
