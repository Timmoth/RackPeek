namespace RackPeek.Domain.Git.UseCases;

public class AddRemoteUseCase(IGitRepository repo) : IUseCase {
    public Task<string?> ExecuteAsync(string url) {
        if (!repo.IsAvailable)
            return Task.FromResult<string?>("Git is not available.");

        if (string.IsNullOrWhiteSpace(url))
            return Task.FromResult<string?>("URL is required.");

        if (!url.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<string?>("Only HTTPS URLs are supported.");

        if (repo.HasRemote())
            return Task.FromResult<string?>("Remote already configured.");

        try {
            repo.AddRemote("origin", url.Trim());
            return Task.FromResult<string?>(null);
        }
        catch (Exception ex) {
            return Task.FromResult<string?>($"Add remote failed: {ex.Message}");
        }
    }
}
