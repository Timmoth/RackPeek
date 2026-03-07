namespace RackPeek.Domain.Git.UseCases;

public class RestoreAllUseCase(IGitRepository repo) : IUseCase {
    public Task<string?> ExecuteAsync() {
        if (!repo.IsAvailable)
            return Task.FromResult<string?>("Git is not available.");

        try {
            repo.RestoreAll();
            return Task.FromResult<string?>(null);
        }
        catch (Exception ex) {
            return Task.FromResult<string?>($"Restore failed: {ex.Message}");
        }
    }
}
