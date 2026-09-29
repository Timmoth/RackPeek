using LibGit2Sharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RackPeek.Domain;
using RackPeek.Domain.Git;

namespace Tests.Git;

[Collection("Process-wide static state")]
public sealed class GitConfigurationTests : IDisposable {
    private readonly string _tempDir;

    public GitConfigurationTests() {
        _tempDir = Path.Combine(
            Path.GetTempPath(),
            "rackpeek-git-cfg-tests",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() {
        try {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch {
            // ignore cleanup issues
        }
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    [Fact]
    public void Missing_Git_Token_Registers_NullGitRepository() {
        // Doc: GIT_TOKEN is required to enable the integration. Without it the
        // indicator never appears (RpkConstants.HasGitServices stays false) and
        // every git call is a no-op via NullGitRepository.
        var services = new ServiceCollection();

        services.AddGitServices(BuildConfig(new Dictionary<string, string?>()), _tempDir);

        ServiceProvider provider = services.BuildServiceProvider();
        IGitRepository repo = provider.GetRequiredService<IGitRepository>();

        Assert.IsType<NullGitRepository>(repo);
        Assert.False(RpkConstants.HasGitServices);
    }

    [Fact]
    public void Empty_Git_Token_Registers_NullGitRepository() {
        // An empty/whitespace GIT_TOKEN must be treated the same as unset.
        var services = new ServiceCollection();

        services.AddGitServices(BuildConfig(new Dictionary<string, string?> {
            ["GIT_TOKEN"] = "   "
        }), _tempDir);

        ServiceProvider provider = services.BuildServiceProvider();
        IGitRepository repo = provider.GetRequiredService<IGitRepository>();

        Assert.IsType<NullGitRepository>(repo);
        Assert.False(RpkConstants.HasGitServices);
    }

    [Fact]
    public void Git_Token_Set_Registers_LibGit2GitRepository_And_Flips_HasGitServices() {
        // Doc: with GIT_TOKEN set, a Git status indicator appears in the
        // header. The header gates the indicator on RpkConstants.HasGitServices.
        var services = new ServiceCollection();

        services.AddGitServices(BuildConfig(new Dictionary<string, string?> {
            ["GIT_TOKEN"] = "ghp_test_token",
            ["GIT_USERNAME"] = "octocat"
        }), _tempDir);

        ServiceProvider provider = services.BuildServiceProvider();
        IGitRepository repo = provider.GetRequiredService<IGitRepository>();

        Assert.IsType<LibGit2GitRepository>(repo);
        Assert.True(RpkConstants.HasGitServices);
    }

    [Fact]
    public void Git_Username_Defaults_To_git_When_Only_Token_Provided() {
        // Doc table says GIT_USERNAME is "required", but in practice the code
        // defaults it to the conventional "git" so users with personal access
        // tokens that don't care about the username (e.g. GitHub fine-grained
        // tokens) still authenticate correctly.
        var services = new ServiceCollection();

        services.AddGitServices(BuildConfig(new Dictionary<string, string?> {
            ["GIT_TOKEN"] = "ghp_test_token"
        }), _tempDir);

        IGitCredentialsProvider creds = services.BuildServiceProvider()
            .GetRequiredService<IGitCredentialsProvider>();

        LibGit2Sharp.Handlers.CredentialsHandler handler = creds.GetHandler();
        var credentials = (UsernamePasswordCredentials)handler("https://example.com", null!,
            SupportedCredentialTypes.UsernamePassword);

        Assert.Equal("git", credentials.Username);
        Assert.Equal("ghp_test_token", credentials.Password);
    }

    [Fact]
    public void Insecure_Tls_Flag_Defaults_To_False() {
        // Doc: GIT_INSECURE_TLS is optional. Default behaviour must validate
        // certificates so we don't silently accept MITM on public hosts.
        var services = new ServiceCollection();

        services.AddGitServices(BuildConfig(new Dictionary<string, string?> {
            ["GIT_TOKEN"] = "ghp_test_token"
        }), _tempDir);

        var repo = (LibGit2GitRepository)services.BuildServiceProvider()
            .GetRequiredService<IGitRepository>();

        Assert.False(repo.InsecureTls);
    }

    [Fact]
    public void Insecure_Tls_True_Is_Plumbed_Through_To_Repository() {
        // Doc: GIT_INSECURE_TLS=true skips TLS validation. The flag must
        // reach the repository instance that runs push/pull/fetch.
        var services = new ServiceCollection();

        services.AddGitServices(BuildConfig(new Dictionary<string, string?> {
            ["GIT_TOKEN"] = "ghp_test_token",
            ["GIT_INSECURE_TLS"] = "true"
        }), _tempDir);

        var repo = (LibGit2GitRepository)services.BuildServiceProvider()
            .GetRequiredService<IGitRepository>();

        Assert.True(repo.InsecureTls);
    }

    [Fact]
    public void Insecure_Tls_String_Comparison_Is_Case_Insensitive() {
        // Common YAML/.env idiom is "True" or "TRUE"; the doc shows lowercase
        // but users will type whatever feels natural.
        var services = new ServiceCollection();

        services.AddGitServices(BuildConfig(new Dictionary<string, string?> {
            ["GIT_TOKEN"] = "ghp_test_token",
            ["GIT_INSECURE_TLS"] = "TRUE"
        }), _tempDir);

        var repo = (LibGit2GitRepository)services.BuildServiceProvider()
            .GetRequiredService<IGitRepository>();

        Assert.True(repo.InsecureTls);
    }

    [Fact]
    public void Insecure_Tls_Any_Other_Value_Means_False() {
        // Only "true" enables the bypass — strings like "yes", "1", etc.
        // must NOT silently disable TLS.
        var services = new ServiceCollection();

        services.AddGitServices(BuildConfig(new Dictionary<string, string?> {
            ["GIT_TOKEN"] = "ghp_test_token",
            ["GIT_INSECURE_TLS"] = "yes"
        }), _tempDir);

        var repo = (LibGit2GitRepository)services.BuildServiceProvider()
            .GetRequiredService<IGitRepository>();

        Assert.False(repo.InsecureTls);
    }
}
