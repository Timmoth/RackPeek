using System.Net;
using RackPeek.Domain.Discovery;

namespace Tests.Discovery;

/// <summary>
///     Exercises the IO half against a stub that records what was actually asked for.
///     Path construction and the authorization header cannot be checked any other way
///     without a Proxmox to point at.
/// </summary>
public class ProxmoxClientTests {
    private static (ProxmoxApiClient Client, StubHandler Stub) Create(
        string host = "https://pve.lan:8006",
        bool clusterAvailable = true) {
        var stub = new StubHandler(clusterAvailable);

        return (new ProxmoxApiClient(host, "root@pam!rackpeek", "secret-uuid", false, new HttpClient(stub)), stub);
    }

    [Fact]
    public async Task Requests_go_to_the_documented_api_paths() {
        (ProxmoxApiClient client, StubHandler stub) = Create();

        using (client) {
            await client.GetNodesAsync();
            await client.EnrichAsync(new ProxmoxNode { Name = "pve01" });
            await client.GetGuestsAsync("pve01", ProxmoxApiClient.QemuEndpoint);
            await client.GetGuestConfigAsync("pve01", ProxmoxApiClient.LxcEndpoint, 201);
        }

        Assert.Equal([
            "/api2/json/nodes",
            "/api2/json/nodes/pve01/status",
            "/api2/json/nodes/pve01/disks/list",
            "/api2/json/nodes/pve01/hardware/pci",
            "/api2/json/nodes/pve01/qemu",
            "/api2/json/nodes/pve01/lxc/201/config"
        ], stub.Paths);
    }

    [Fact]
    public async Task The_token_is_sent_the_way_proxmox_expects_it() {
        (ProxmoxApiClient client, StubHandler stub) = Create();

        using (client)
            await client.GetNodesAsync();

        Assert.Equal("PVEAPIToken=root@pam!rackpeek=secret-uuid", stub.Authorization);
    }

    [Theory]
    [InlineData("pve.lan", "https://pve.lan:8006")]
    [InlineData("https://pve.lan:8006", "https://pve.lan:8006")]
    [InlineData("https://pve.lan:8006/", "https://pve.lan:8006")]
    [InlineData("http://10.0.50.10:8006", "http://10.0.50.10:8006")]
    public void A_bare_host_name_gets_the_scheme_and_port_proxmox_uses(string input, string expected) {
        (ProxmoxApiClient client, _) = Create(input);

        using (client)
            Assert.Equal(expected, client.Endpoint);
    }

    [Fact]
    public async Task A_clustered_host_identifies_guests_by_the_cluster() {
        (ProxmoxApiClient client, _) = Create();

        using (client)
            Assert.Equal("homelab", await client.GetIdentityScopeAsync());
    }

    [Fact]
    public async Task A_standalone_host_has_no_cluster_endpoint_and_falls_back_to_its_node() {
        // Proxmox answers an error rather than an empty list when there is no cluster.
        (ProxmoxApiClient client, _) = Create(clusterAvailable: false);

        using (client)
            Assert.Equal("pve01", await client.GetIdentityScopeAsync());
    }

    [Fact]
    public async Task A_guest_that_vanishes_mid_run_contributes_nothing_instead_of_failing() {
        (ProxmoxApiClient client, _) = Create();

        using (client) {
            ProxmoxGuestConfig config =
                await client.GetGuestConfigAsync("pve01", ProxmoxApiClient.QemuEndpoint, 999);

            Assert.Null(config.Os);
            Assert.Null(config.Ip);
        }
    }

    [Fact]
    public async Task A_rejected_token_says_so_in_terms_that_point_at_the_fix() {
        var stub = new StubHandler(true) { Unauthorized = true };

        using var client = new ProxmoxApiClient(
            "https://pve.lan:8006", "bad", "worse", false, new HttpClient(stub));

        HttpRequestException error =
            await Assert.ThrowsAsync<HttpRequestException>(() => client.GetNodesAsync());

        Assert.Contains("user@realm!tokenname", error.Message);
    }

    private sealed class StubHandler(bool clusterAvailable) : HttpMessageHandler {
        public List<string> Paths { get; } = [];
        public string? Authorization { get; private set; }
        public bool Unauthorized { get; init; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) {
            var path = request.RequestUri!.AbsolutePath;
            Paths.Add(path);
            Authorization = request.Headers.TryGetValues("Authorization", out IEnumerable<string>? values)
                ? string.Join("", values)
                : null;

            if (Unauthorized)
                return Respond(HttpStatusCode.Unauthorized, "{}");

            return path switch {
                "/api2/json/nodes" => Respond(HttpStatusCode.OK, Fixture.Read("pve-nodes-full.json")),
                "/api2/json/cluster/status" when clusterAvailable =>
                    Respond(HttpStatusCode.OK, Fixture.Read("pve-cluster-status.json")),
                "/api2/json/cluster/status" => Respond(HttpStatusCode.InternalServerError, "{}"),
                "/api2/json/nodes/pve01/status" => Respond(HttpStatusCode.OK, Fixture.Read("pve-node-status.json")),
                "/api2/json/nodes/pve01/qemu" => Respond(HttpStatusCode.OK, Fixture.Read("pve-qemu.json")),
                "/api2/json/nodes/pve01/lxc" => Respond(HttpStatusCode.OK, Fixture.Read("pve-lxc.json")),
                "/api2/json/nodes/pve01/disks/list" => Respond(HttpStatusCode.OK, Fixture.Read("pve-disks.json")),
                "/api2/json/nodes/pve01/hardware/pci" => Respond(HttpStatusCode.OK, Fixture.Read("pve-hardware-pci.json")),
                "/api2/json/nodes/pve01/lxc/201/config" =>
                    Respond(HttpStatusCode.OK, Fixture.Read("pve-lxc-config.json")),
                _ => Respond(HttpStatusCode.NotFound, "{}")
            };
        }

        private static Task<HttpResponseMessage> Respond(HttpStatusCode status, string body) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}
