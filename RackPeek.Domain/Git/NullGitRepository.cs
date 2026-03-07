namespace RackPeek.Domain.Git;

public sealed class NullGitRepository : IGitRepository {
    public bool IsAvailable => false;
    public void Init() { }
    public GitRepoStatus GetStatus() => GitRepoStatus.NotAvailable;
    public void StageAll() { }
    public void Commit(string message) { }
    public string GetDiff() => string.Empty;
    public string[] GetChangedFiles() => [];
    public void RestoreAll() { }
    public string GetCurrentBranch() => string.Empty;
    public GitLogEntry[] GetLog(int count) => [];
    public bool HasRemote() => false;
    public GitSyncStatus FetchAndGetSyncStatus() => new(0, 0, false);
    public void Push() { }
    public void Pull() { }
    public void AddRemote(string name, string url) { }
}
