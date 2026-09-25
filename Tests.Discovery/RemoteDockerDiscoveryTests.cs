using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     Over TCP the machine running the command is not the machine running the
///     containers, so nothing probed locally may leak into what gets recorded: identity
///     comes from the engine (<c>GET /info</c>), the address from the endpoint the user
///     dialled, and a rename of the host is preserved by the merge rather than by the
///     collector, which cannot see it.
/// </summary>
public class RemoteDockerDiscoveryTests {
    // -- GET /info --------------------------------------------------------------------

    [Fact]
    public void The_engines_identity_is_read_from_info() {
        DockerEngineInfo? info = DockerEngineInfoParser.Parse(Fixture.Read("docker-info.json"));

        Assert.NotNull(info);
        Assert.Equal("e7c3a2d0-5a8f-4b2e-9c1d-2f6e8a9b0c3d", info.Id);
        Assert.Equal("nas01", info.Hostname);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"ID": "", "Name": "   "}""")]
    [InlineData("""{"ID": 42, "Name": ["nas01"]}""")]
    public void Missing_or_unusable_fields_come_back_null_rather_than_empty(string json) {
        DockerEngineInfo? info = DockerEngineInfoParser.Parse(json);

        Assert.NotNull(info);
        Assert.Null(info.Id);
        Assert.Null(info.Hostname);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("[]")]
    [InlineData("\"a string\"")]
    public void A_broken_info_response_is_null_not_an_exception(string json) =>
        Assert.Null(DockerEngineInfoParser.Parse(json));

    // -- The endpoint -----------------------------------------------------------------

    [Theory]
    [InlineData("tcp://nas01:2375", "nas01")]
    [InlineData("tcp://192.168.1.20:2375", "192.168.1.20")]
    [InlineData("http://nas01.lan:2376", "nas01.lan")]
    public void The_remote_host_is_the_host_part_of_the_endpoint(string endpoint, string expected) {
        using var client = new DockerApiClient(endpoint);

        Assert.Equal(expected, client.RemoteHost);
    }

    [Fact]
    public void A_local_socket_has_no_remote_host() {
        using var client = new DockerApiClient("unix:///var/run/docker.sock");

        Assert.Null(client.RemoteHost);
    }

    [Fact]
    public async Task An_ipv4_endpoint_address_is_used_verbatim() =>
        Assert.Equal("192.168.1.20", await DockerApiClient.ResolveIpv4Async("192.168.1.20"));

    [Fact]
    public async Task An_ipv6_endpoint_yields_null_because_the_schema_holds_ipv4() =>
        Assert.Null(await DockerApiClient.ResolveIpv4Async("::1"));

    [Fact]
    public async Task A_resolvable_name_becomes_its_ipv4_address() =>
        // localhost is the one name every test machine resolves without real DNS.
        Assert.Equal("127.0.0.1", await DockerApiClient.ResolveIpv4Async("localhost"));

    [Fact]
    public async Task An_unresolvable_name_is_null_rather_than_an_exception() =>
        Assert.Null(await DockerApiClient.ResolveIpv4Async("host name with spaces!"));

    // -- Identity independent of the workstation ---------------------------------------

    [Fact]
    public void Two_workstations_discovering_the_same_engine_agree_on_every_id() {
        List<DockerContainer> containers = DockerContainerParser.Parse(Fixture.Read("docker-containers.json"));
        const string engineId = "e7c3a2d0-5a8f-4b2e-9c1d-2f6e8a9b0c3d";

        // Same engine seed; everything the workstation contributes differs.
        List<Service> fromLaptop = DockerServiceMapper.ToResources(containers, engineId, "nas01", "192.168.1.20");
        List<Service> fromCi = DockerServiceMapper.ToResources(containers, engineId, "nas01", "10.0.0.9");

        Assert.Equal(
            fromLaptop.Select(s => s.DiscoveryId),
            fromCi.Select(s => s.DiscoveryId));
    }

    // -- Preserving the user's links when the collector cannot see the host ------------

    private static SystemResource StoredHost(string name) => new() {
        Kind = SystemResource.KindLabel,
        Name = name,
        DiscoveryId = DiscoveryId.Create(DiscoveryId.SystemScheme, "machine-a"),
        Type = "baremetal",
        Os = "Debian",
        Cores = 12,
        Ram = 63
    };

    private static Service ServiceRunningOn(string host, string container = "jellyfin") => new() {
        Kind = Service.KindLabel,
        Name = container,
        DiscoveryId = DiscoveryId.Create(DiscoveryId.DockerScheme, $"engine-a/{container}"),
        RunsOn = [host]
    };

    [Fact]
    public void A_dangling_runs_on_keeps_the_stored_link_the_user_chose() {
        // The host was renamed on the server; a remote collector still sends the
        // hostname it sees, which no longer names anything.
        List<Resource> existing = [StoredHost("storage-01"), ServiceRunningOn("storage-01")];
        List<Resource> incoming = [ServiceRunningOn("nas01")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal(["storage-01"], incoming[0].RunsOn);
    }

    [Fact]
    public void A_genuine_move_to_a_documented_host_is_recorded() {
        List<Resource> existing = [StoredHost("storage-01"), StoredHost2("pi01"), ServiceRunningOn("storage-01")];
        List<Resource> incoming = [ServiceRunningOn("pi01")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal(["pi01"], incoming[0].RunsOn);
    }

    [Fact]
    public void A_move_to_a_host_arriving_in_the_same_payload_is_recorded() {
        // Local discovery sends the host along; its (possibly renamed) name anchors
        // the services, so nothing here should fall back to the stored link.
        List<Resource> existing = [StoredHost("storage-01"), ServiceRunningOn("storage-01")];
        List<Resource> incoming = [StoredHost2("pi01"), ServiceRunningOn("pi01")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal(["pi01"], incoming[1].RunsOn);
    }

    [Fact]
    public void A_service_seen_for_the_first_time_keeps_whatever_it_reports() {
        // Nothing stored to preserve: the dangling name is still the best available.
        List<Resource> existing = [StoredHost("storage-01")];
        List<Resource> incoming = [ServiceRunningOn("nas01")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal(["nas01"], incoming[0].RunsOn);
    }

    [Fact]
    public void A_stored_service_with_no_link_gains_the_reported_one() {
        Service unparented = ServiceRunningOn("storage-01");
        unparented.RunsOn = [];

        List<Resource> existing = [unparented];
        List<Resource> incoming = [ServiceRunningOn("nas01")];

        DiscoveryIdResolver.ResolveNames(existing, incoming);

        Assert.Equal(["nas01"], incoming[0].RunsOn);
    }

    /// <summary>A second stored host with its own identity.</summary>
    private static SystemResource StoredHost2(string name) {
        SystemResource host = StoredHost(name);
        host.DiscoveryId = DiscoveryId.Create(DiscoveryId.SystemScheme, "machine-b");

        return host;
    }
}
