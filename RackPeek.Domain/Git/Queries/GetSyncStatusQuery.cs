namespace RackPeek.Domain.Git.Queries;

public interface IGetSyncStatusQuery
{
    GitSyncStatus Execute();
}

public class GetSyncStatusQuery(IGitRepository repo) : IGetSyncStatusQuery
{
    public GitSyncStatus Execute()
    {
        if (!repo.IsAvailable || !repo.HasRemote())
            return new GitSyncStatus(0, 0, false);

        try
        {
            return repo.FetchAndGetSyncStatus();
        }
        catch (Exception ex)
        {
            return new GitSyncStatus(0, 0, true, $"Fetch failed: {ex.Message}");
        }
    }
}
