using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     Learning where a guest actually is. Proxmox only records an address in a guest's
///     config when someone set one statically, so on a DHCP estate every guest arrived
///     without one — and an address is what lets a guest line up with the host a network
///     sweep found. The guest itself knows, and will say so through its agent.
///     <para>
///         The hard part is that the agent reports every interface inside the machine,
///         including the bridges Docker and Home Assistant create. Picking the wrong one
///         would record 172.17.0.1 as the machine's address, which every container host
///         on the estate would also report.
///     </para>
/// </summary>
public class ProxmoxGuestAddressTests {
    private const string _nicMac = "bc:24:11:00:1a:01";

    private static List<ProxmoxGuestAddress> AgentAddresses() =>
        ProxmoxResponseParser.ParseAgentInterfaces(Fixture.Read("pve-agent-interfaces.json"));

    [Fact]
    public void An_agent_reports_every_routable_ipv4_it_can_see() {
        List<ProxmoxGuestAddress> addresses = AgentAddresses();

        // Loopback and the IPv6 entries are dropped; the NIC and both docker bridges stay,
        // because deciding between them is the caller's job, not the parser's.
        Assert.Equal(["192.0.2.105", "172.17.0.1", "172.18.0.1"], addresses.Select(a => a.Ip));
    }

    [Fact]
    public void The_guests_own_nic_wins_over_the_bridges_it_created() =>
        Assert.Equal("192.0.2.105", ProxmoxDiscovery.SelectGuestIp(AgentAddresses(), [_nicMac]));

    [Fact]
    // Proxmox configs upper-case them; agents vary. Both go through the same normaliser
    // the ARP reader uses, so a scan and this collector always agree.
    public void The_mac_is_matched_however_either_side_spells_it() =>
        Assert.Equal("192.0.2.105", ProxmoxDiscovery.SelectGuestIp(AgentAddresses(), ["BC-24-11-00-1A-01"]));

    [Fact]
    // Every address on offer belongs to something the guest invented. Recording one
    // would be worse than recording nothing.
    public void Nothing_is_claimed_when_no_interface_carries_a_configured_mac() =>
        Assert.Null(ProxmoxDiscovery.SelectGuestIp(AgentAddresses(), ["bc:24:11:ff:ff:ff"]));

    [Fact]
    // A token that cannot read the guest config leaves no discriminator, so there is no
    // way to tell a NIC from a bridge.
    public void Nothing_is_claimed_when_the_configured_macs_are_unknown() =>
        Assert.Null(ProxmoxDiscovery.SelectGuestIp(AgentAddresses(), []));

    [Fact]
    public void An_agent_that_answers_nothing_yields_nothing() {
        Assert.Empty(ProxmoxResponseParser.ParseAgentInterfaces("""{"data":null}"""));
        Assert.Empty(ProxmoxResponseParser.ParseAgentInterfaces("""{"data":{"result":[]}}"""));
    }

    [Fact]
    public void A_container_reports_its_address_with_the_prefix_stripped() {
        List<ProxmoxGuestAddress> addresses =
            ProxmoxResponseParser.ParseContainerInterfaces(Fixture.Read("pve-lxc-interfaces.json"));

        ProxmoxGuestAddress only = Assert.Single(addresses);
        Assert.Equal("192.0.2.150", only.Ip);
        Assert.Equal("eth0", only.Interface);
    }

    [Fact]
    public void A_guest_that_failed_dhcp_is_not_recorded_at_its_self_assigned_address() {
        // 169.254 means "I could not get an address", which is not an address worth
        // writing into an inventory.
        var json = """
                   {"data":{"result":[{"name":"ens18","hardware-address":"bc:24:11:00:1a:01",
                   "ip-addresses":[{"ip-address":"169.254.12.7","ip-address-type":"ipv4","prefix":16}]}]}}
                   """;

        Assert.Empty(ProxmoxResponseParser.ParseAgentInterfaces(json));
    }

    [Fact]
    public async Task A_statically_configured_address_still_wins_over_the_agent() {
        // The config is what the administrator asked for; the agent is what the guest
        // happens to report. Where both exist they agree, and where they do not the
        // configured one is the intent.
        var client = new ScriptedProxmoxClient {
            Guests = [Guest(100, "static-guest")],
            Configs = { [100] = new ProxmoxGuestConfig("Linux", "192.0.2.9", [], [], [_nicMac]) },
            Addresses = { [100] = [new ProxmoxGuestAddress("ens18", _nicMac, "192.0.2.105")] }
        };

        List<Resource> resources = await ProxmoxDiscovery.ReadAsync(client);

        Assert.Equal("192.0.2.9", GuestCard(resources, "static-guest").Ip);
    }

    [Fact]
    public async Task A_dhcp_guest_takes_the_address_its_agent_reports() {
        var client = new ScriptedProxmoxClient {
            Guests = [Guest(101, "dhcp-guest")],
            Configs = { [101] = new ProxmoxGuestConfig("Linux", null, [], [], [_nicMac]) },
            Addresses = { [101] = [new ProxmoxGuestAddress("ens18", _nicMac, "192.0.2.105")] }
        };

        List<Resource> resources = await ProxmoxDiscovery.ReadAsync(client);

        Assert.Equal("192.0.2.105", GuestCard(resources, "dhcp-guest").Ip);
    }

    [Fact]
    public async Task A_stopped_guest_is_never_asked_where_it_is() {
        // It has no address to report, and asking costs a round trip per guest on an
        // estate where most guests may be off.
        var client = new ScriptedProxmoxClient {
            Guests = [Guest(102, "stopped-guest", "stopped")],
            Configs = { [102] = new ProxmoxGuestConfig("Linux", null, [], [], [_nicMac]) }
        };

        await ProxmoxDiscovery.ReadAsync(client);

        Assert.Empty(client.AddressCalls);
    }

    [Fact]
    public async Task The_guests_macs_reach_the_card() {
        // The MCP tool used to run its own copy of this orchestration which dropped the
        // MACs, silently costing every guest its chance of unifying with a scan.
        var client = new ScriptedProxmoxClient {
            Guests = [Guest(103, "mac-guest")],
            Configs = { [103] = new ProxmoxGuestConfig("Linux", null, [], [], [_nicMac]) }
        };

        List<Resource> resources = await ProxmoxDiscovery.ReadAsync(client);

        Assert.Equal(_nicMac, GuestCard(resources, "mac-guest").Labels["macs"]);
    }

    private static SystemResource GuestCard(List<Resource> resources, string name) =>
        resources.OfType<SystemResource>().Single(r => r.Name == name);

    private static ProxmoxGuest Guest(int vmId, string name, string status = "running") =>
        new() {
            VmId = vmId,
            Node = "pve01",
            Name = name,
            Type = "vm",
            Status = status
        };

    private sealed class ScriptedProxmoxClient : IProxmoxClient {
        public List<ProxmoxGuest> Guests { get; init; } = [];
        public Dictionary<int, ProxmoxGuestConfig> Configs { get; } = [];
        public Dictionary<int, List<ProxmoxGuestAddress>> Addresses { get; } = [];
        public List<int> AddressCalls { get; } = [];

        public string Endpoint => "https://pve.example.com:8006";

        public Task<string> GetIdentityScopeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("example-cluster");

        public Task<IReadOnlyList<ProxmoxNode>> GetNodesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProxmoxNode>>([new ProxmoxNode { Name = "pve01" }]);

        public Task<ProxmoxNode> EnrichAsync(ProxmoxNode node, CancellationToken cancellationToken = default) =>
            Task.FromResult(node);

        public Task<IReadOnlyList<ProxmoxGuest>> GetGuestsAsync(
            string node,
            string endpoint,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProxmoxGuest>>(
                endpoint == ProxmoxApiClient.QemuEndpoint ? Guests : []);

        public Task<IReadOnlyList<ProxmoxDisk>> GetDisksAsync(
            string node,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProxmoxDisk>>([]);

        public Task<IReadOnlyList<ProxmoxGpu>> GetGpusAsync(
            string node,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProxmoxGpu>>([]);

        public Task<ProxmoxGuestConfig> GetGuestConfigAsync(
            string node,
            string endpoint,
            int vmId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Configs.TryGetValue(vmId, out ProxmoxGuestConfig? config)
                ? config
                : new ProxmoxGuestConfig(null, null, [], []));

        public Task<IReadOnlyList<ProxmoxGuestAddress>> GetGuestAddressesAsync(
            string node,
            string endpoint,
            int vmId,
            CancellationToken cancellationToken = default) {
            AddressCalls.Add(vmId);

            return Task.FromResult<IReadOnlyList<ProxmoxGuestAddress>>(
                Addresses.TryGetValue(vmId, out List<ProxmoxGuestAddress>? found) ? found : []);
        }
    }
}
