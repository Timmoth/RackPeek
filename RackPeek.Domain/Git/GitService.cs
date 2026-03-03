using System.Diagnostics;

namespace RackPeek.Domain.Git;

public sealed class GitService : IGitService
{
    private readonly string _workingDirectory;
    private readonly bool _isAvailable;

    public GitService(string configDirectory)
    {
        _workingDirectory = configDirectory;
        _isAvailable = CheckGitAvailable();
    }

    public bool IsAvailable => _isAvailable;

    public async Task<GitRepoStatus> GetStatusAsync()
    {
        if (!_isAvailable)
            return GitRepoStatus.NotAvailable;

        var (exitCode, output) = await RunGitAsync("status", "--porcelain");
        if (exitCode != 0)
            return GitRepoStatus.NotAvailable;

        return string.IsNullOrWhiteSpace(output)
            ? GitRepoStatus.Clean
            : GitRepoStatus.Dirty;
    }

    public async Task<string?> CommitAllAsync(string message)
    {
        if (!_isAvailable)
            return "Git is not available.";

        var (addExit, addOutput) = await RunGitAsync("add", "-A");
        if (addExit != 0)
            return $"git add failed: {addOutput}";

        var (commitExit, commitOutput) = await RunGitAsync("commit", "-m", message);
        if (commitExit != 0)
        {
            if (commitOutput.Contains("nothing to commit"))
                return null;
            return $"git commit failed: {commitOutput}";
        }

        return null;
    }

    public async Task<string[]> GetChangedFilesAsync()
    {
        if (!_isAvailable)
            return [];

        var (exitCode, output) = await RunGitAsync("status", "--porcelain");
        if (exitCode != 0 || string.IsNullOrWhiteSpace(output))
            return [];

        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    public async Task<string> GetDiffAsync()
    {
        if (!_isAvailable)
            return string.Empty;

        // Show both staged and unstaged changes, plus untracked files
        var (_, trackedDiff) = await RunGitAsync("diff", "HEAD");
        var (_, untrackedFiles) = await RunGitAsync("ls-files", "--others", "--exclude-standard");

        var result = trackedDiff;
        if (!string.IsNullOrWhiteSpace(untrackedFiles))
        {
            var files = untrackedFiles.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var file in files)
            {
                var (_, content) = await RunGitAsync("diff", "--no-index", "/dev/null", file);
                if (!string.IsNullOrWhiteSpace(content))
                    result = string.IsNullOrWhiteSpace(result) ? content : $"{result}\n{content}";
            }
        }

        return result;
    }

    public async Task<string?> RestoreAllAsync()
    {
        if (!_isAvailable)
            return "Git is not available.";

        // Restore tracked files
        var (restoreExit, restoreOutput) = await RunGitAsync("checkout", "--", ".");
        if (restoreExit != 0)
            return $"git restore failed: {restoreOutput}";

        // Remove untracked files
        var (cleanExit, cleanOutput) = await RunGitAsync("clean", "-fd");
        if (cleanExit != 0)
            return $"git clean failed: {cleanOutput}";

        return null;
    }

    public async Task<string> GetCurrentBranchAsync()
    {
        if (!_isAvailable)
            return string.Empty;

        var (exitCode, output) = await RunGitAsync("branch", "--show-current");
        return exitCode == 0 ? output : string.Empty;
    }

    public async Task<GitLogEntry[]> GetLogAsync(int count = 20)
    {
        if (!_isAvailable)
            return [];

        var (exitCode, output) = await RunGitAsync(
            "log", $"-{count}", "--format=%h\t%s\t%an\t%ar");

        if (exitCode != 0 || string.IsNullOrWhiteSpace(output))
            return [];

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line =>
            {
                var parts = line.Split('\t', 4);
                return parts.Length >= 4
                    ? new GitLogEntry(parts[0], parts[1], parts[2], parts[3])
                    : new GitLogEntry(parts.ElementAtOrDefault(0) ?? "", line, "", "");
            })
            .ToArray();
    }

    private bool CheckGitAvailable()
    {
        try
        {
            var gitCheck = RunGitAsync("--version").GetAwaiter().GetResult();
            if (gitCheck.ExitCode != 0) return false;

            var repoCheck = RunGitAsync("rev-parse", "--is-inside-work-tree")
                .GetAwaiter().GetResult();
            return repoCheck.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<(int ExitCode, string Output)> RunGitAsync(params string[] arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = _workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var output = string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
        return (process.ExitCode, output.Trim());
    }
}
