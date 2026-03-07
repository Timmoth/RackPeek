using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace RackPeek.Domain.Git;

public interface IGitCredentialsProvider {
    CredentialsHandler GetHandler();
}
public sealed class GitHubTokenCredentialsProvider(string username, string token) : IGitCredentialsProvider {
    private readonly string _username = username ?? throw new ArgumentNullException(nameof(username));
    private readonly string _token = token ?? throw new ArgumentNullException(nameof(token));

    public CredentialsHandler GetHandler() {
        return (_, _, _) => new UsernamePasswordCredentials {
            Username = _username,
            Password = _token
        };
    }
}

public sealed class LibGit2GitRepository(
    string configDirectory,
    IGitCredentialsProvider credentialsProvider) : IGitRepository {
    private readonly CredentialsHandler _credentials = credentialsProvider.GetHandler();

    private bool _isAvailable = Repository.IsValid(configDirectory);

    public bool IsAvailable => _isAvailable;

    public void Init() {
        Repository.Init(configDirectory);
        _isAvailable = true;
    }

    private Repository OpenRepo() => new(configDirectory);

    private static Signature GetSignature(Repository repo) {
        var name = repo.Config.Get<string>("user.name")?.Value ?? "RackPeek";
        var email = repo.Config.Get<string>("user.email")?.Value ?? "rackpeek@local";

        return new Signature(name, email, DateTimeOffset.Now);
    }

    private static Remote GetRemote(Repository repo) => repo.Network.Remotes["origin"] ?? repo.Network.Remotes.First();

    public GitRepoStatus GetStatus() {
        if (!_isAvailable)
            return GitRepoStatus.NotAvailable;

        using Repository repo = OpenRepo();

        return repo.RetrieveStatus().IsDirty
            ? GitRepoStatus.Dirty
            : GitRepoStatus.Clean;
    }

    public void StageAll() {
        using Repository repo = OpenRepo();
        Commands.Stage(repo, "*");
    }

    public void Commit(string message) {
        using Repository repo = OpenRepo();

        Signature signature = GetSignature(repo);
        repo.Commit(message, signature, signature);
    }

    public string GetDiff() {
        using Repository repo = OpenRepo();

        Patch patch = repo.Diff.Compare<Patch>(
            repo.Head.Tip?.Tree,
            DiffTargets.Index | DiffTargets.WorkingDirectory);

        return patch?.Content ?? string.Empty;
    }

    public string[] GetChangedFiles() {
        using Repository repo = OpenRepo();

        return repo.RetrieveStatus()
            .Where(e => e.State != FileStatus.Ignored)
            .Select(e => $"{GetPrefix(e.State)}  {e.FilePath}")
            .ToArray();
    }

    private static string GetPrefix(FileStatus state) => state switch {
        FileStatus.NewInWorkdir or FileStatus.NewInIndex => "A",
        FileStatus.DeletedFromWorkdir or FileStatus.DeletedFromIndex => "D",
        FileStatus.RenamedInWorkdir or FileStatus.RenamedInIndex => "R",
        _ when state.HasFlag(FileStatus.ModifiedInWorkdir)
          || state.HasFlag(FileStatus.ModifiedInIndex) => "M",
        _ => "?"
    };

    public void RestoreAll() {
        using Repository repo = OpenRepo();

        repo.CheckoutPaths(
            repo.Head.FriendlyName,
            ["*"],
            new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });

        repo.RemoveUntrackedFiles();
    }

    public string GetCurrentBranch() {
        using Repository repo = OpenRepo();
        return repo.Head.FriendlyName;
    }

    public GitLogEntry[] GetLog(int count) {
        using Repository repo = OpenRepo();

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

    public bool HasRemote() {
        using Repository repo = OpenRepo();
        return repo.Network.Remotes.Any();
    }

    public GitSyncStatus FetchAndGetSyncStatus() {
        using Repository repo = OpenRepo();

        if (!repo.Network.Remotes.Any())
            return new(0, 0, false);

        Remote remote = GetRemote(repo);

        Commands.Fetch(
            repo,
            remote.Name,
            remote.FetchRefSpecs.Select(r => r.Specification),
            new FetchOptions { CredentialsProvider = _credentials },
            null);

        Branch? tracking = repo.Head.TrackedBranch;

        if (tracking is null)
            return new(repo.Commits.Count(), 0, true);

        HistoryDivergence divergence = repo.ObjectDatabase.CalculateHistoryDivergence(
            repo.Head.Tip,
            tracking.Tip);

        return new(
            divergence.AheadBy ?? 0,
            divergence.BehindBy ?? 0,
            true);
    }

    public void Push() {
        using Repository repo = OpenRepo();

        Remote remote = GetRemote(repo);
        var refSpec = $"refs/heads/{repo.Head.FriendlyName}";

        try {
            repo.Network.Push(
                remote,
                refSpec,
                new PushOptions { CredentialsProvider = _credentials });
        }
        catch (LibGit2Sharp.NonFastForwardException) {
            // remote has commits we don't have
            Pull();

            // retry push
            repo.Network.Push(
                remote,
                refSpec,
                new PushOptions { CredentialsProvider = _credentials });
        }

        if (repo.Head.TrackedBranch is null) {
            Branch remoteBranch = repo.Branches[$"{remote.Name}/{repo.Head.FriendlyName}"];

            if (remoteBranch != null) {
                repo.Branches.Update(repo.Head,
                    b => b.TrackedBranch = remoteBranch.CanonicalName);
            }
        }
    }

    public void Pull() {
        using Repository repo = OpenRepo();

        Commands.Pull(
            repo,
            GetSignature(repo),
            new PullOptions {
                FetchOptions = new FetchOptions {
                    CredentialsProvider = _credentials
                },
                MergeOptions = new MergeOptions {
                    FailOnConflict = false
                }
            });
    }

    public void AddRemote(string name, string url) {
        using Repository repo = OpenRepo();
        repo.Network.Remotes.Add(name, url);
    }

    private static string FormatRelativeDate(DateTimeOffset date) {
        TimeSpan diff = DateTimeOffset.Now - date;

        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} minutes ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hours ago";
        if (diff.TotalDays < 30) return $"{(int)diff.TotalDays} days ago";
        if (diff.TotalDays < 365) return $"{(int)(diff.TotalDays / 30)} months ago";

        return $"{(int)(diff.TotalDays / 365)} years ago";
    }
}
