using ModelContextProtocol.Client;
using RackPeek.Mcp.Tools;

namespace Tests.Mcp;

/// <summary>
///     Git tools ride on the same GIT_TOKEN opt-in as the web UI: without it they say
///     how to turn git on; with it the config directory is a real repository (the
///     server auto-inits it) and commits are observable through git_status.
/// </summary>
public class GitToolTests {
    private static Dictionary<string, string?> WithGit() => new() {
        ["GIT_TOKEN"] = "dummy-token-for-local-repo"
    };

    [Fact]
    public async Task Without_a_token_the_tools_explain_how_to_enable_git() {
        using var api = new McpFixture(TestData.Seed);
        await using McpClient client = await api.ConnectAsync();

        GitStatusResult status = await client.CallOkAsync<GitStatusResult>("git_status");

        Assert.False(status.Available);
        Assert.Contains("GIT_TOKEN", status.Message);

        var error = await client.CallErrorAsync(
            "git_commit", new Dictionary<string, object?> { ["message"] = "won't happen" });
        Assert.Contains("GIT_TOKEN", error);
    }

    [Fact]
    public async Task A_fresh_config_repo_reports_dirty_then_commits_clean() {
        using var api = new McpFixture(TestData.Seed, WithGit());
        await using McpClient client = await api.ConnectAsync();

        GitStatusResult before = await client.CallOkAsync<GitStatusResult>("git_status");
        Assert.True(before.Available);
        Assert.Equal("Dirty", before.Status); // config.yaml is untracked
        Assert.False(before.HasRemote);

        var committed = await client.CallTextAsync(
            "git_commit", new Dictionary<string, object?> { ["message"] = "inventory snapshot" });
        Assert.Equal("Committed.", committed);

        GitStatusResult after = await client.CallOkAsync<GitStatusResult>("git_status");
        Assert.Equal("Clean", after.Status);
        Assert.NotNull(after.RecentCommits);
        Assert.Contains(after.RecentCommits, c => c.Contains("inventory snapshot"));
    }

    [Fact]
    public async Task An_mcp_edit_shows_up_as_a_dirty_tree_ready_to_commit() {
        using var api = new McpFixture(TestData.Seed, WithGit());
        await using McpClient client = await api.ConnectAsync();

        await client.CallTextAsync(
            "git_commit", new Dictionary<string, object?> { ["message"] = "baseline" });

        await client.CallOkAsync<TagsResult>("edit_tags", new Dictionary<string, object?> {
            ["name"] = "rack-server",
            ["add"] = new[] { "audited" }
        });

        GitStatusResult status = await client.CallOkAsync<GitStatusResult>("git_status");
        Assert.Equal("Dirty", status.Status);
        Assert.NotNull(status.ChangedFiles);
        Assert.Contains(status.ChangedFiles, f => f.Contains("config.yaml"));
    }

    [Fact]
    public async Task Committing_a_clean_tree_succeeds_without_inventing_a_commit() {
        using var api = new McpFixture(TestData.Seed, WithGit());
        await using McpClient client = await api.ConnectAsync();

        await client.CallTextAsync(
            "git_commit", new Dictionary<string, object?> { ["message"] = "first" });
        var second = await client.CallTextAsync(
            "git_commit", new Dictionary<string, object?> { ["message"] = "second" });

        Assert.Equal("Committed.", second);

        GitStatusResult status = await client.CallOkAsync<GitStatusResult>("git_status");
        Assert.NotNull(status.RecentCommits);
        var commit = Assert.Single(status.RecentCommits);
        Assert.Contains("first", commit);
    }

    [Fact]
    public async Task Pushing_without_a_remote_is_an_error_that_says_the_commit_happened() {
        using var api = new McpFixture(TestData.Seed, WithGit());
        await using McpClient client = await api.ConnectAsync();

        var error = await client.CallErrorAsync("git_commit", new Dictionary<string, object?> {
            ["message"] = "local only",
            ["push"] = true
        });

        Assert.Contains("Committed, but the push failed", error);
        Assert.Contains("No remote", error);

        GitStatusResult status = await client.CallOkAsync<GitStatusResult>("git_status");
        Assert.Equal("Clean", status.Status); // the commit itself landed
    }
}
