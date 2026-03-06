namespace RackPeek.Domain.Git.UseCases;

public interface IInitRepoUseCase
{
    Task<string?> ExecuteAsync();
}

public class InitRepoUseCase(IGitRepository repo) : IInitRepoUseCase
{
    public Task<string?> ExecuteAsync()
    {
        if (repo.IsAvailable)
            return Task.FromResult<string?>(null);

        try
        {
            repo.Init();
            return Task.FromResult<string?>(null);
        }
        catch (Exception ex)
        {
            return Task.FromResult<string?>($"Init failed: {ex.Message}");
        }
    }
}
