using RackPeek.Domain;
using RackPeek.Domain.Git;

public class PushUseCase(IGitRepository repo) : IUseCase
{
    public Task<string?> ExecuteAsync()
    {
        if (!repo.IsAvailable)
            return Task.FromResult<string?>("Git is not available.");

        if (!repo.HasRemote())
            return Task.FromResult<string?>("No remote configured.");

        try
        {
            try
            {
                repo.Push();
            }
            catch
            {
                repo.Pull();
                repo.Push();
            }

            return Task.FromResult<string?>(null);
        }
        catch (Exception ex)
        {
            return Task.FromResult<string?>($"Push failed: {ex.Message}");
        }
    }
}
