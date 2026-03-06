namespace RackPeek.Domain.Git.UseCases;

public interface ICommitAllUseCase
{
    Task<string?> ExecuteAsync(string message);
}

public class CommitAllUseCase(IGitRepository repo) : ICommitAllUseCase
{
    public Task<string?> ExecuteAsync(string message)
    {
        if (!repo.IsAvailable)
            return Task.FromResult<string?>("Git is not available.");

        try
        {
            repo.StageAll();

            if (repo.GetStatus() != GitRepoStatus.Dirty)
                return Task.FromResult<string?>(null);

            repo.Commit(message);
            return Task.FromResult<string?>(null);
        }
        catch (Exception ex)
        {
            return Task.FromResult<string?>($"Commit failed: {ex.Message}");
        }
    }
}
