namespace RackPeek.Domain.Git.UseCases;


public class PushUseCase(IGitRepository repo) : IUseCase {
    public Task<string?> ExecuteAsync() {
        if (!repo.IsAvailable)
            return Task.FromResult<string?>("Git is not available.");

        if (!repo.HasRemote())
            return Task.FromResult<string?>("No remote configured.");

        try {
            repo.Push();
            return Task.FromResult<string?>(null);
        }
        catch (Exception ex) {
            return Task.FromResult<string?>($"Push failed: {ex.Message}");
        }
    }
}
