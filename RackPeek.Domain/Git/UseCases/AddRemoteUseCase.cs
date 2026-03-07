namespace RackPeek.Domain.Git.UseCases;

public class AddRemoteUseCase(IGitRepository repo) : IUseCase
{
    public Task<string?> ExecuteAsync(string url)
    {
        if (!repo.IsAvailable)
            return Task.FromResult<string?>("Git is not available.");

        if (string.IsNullOrWhiteSpace(url))
            return Task.FromResult<string?>("URL is required.");

        url = url.Trim();

        if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<string?>("Only HTTPS URLs are supported.");

        if (repo.HasRemote())
            return Task.FromResult<string?>("Remote already configured.");

        try
        {
            repo.AddRemote("origin", url);

            // fetch remote state
            GitSyncStatus sync = repo.FetchAndGetSyncStatus();

            // if remote already has commits, bring them locally
            if (sync.Behind > 0)
            {
                repo.Pull();
            }

            return Task.FromResult<string?>(null);
        }
        catch (Exception ex)
        {
            return Task.FromResult<string?>($"Add remote failed: {ex.Message}");
        }
    }
}
