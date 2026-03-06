namespace RackPeek.Domain.Git.UseCases;

public interface IRestoreAllUseCase
{
    Task<string?> ExecuteAsync();
}

public class RestoreAllUseCase(IGitRepository repo) : IRestoreAllUseCase
{
    public Task<string?> ExecuteAsync()
    {
        if (!repo.IsAvailable)
            return Task.FromResult<string?>("Git is not available.");

        try
        {
            repo.RestoreAll();
            return Task.FromResult<string?>(null);
        }
        catch (Exception ex)
        {
            return Task.FromResult<string?>($"Restore failed: {ex.Message}");
        }
    }
}
