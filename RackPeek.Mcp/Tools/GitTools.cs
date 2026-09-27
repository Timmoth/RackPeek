using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RackPeek.Domain.Git;
using RackPeek.Domain.Git.UseCases;

namespace RackPeek.Mcp.Tools;

public sealed record GitStatusResult(
    [property: Description("False when the server has no GIT_TOKEN configured — every other field is then empty.")]
    bool Available,
    string? Message,
    string? Branch = null,
    [property: Description("Clean or Dirty.")] string? Status = null,
    string[]? ChangedFiles = null,
    bool HasRemote = false,
    [property: Description("Commits the remote is missing. Null when there is no remote.")]
    int? Ahead = null,
    [property: Description("Commits this repo is missing. Null when there is no remote.")]
    int? Behind = null,
    List<string>? RecentCommits = null);

/// <summary>
///     Version control over the config directory. Only active when the server is
///     started with GIT_TOKEN — otherwise <see cref="NullGitRepository" /> is wired
///     in and these tools explain how to enable it instead of failing obscurely.
/// </summary>
[McpServerToolType]
public sealed class GitTools(IGitRepository repo, IServiceProvider services) {
    private const string _notConfigured =
        "Git integration is not configured. Start the server with GIT_TOKEN " +
        "(and optionally GIT_USERNAME) set to enable it.";

    [McpServerTool(Name = "git_status", UseStructuredContent = true, ReadOnly = true, OpenWorld = false)]
    [Description("The config repository's branch, dirty/clean state, changed files, remote sync state and recent commits.")]
    public Task<GitStatusResult> GitStatus() {
        return ToolErrors.RunAsync(() => {
            if (!repo.IsAvailable)
                return Task.FromResult(new GitStatusResult(false, _notConfigured));

            try {
                var hasRemote = repo.HasRemote();
                GitSyncStatus? sync = hasRemote ? repo.FetchAndGetSyncStatus() : null;

                GitLogEntry[] log;
                try {
                    log = repo.GetLog(5);
                }
                catch {
                    // An empty repository has no log yet; that is not an error.
                    log = [];
                }

                return Task.FromResult(new GitStatusResult(
                    true,
                    sync?.Error,
                    repo.GetCurrentBranch(),
                    repo.GetStatus().ToString(),
                    repo.GetChangedFiles(),
                    hasRemote,
                    sync?.Ahead,
                    sync?.Behind,
                    log.Select(e => $"{e.Hash} {e.Date} {e.Author}: {e.Message}").ToList()));
            }
            catch (Exception ex) when (ex is not McpException) {
                throw new McpException($"Git error: {ex.Message}");
            }
        });
    }

    [McpServerTool(Name = "git_commit", Idempotent = true, OpenWorld = false)]
    [Description("Stages everything in the config directory and commits it. A clean tree commits nothing and still succeeds.")]
    public Task<string> GitCommit(
        [Description("The commit message.")] string message,
        [Description("Also push to the configured remote.")] bool push = false) {
        return ToolErrors.RunAsync(async () => {
            if (!repo.IsAvailable)
                throw new McpException(_notConfigured);

            var error = await services.GetRequiredService<CommitAllUseCase>().ExecuteAsync(message);
            if (error != null)
                throw new McpException(error);

            if (!push)
                return "Committed.";

            var pushError = await services.GetRequiredService<PushUseCase>().ExecuteAsync();
            if (pushError != null)
                throw new McpException($"Committed, but the push failed: {pushError}");

            return "Committed and pushed.";
        });
    }
}
