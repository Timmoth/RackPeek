using RackPeek.Domain.Discovery;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;

namespace Tests.Discovery;

/// <summary>
///     A captured <c>GET /containers/json</c> response in, Service resources out.
///     No Docker daemon is needed, so this runs anywhere.
/// </summary>
public class DockerDiscoveryTests {
    private const string _hostSeed = "7f3c9a1e5b2d4f6081a3c5e7b9d1f3a5";

    private static List<Service> Discover() =>
        DockerServiceMapper.ToResources(
            DockerContainerParser.Parse(Fixture.Read("docker-containers.json")),
            _hostSeed,
            "nas01",
            "192.168.1.20");

    [Fact]
    public void Only_containers_reachable_from_outside_the_host_become_services() {
        List<Service> services = Discover();

        // redis publishes nothing and pgadmin only binds loopback, so nothing outside
        // the host can reach either and neither is a service.
        Assert.Equal(["jellyfin", "paperless-ngx", "unifi", "wireguard"], services.Select(s => s.Name));
    }

    [Fact]
    public void A_binding_pinned_to_one_interface_is_reported_at_that_address() {
        Service unifi = Discover().Single(s => s.Name == "unifi");

        // The loopback 8843 binding is ignored; the 8443 binding is only reachable on
        // the address it is pinned to, so that address wins over the host's.
        Assert.Equal("192.168.1.21", unifi.Network!.Ip);
        Assert.Equal(8443, unifi.Network.Port);
    }

    [Fact]
    public void A_dual_stack_publish_is_one_binding_not_two() {
        List<DockerContainer> containers = DockerContainerParser.Parse(Fixture.Read("docker-containers.json"));

        // Docker reports 0.0.0.0 and :: separately for the same publish.
        DockerContainer jellyfin = containers.Single(c => c.Name == "jellyfin");

        Assert.Single(jellyfin.PublishedPorts);
        Assert.True(jellyfin.PublishedPorts[0].IsWildcard);
    }

    [Fact]
    public void A_service_carries_the_address_it_is_reachable_on() {
        Service jellyfin = Discover().Single(s => s.Name == "jellyfin");

        Assert.Equal("192.168.1.20", jellyfin.Network!.Ip);
        Assert.Equal(8096, jellyfin.Network.Port);
        Assert.Equal("TCP", jellyfin.Network.Protocol);
        Assert.Equal("jellyfin/jellyfin:10.9.6", jellyfin.Notes);
        Assert.Equal(["nas01"], jellyfin.RunsOn);
    }

    [Fact]
    public void Udp_bindings_keep_their_protocol() =>
        Assert.Equal("UDP", Discover().Single(s => s.Name == "wireguard").Network!.Protocol);

    [Fact]
    public void A_service_keeps_every_published_protocol_and_interface_address_in_order() {
        const string json = """[{"Names":["/app"],"Image":"app:latest","Ports":[{"IP":"127.0.0.1","PublicPort":8080,"Type":"tcp"},{"IP":"::","PublicPort":443,"Type":"tcp"},{"IP":"0.0.0.0","PublicPort":443,"Type":"tcp"},{"IP":"192.168.1.21","PublicPort":8443,"Type":"tcp"},{"IP":"192.168.1.22","PublicPort":8443,"Type":"udp"},{"IP":"::1","PublicPort":9090,"Type":"tcp"}]}]""";

        Service service = DockerServiceMapper.ToResources(
            DockerContainerParser.Parse(json), _hostSeed, "nas01", "192.168.1.20").Single();

        Assert.Equal(("192.168.1.20", 443, "TCP"),
            (service.Network!.Ip, service.Network.Port, service.Network.Protocol));
        Assert.Equal([
            ("192.168.1.20", 443, "TCP"),
            ("192.168.1.21", 8443, "TCP"),
            ("192.168.1.22", 8443, "UDP")
        ], service.Endpoints.Select(e => (e.Ip, e.Port, e.Protocol)));
        DockerContainer container = DockerContainerParser.Parse(json).Single();
        Service fromReverseOrder = DockerServiceMapper.ToResources(
            [container with { PublishedPorts = container.PublishedPorts.Reverse().ToArray() }],
            _hostSeed,
            "nas01",
            "192.168.1.20").Single();
        Assert.Equal(service.Endpoints.Select(e => (e.Ip, e.Port, e.Protocol)),
            fromReverseOrder.Endpoints.Select(e => (e.Ip, e.Port, e.Protocol)));
        Fixture.AssertConformsToSchema(DiscoveryDocument.ToYaml([service]));
    }

    [Fact]
    public void Rediscovery_replaces_the_endpoint_list_for_an_existing_service() {
        Service original = Discover().Single(s => s.Name == "jellyfin");
        Service incoming = new() {
            Kind = Service.KindLabel,
            Name = original.Name,
            Endpoints = [new Network { Ip = "192.168.1.20", Port = 8181, Protocol = "TCP" }]
        };

        Service merged = Assert.IsType<Service>(
            ResourceCollectionMerger.Merge([original], [incoming], MergeMode.Merge).Single());

        Assert.Equal(8181, Assert.Single(merged.Endpoints).Port);
    }

    [Fact]
    public void A_compose_project_becomes_a_tag_so_a_stack_stays_grouped() {
        Assert.Equal(["media"], Discover().Single(s => s.Name == "jellyfin").Tags);
        Assert.Equal(["paperless-stack"], Discover().Single(s => s.Name == "paperless-ngx").Tags);
    }

    [Fact]
    public void A_container_outside_compose_gets_no_tag() =>
        Assert.Empty(Discover().Single(s => s.Name == "wireguard").Tags);

    [Fact]
    public void The_same_container_name_on_two_hosts_is_two_different_resources() {
        List<DockerContainer> containers = DockerContainerParser.Parse(Fixture.Read("docker-containers.json"));

        List<Service> onNas = DockerServiceMapper.ToResources(containers, "host-a", "nas01", "192.168.1.20");
        List<Service> onPi = DockerServiceMapper.ToResources(containers, "host-b", "pi01", "192.168.1.34");

        Assert.NotEqual(onNas[0].DiscoveryId, onPi[0].DiscoveryId);
    }

    [Fact]
    public void Rediscovering_the_same_host_produces_the_same_ids() =>
        Assert.Equal(
            Discover().Select(s => s.DiscoveryId),
            Discover().Select(s => s.DiscoveryId));

    [Fact]
    public void Output_conforms_to_the_published_schema() =>
        Fixture.AssertConformsToSchema(DiscoveryDocument.ToYaml(Discover()));

    [Fact]
    public void An_empty_daemon_yields_nothing_rather_than_failing() =>
        Assert.Empty(DockerContainerParser.Parse("[]"));

    [Fact]
    public void A_socket_endpoint_counts_as_local_but_tcp_does_not() {
        // Local is what decides whether the host System rides along in the payload:
        // over TCP the locally probed facts describe this machine, not the engine's.
        // The no-argument default is not asserted here because it honours DOCKER_HOST,
        // which a developer machine may legitimately point anywhere.
        using var bySocket = new DockerApiClient("unix:///run/user/1000/podman/podman.sock");
        using var byTcp = new DockerApiClient("tcp://nas01:2375");

        Assert.True(bySocket.IsLocal);
        Assert.False(byTcp.IsLocal);
    }
}
