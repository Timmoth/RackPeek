using Tests.EndToEnd.Infra;
using Xunit.Abstractions;

namespace Tests.EndToEnd.DiscoveryTests;

/// <summary>
///     `rpk discover docker` against endpoints it cannot reach. Every case here fails
///     before any container is listed, so these tests never talk to a daemon.
/// </summary>
[Collection("Yaml CLI tests")]
public class DiscoverDockerEndpointTests(TempYamlCliFixture fs, ITestOutputHelper outputHelper)
    : IClassFixture<TempYamlCliFixture> {
    private async Task<string> ExecuteAsync(params string[] args) =>
        await YamlCliTestHost.RunAsync(args, fs.Root, outputHelper, "config.yaml");

    [Theory]
    [InlineData("ssh://user@nas")]
    [InlineData("npipe:////./pipe/docker_engine")]
    public async Task an_endpoint_that_cannot_be_dialled_says_so_instead_of_crashing(string endpoint) {
        // `docker context` sets ssh:// for a remote host and npipe:// is the Windows
        // default, so both are ordinary values to find in DOCKER_HOST. They used to
        // reach HttpClient, which accepts the URI and throws NotSupportedException on
        // the first request — past the command's catch list, so the user got
        // "Unexpected error occurred" and a stack trace.
        var output = await ExecuteAsync("discover", "docker", "--docker-host", endpoint);

        Assert.DoesNotContain("Unexpected error", output);
        Assert.DoesNotContain("at RackPeek.", output);

        // And the message says what to do about it.
        Assert.Contains("ssh -L", output);
    }
}
