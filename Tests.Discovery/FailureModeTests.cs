using RackPeek.Domain.Discovery;

namespace Tests.Discovery;

/// <summary>
///     Discovery runs unattended on machines nobody is watching, so its failures have to
///     be legible from a log line rather than a stack trace.
/// </summary>
public class FailureModeTests {
    [Fact]
    public async Task An_unreachable_server_fails_with_a_network_error_not_a_crash() {
        // Port 1 is reserved and nothing listens on it.
        using var publisher = new DiscoveryPublisher("http://127.0.0.1:1", "key");

        await Assert.ThrowsAnyAsync<HttpRequestException>(
            () => publisher.PublishAsync("version: 3\nresources: []\n", false));
    }

    [Fact]
    public async Task An_unreachable_docker_socket_reports_where_it_looked() {
        using var client = new DockerApiClient("unix:///tmp/definitely-not-a-docker.sock");

        Assert.Equal("unix:///tmp/definitely-not-a-docker.sock", client.Endpoint);

        await Assert.ThrowsAnyAsync<Exception>(() => client.ListContainersAsync());
    }

    [Fact]
    public void The_default_docker_endpoint_is_the_conventional_socket() {
        using var client = new DockerApiClient();

        // DOCKER_HOST wins when set, which is how the remote and Podman cases work.
        Assert.Contains("docker.sock", client.Endpoint, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_server_or_key_resolves_to_nothing_so_validation_can_catch_it(string? value) {
        Assert.Null(DiscoveryPublisher.ResolveServer(value)
                    ?? Environment.GetEnvironmentVariable(DiscoveryPublisher.ServerEnvironmentVariable));
    }

    [Fact]
    public void Garbage_from_the_docker_api_does_not_take_the_process_down() =>
        Assert.ThrowsAny<Exception>(() => DockerContainerParser.Parse("not json at all"));

    [Fact]
    public void A_container_with_no_name_is_skipped_rather_than_named_badly() {
        List<DockerContainer> containers = DockerContainerParser.Parse("""
                                                     [
                                                       { "Id": "abc", "Image": "x", "Ports": [] },
                                                       { "Id": "def", "Names": ["/real"], "Image": "y",
                                                         "Ports": [{ "PrivatePort": 80, "PublicPort": 80, "Type": "tcp" }] }
                                                     ]
                                                     """);

        Assert.Equal(["real"], containers.Select(c => c.Name));
    }

    [Fact]
    public async Task A_push_against_an_unreadable_config_fails_rather_than_overwriting_it() {
        // The server tolerates a malformed config at boot so the web UI can be used to
        // fix it. A push arriving in that state must fail — merging against the empty
        // in-memory collection and saving would replace the user's whole inventory
        // with just the pushed resources.
        const string malformed = "version: 3\nresources:\n  - kind: [not yaml";

        using var api = new DiscoveryApiFixture(malformed);

        SystemFacts facts = SystemFactsParser.Parse(new RawSystemSnapshot {
            Hostname = "pusher",
            Cores = 2,
            OsName = "Debian",
            MemoryBytes = 4L * 1024 * 1024 * 1024,
            PlatformUuid = "uuid"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            api.PublishAsync(DiscoveryDocument.ToYaml([SystemResourceMapper.ToResource(facts)])));

        Assert.Equal(malformed, api.StoredYaml);
    }

    [Fact]
    public void A_machine_with_no_network_still_produces_an_importable_resource() {
        SystemFacts facts = SystemFactsParser.Parse(new RawSystemSnapshot {
            Hostname = "offline-box",
            Cores = 2,
            OsName = "Debian",
            MemoryBytes = 4L * 1024 * 1024 * 1024,
            PlatformUuid = "uuid"
        });

        Assert.Null(facts.Ip);

        // ip is optional in the schema; type, os, cores and ram are not.
        Fixture.AssertConformsToSchema(
            DiscoveryDocument.ToYaml([SystemResourceMapper.ToResource(facts)]));
    }
}
