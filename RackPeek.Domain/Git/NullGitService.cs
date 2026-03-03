namespace RackPeek.Domain.Git;

public sealed class NullGitService : IGitService
{
    public bool IsAvailable => false;
    public Task<GitRepoStatus> GetStatusAsync() => Task.FromResult(GitRepoStatus.NotAvailable);
    public Task<string?> CommitAllAsync(string message) => Task.FromResult<string?>("Not available.");
    public Task<string[]> GetChangedFilesAsync() => Task.FromResult(Array.Empty<string>());
    public Task<string> GetDiffAsync() => Task.FromResult(string.Empty);
    public Task<string?> RestoreAllAsync() => Task.FromResult<string?>("Not available.");
    public Task<string> GetCurrentBranchAsync() => Task.FromResult(string.Empty);
    public Task<GitLogEntry[]> GetLogAsync(int count = 20) => Task.FromResult(Array.Empty<GitLogEntry>());
}
