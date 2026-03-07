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

    private static Remote GetRemote(Repository repo)
        => repo.Network.Remotes["origin"] ?? repo.Network.Remotes.First();

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

        var files = repo.RetrieveStatus()
            .Where(e => e.State != FileStatus.Ignored)
            .Select(e => e.FilePath)
            .ToList();

        if (files.Count == 0)
            return;

        Commands.Stage(repo, files);
    }

    public void Commit(string message) {
        using Repository repo = OpenRepo();

        Signature signature = GetSignature(repo);
        repo.Commit(message, signature, signature);
    }

    public string GetDiff() {
        using Repository repo = OpenRepo();

        Tree? tree = repo.Head.Tip?.Tree;

        Patch patch = repo.Diff.Compare<Patch>(
            tree,
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
            return new GitSyncStatus(0, 0, false);

        Remote remote = GetRemote(repo);

        Commands.Fetch(
            repo,
            remote.Name,
            remote.FetchRefSpecs.Select(r => r.Specification),
            new FetchOptions { CredentialsProvider = _credentials },
            null);

        // If the repo has no commits yet (unborn branch)
        if (repo.Head.Tip == null)
            return new GitSyncStatus(0, 0, true);

        Branch? remoteBranch = repo.Branches[$"{remote.Name}/{repo.Head.FriendlyName}"];

        if (remoteBranch?.Tip == null)
            return new GitSyncStatus(repo.Commits.Count(), 0, true);

        HistoryDivergence? divergence = repo.ObjectDatabase.CalculateHistoryDivergence(
            repo.Head.Tip,
            remoteBranch.Tip);

        return new GitSyncStatus(
            divergence.AheadBy ?? 0,
            divergence.BehindBy ?? 0,
            true);
    }
    public void Push() {
        using Repository repo = OpenRepo();

        Remote remote = GetRemote(repo);
        var branch = repo.Head.FriendlyName;
        var refSpec = $"refs/heads/{branch}:refs/heads/{branch}";

        try {
            repo.Network.Push(
                remote,
                refSpec,
                new PushOptions { CredentialsProvider = _credentials });
        }
        catch (NonFastForwardException) {
            PullInternal(repo);

            repo.Network.Push(
                remote,
                refSpec,
                new PushOptions { CredentialsProvider = _credentials });
        }

        if (repo.Head.TrackedBranch is null) {
            repo.Branches.Update(repo.Head,
                b => b.TrackedBranch = $"refs/remotes/{remote.Name}/{branch}");
        }
    }

    public void Pull() {
        using Repository repo = OpenRepo();
        PullInternal(repo);
    }

    private void PullInternal(Repository repo) {
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

        if (repo.Network.Remotes[name] != null)
            return;

        repo.Network.Remotes.Add(name, url);

        Remote remote = repo.Network.Remotes[name];

        // fetch remote state
        Commands.Fetch(
            repo,
            remote.Name,
            remote.FetchRefSpecs.Select(r => r.Specification),
            new FetchOptions { CredentialsProvider = _credentials },
            null);

        // detect if remote has a default branch
        Branch? remoteMain =
            repo.Branches[$"{remote.Name}/main"] ??
            repo.Branches[$"{remote.Name}/master"];

        var hasLocalFiles =
            repo.RetrieveStatus()
                .Any(e => e.State != FileStatus.Ignored);

        // CASE 1: remote repo already has commits
        if (remoteMain != null && remoteMain.Tip != null) {
            Branch local = repo.CreateBranch(remoteMain.FriendlyName, remoteMain.Tip);
            Commands.Checkout(repo, local);

            repo.Branches.Update(local,
                b => b.TrackedBranch = remoteMain.CanonicalName);

            if (hasLocalFiles) {
                // import existing config to a new branch
                var importBranchName = $"rackpeek-{DateTime.UtcNow:yyyyMMddHHmmss}";

                Branch importBranch = repo.CreateBranch(importBranchName);
                Commands.Checkout(repo, importBranch);

                Commands.Stage(repo, "*");

                Signature sig = GetSignature(repo);

                repo.Commit(
                    "rackpeek: import existing config",
                    sig,
                    sig);

                repo.Network.Push(
                    remote,
                    $"refs/heads/{importBranchName}:refs/heads/{importBranchName}",
                    new PushOptions { CredentialsProvider = _credentials });

                repo.Branches.Update(importBranch,
                    b => b.TrackedBranch = $"refs/remotes/{remote.Name}/{importBranchName}");
            }

            return;
        }

        // CASE 2: remote repo is empty
        if (hasLocalFiles) {
            var branchName = "main";

            Branch branch = repo.CreateBranch(branchName);
            Commands.Checkout(repo, branch);

            Commands.Stage(repo, "*");

            Signature sig = GetSignature(repo);

            repo.Commit(
                "rackpeek: initial config",
                sig,
                sig);

            repo.Network.Push(
                remote,
                $"refs/heads/{branchName}:refs/heads/{branchName}",
                new PushOptions { CredentialsProvider = _credentials });

            repo.Branches.Update(branch,
                b => b.TrackedBranch = $"refs/remotes/{remote.Name}/{branchName}");
        }
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
