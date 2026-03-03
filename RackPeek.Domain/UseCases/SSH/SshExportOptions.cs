namespace RackPeek.Domain.UseCases.Ssh;

public sealed record SshExportOptions {
    /// <summary>
    ///     Only include resources with this tag (optional)
    /// </summary>
    public IReadOnlyList<string> IncludeTags { get; init; } = [];

    /// <summary>
    ///     Optional default SSH user
    /// </summary>
    public string? DefaultUser { get; init; }

    /// <summary>
    ///     Optional default SSH port
    /// </summary>
    public int DefaultPort { get; init; } = 22;

    /// <summary>
    ///     Optional default identity file
    /// </summary>
    public string? DefaultIdentityFile { get; init; }

    /// <summary>
    ///     If true, use resource name as Host alias (default true)
    /// </summary>
    public bool UseResourceNameAsAlias { get; init; } = true;
}

public sealed record SshExportResult(
    string ConfigText,
    IReadOnlyList<string> Warnings);
