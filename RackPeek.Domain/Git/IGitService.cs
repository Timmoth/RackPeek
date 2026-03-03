namespace RackPeek.Domain.Git;

public enum GitRepoStatus
{
    NotAvailable,
    Clean,
    Dirty
}

public interface IGitService
{
    bool IsAvailable { get; }
    Task<GitRepoStatus> GetStatusAsync();
    Task<string?> CommitAllAsync(string message);
    Task<string[]> GetChangedFilesAsync();
    Task<string> GetDiffAsync();
    Task<string?> RestoreAllAsync();
    Task<string> GetCurrentBranchAsync();
    Task<GitLogEntry[]> GetLogAsync(int count = 20);
}

public record GitLogEntry(string Hash, string Message, string Author, string Date);
