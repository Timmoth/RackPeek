namespace RackPeek.Domain.Git;

public interface IGitRepository
{
    bool IsAvailable { get; }
    void Init();
    GitRepoStatus GetStatus();
    void StageAll();
    void Commit(string message);
    string GetDiff();
    string[] GetChangedFiles();
    void RestoreAll();
    string GetCurrentBranch();
    GitLogEntry[] GetLog(int count);
    bool HasRemote();
    GitSyncStatus FetchAndGetSyncStatus();
    void Push();
    void Pull();
    void AddRemote(string name, string url);
}
