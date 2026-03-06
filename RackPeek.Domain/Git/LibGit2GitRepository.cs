using LibGit2Sharp;

namespace RackPeek.Domain.Git;

public sealed class LibGit2GitRepository : IGitRepository
{
    private readonly string _repoPath;
    private bool _isAvailable;

    public LibGit2GitRepository(string configDirectory)
    {
        _repoPath = configDirectory;
        _isAvailable = Repository.IsValid(configDirectory);
    }

    public bool IsAvailable => _isAvailable;

    public void Init()
    {
        Repository.Init(_repoPath);
        _isAvailable = true;
    }

    public GitRepoStatus GetStatus()
    {
        if (!_isAvailable)
            return GitRepoStatus.NotAvailable;

        using var repo = new Repository(_repoPath);
        return repo.RetrieveStatus().IsDirty ? GitRepoStatus.Dirty : GitRepoStatus.Clean;
    }

    public void StageAll()
    {
        using var repo = new Repository(_repoPath);
        Commands.Stage(repo, "*");
    }

    public void Commit(string message)
    {
        using var repo = new Repository(_repoPath);
        Signature signature = GetSignature(repo);
        repo.Commit(message, signature, signature);
    }

    public string GetDiff()
    {
        using var repo = new Repository(_repoPath);
        Patch changes = repo.Diff.Compare<Patch>(
            repo.Head.Tip?.Tree,
            DiffTargets.Index | DiffTargets.WorkingDirectory);
        return changes?.Content ?? string.Empty;
    }

    public string[] GetChangedFiles()
    {
        using var repo = new Repository(_repoPath);
        RepositoryStatus status = repo.RetrieveStatus();
        return status
            .Where(e => e.State != FileStatus.Ignored)
            .Select(e =>
            {
                var prefix = e.State switch
                {
                    FileStatus.NewInWorkdir or FileStatus.NewInIndex => "A",
                    FileStatus.DeletedFromWorkdir or FileStatus.DeletedFromIndex => "D",
                    FileStatus.RenamedInWorkdir or FileStatus.RenamedInIndex => "R",
                    _ when e.State.HasFlag(FileStatus.ModifiedInWorkdir)
                        || e.State.HasFlag(FileStatus.ModifiedInIndex) => "M",
                    _ => "?"
                };
                return $"{prefix}  {e.FilePath}";
            })
            .ToArray();
    }

    public void RestoreAll()
    {
        using var repo = new Repository(_repoPath);
        var options = new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force };
        repo.CheckoutPaths(repo.Head.FriendlyName, new[] { "*" }, options);
        repo.RemoveUntrackedFiles();
    }

    public string GetCurrentBranch()
    {
        using var repo = new Repository(_repoPath);
        return repo.Head.FriendlyName;
    }

    public GitLogEntry[] GetLog(int count)
    {
        using var repo = new Repository(_repoPath);
        if (repo.Head.Tip is null)
            return [];

        return repo.Commits
            .Take(count)
            .Select(c => new GitLogEntry(
                c.Sha[..7],
                c.MessageShort,
                c.Author.Name,
                FormatRelativeDate(c.Author.When)))
            .ToArray();
    }

    public bool HasRemote()
    {
        using var repo = new Repository(_repoPath);
        return repo.Network.Remotes.Any();
    }

    public GitSyncStatus FetchAndGetSyncStatus()
    {
        using var repo = new Repository(_repoPath);

        if (!repo.Network.Remotes.Any())
            return new GitSyncStatus(0, 0, false);

        Remote remote = repo.Network.Remotes["origin"]
            ?? repo.Network.Remotes.First();

        var fetchOptions = new FetchOptions();
        ConfigureCredentials(fetchOptions);
        IEnumerable<string> refSpecs = remote.FetchRefSpecs.Select(r => r.Specification);
        Commands.Fetch(repo, remote.Name, refSpecs, fetchOptions, null);

        Branch? tracking = repo.Head.TrackedBranch;
        if (tracking is null)
        {
            var localCount = repo.Head.Tip is not null
                ? repo.Commits.Count()
                : 0;
            return new GitSyncStatus(localCount, 0, true);
        }

        HistoryDivergence divergence = repo.ObjectDatabase.CalculateHistoryDivergence(
            repo.Head.Tip, tracking.Tip);

        var ahead = divergence.AheadBy ?? 0;
        var behind = divergence.BehindBy ?? 0;

        return new GitSyncStatus(ahead, behind, true);
    }

    public void Push()
    {
        using var repo = new Repository(_repoPath);

        Remote remote = repo.Network.Remotes["origin"]
            ?? repo.Network.Remotes.First();

        var pushOptions = new PushOptions();
        ConfigureCredentials(pushOptions);

        var pushRefSpec = $"refs/heads/{repo.Head.FriendlyName}";
        repo.Network.Push(remote, pushRefSpec, pushOptions);

        if (repo.Head.TrackedBranch is null)
        {
            Branch? remoteBranch = repo.Branches[$"{remote.Name}/{repo.Head.FriendlyName}"];
            if (remoteBranch is not null)
            {
                repo.Branches.Update(repo.Head,
                    b => b.TrackedBranch = remoteBranch.CanonicalName);
            }
        }
    }

    public void Pull()
    {
        using var repo = new Repository(_repoPath);

        var pullOptions = new PullOptions
        {
            FetchOptions = new FetchOptions()
        };
        ConfigureCredentials(pullOptions.FetchOptions);

        Signature signature = GetSignature(repo);
        Commands.Pull(repo, signature, pullOptions);
    }

    public void AddRemote(string name, string url)
    {
        using var repo = new Repository(_repoPath);
        repo.Network.Remotes.Add(name, url);
    }

    private static Signature GetSignature(Repository repo)
    {
        Configuration config = repo.Config;
        var name = config.Get<string>("user.name")?.Value ?? "RackPeek";
        var email = config.Get<string>("user.email")?.Value ?? "rackpeek@local";
        return new Signature(name, email, DateTimeOffset.Now);
    }

    private static void ConfigureCredentials(FetchOptions options)
    {
        var token = Environment.GetEnvironmentVariable("GIT_TOKEN");
        if (!string.IsNullOrEmpty(token))
        {
            options.CredentialsProvider = (_, _, _) =>
                new UsernamePasswordCredentials
                {
                    Username = "git",
                    Password = token
                };
        }
    }

    private static void ConfigureCredentials(PushOptions options)
    {
        var token = Environment.GetEnvironmentVariable("GIT_TOKEN");
        if (!string.IsNullOrEmpty(token))
        {
            options.CredentialsProvider = (_, _, _) =>
                new UsernamePasswordCredentials
                {
                    Username = "git",
                    Password = token
                };
        }
    }

    private static string FormatRelativeDate(DateTimeOffset date)
    {
        TimeSpan diff = DateTimeOffset.Now - date;
        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} minutes ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hours ago";
        if (diff.TotalDays < 30) return $"{(int)diff.TotalDays} days ago";
        if (diff.TotalDays < 365) return $"{(int)(diff.TotalDays / 30)} months ago";
        return $"{(int)(diff.TotalDays / 365)} years ago";
    }
}
