using RackPeek.Domain.Discovery;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.SystemResources;

namespace Tests.Discovery;

/// <summary>
///     How a swept host gets a name and a vendor. Sweeping a homelab that has no PTR
///     records used to produce a page of "host-&lt;hash&gt;" cards; these tests own the
///     rules that turn those into recognisable machines.
/// </summary>
public class NetworkIdentityTests {
    private static NetworkHostFact Host(
        string ip,
        string? mac = null,
        string? hostname = null,
        ServiceIdentity? identity = null,
        string? vendor = null,
        IReadOnlyList<ServiceIdentity>? services = null) =>
        new(ip, mac, hostname, true, []) {
            Identity = identity,
            Vendor = vendor,
            Services = services ?? []
        };

    private static SystemResource Single(params NetworkHostFact[] hosts) =>
        Assert.IsType<SystemResource>(Assert.Single(NetworkScanMapper.ToResources(hosts)));

    [Fact]
    public void A_certificate_name_becomes_the_card_name() {
        SystemResource card = Single(Host(
            "192.0.2.13",
            identity: new ServiceIdentity("pve-node-01.example.com", IdentitySource.TlsCertificate, 8006)));

        // The label is the host part, as it is for a PTR name.
        Assert.Equal("pve-node-01", card.Name);
    }

    [Fact]
    public void A_ptr_record_still_wins_over_a_service_banner() {
        // The network's own answer beats whatever a certificate happens to say — a
        // stale or copied certificate must never rename a host DNS already knows.
        SystemResource card = Single(Host(
            "192.0.2.13",
            hostname: "pve-01.lan",
            identity: new ServiceIdentity("pve-node-01.example.com", IdentitySource.TlsCertificate, 8006)));

        Assert.Equal("pve-01", card.Name);
    }

    [Fact]
    public void Without_any_name_the_card_still_falls_back_to_the_hash() {
        SystemResource card = Single(Host("192.0.2.111"));

        Assert.StartsWith("host-", card.Name);
    }

    [Fact]
    public void What_the_host_said_is_recorded_even_when_it_named_the_card() {
        SystemResource card = Single(Host(
            "192.0.2.204",
            identity: new ServiceIdentity("Home Assistant", IdentitySource.Http, 8123)));

        // Whoever reads the card can see the name came from an HTTP title on 8123 and
        // judge it accordingly, rather than trusting a name of unknown provenance.
        Assert.Equal("http:8123 Home Assistant", card.Labels["identified-by"]);
    }

    [Fact]
    public void An_ssh_greeting_annotates_but_never_names() {
        // Every Linux box on a subnet runs the same daemon. Naming from the greeting
        // produced five cards called "openssh" on a real sweep — no more use than five
        // called "host-<hash>", and misleading about what was actually identified.
        SystemResource card = Single(Host(
            "192.0.2.209",
            identity: new ServiceIdentity("OpenSSH", IdentitySource.SshBanner, 22)));

        Assert.StartsWith("host-", card.Name);
        Assert.Equal("ssh:22 OpenSSH", card.Labels["identified-by"]);
    }

    [Fact]
    public void An_application_that_named_itself_over_http_becomes_a_service_on_its_host() {
        List<Resource> cards = NetworkScanMapper.ToResources([
            Host("192.0.2.204", services: [new ServiceIdentity("Home Assistant", IdentitySource.Http, 8123)])
        ]);

        SystemResource host = Assert.Single(cards.OfType<SystemResource>());
        Service service = Assert.Single(cards.OfType<Service>());

        Assert.Equal("home-assistant", service.Name);
        Assert.Equal([host.Name], service.RunsOn);
        Assert.Equal("192.0.2.204", service.Network?.Ip);
        Assert.Equal(8123, service.Network?.Port);
    }

    [Fact]
    public void A_page_title_names_the_service_and_leaves_the_host_a_hash() {
        // A host may run several applications, so naming the machine after whichever
        // answered first is arbitrary. Only a certificate names the machine itself.
        List<Resource> cards = NetworkScanMapper.ToResources([
            Host("192.0.2.204", services: [new ServiceIdentity("Home Assistant", IdentitySource.Http, 8123)])
        ]);

        Assert.StartsWith("host-", Assert.Single(cards.OfType<SystemResource>()).Name);
    }

    [Fact]
    public void Several_applications_on_one_host_each_get_their_own_service() {
        List<Resource> cards = NetworkScanMapper.ToResources([
            Host("192.0.2.105", services: [
                new ServiceIdentity("Portainer", IdentitySource.Http, 9000),
                new ServiceIdentity("Grafana", IdentitySource.Http, 3000)
            ])
        ]);

        var services = cards.OfType<Service>().ToList();

        Assert.Equal(2, services.Count);
        Assert.Equal([9000, 3000], services.Select(s => s.Network!.Port));
        // Each port is its own thing, so each keeps its own stable id.
        Assert.Equal(2, services.Select(s => s.DiscoveryId).Distinct().Count());
    }

    [Fact]
    public void An_appliances_own_management_page_is_not_a_service_on_itself() {
        // A firewall whose page says "OPNsense" on a card already called opnsense would
        // otherwise gain a second card named opnsense-<hash> saying nothing new.
        List<Resource> cards = NetworkScanMapper.ToResources([
            Host(
                "192.0.2.101",
                identity: new ServiceIdentity("OPNsense.localdomain", IdentitySource.TlsCertificate, 443),
                services: [new ServiceIdentity("OPNsense", IdentitySource.Http, 80)])
        ]);

        Assert.Equal("opnsense", Assert.Single(cards.OfType<SystemResource>()).Name);
        Assert.Empty(cards.OfType<Service>());
    }

    [Fact]
    public void A_services_id_survives_a_rescan_and_differs_per_port() {
        NetworkHostFact host = Host(
            "192.0.2.105",
            "bc:24:11:00:1a:01",
            services: [
                new ServiceIdentity("Portainer", IdentitySource.Http, 9000),
                new ServiceIdentity("Grafana", IdentitySource.Http, 3000)
            ]);

        var first = NetworkScanMapper.ToResources([host]).OfType<Service>().Select(s => s.DiscoveryId).ToList();
        var second = NetworkScanMapper.ToResources([host]).OfType<Service>().Select(s => s.DiscoveryId).ToList();

        Assert.Equal(first, second);
        Assert.Equal(2, first.Distinct().Count());
    }

    [Fact]
    public void A_vendor_is_labelled_when_the_mac_is_known() {
        SystemResource card = Single(Host("192.0.2.64", "80:f3:da:00:1a:06", vendor: "Espressif"));

        Assert.Equal("Espressif", card.Labels["vendor"]);
        Assert.Equal("80:f3:da:00:1a:06", card.Labels["mac"]);
    }

    [Fact]
    public void No_vendor_label_is_invented_when_none_is_known() {
        SystemResource card = Single(Host("192.0.2.111", "00:00:00:11:22:33"));

        Assert.False(card.Labels.ContainsKey("vendor"));
    }

    [Fact]
    public void The_card_stays_sparse_whatever_identification_found() {
        // The sparseness contract: a scan sees an address, never an OS or a core count.
        // Anything written here would overwrite the real values on the next rescan of a
        // card an agent collector has since filled in.
        SystemResource card = Single(Host(
            "192.0.2.13",
            "bc:24:11:00:1a:01",
            identity: new ServiceIdentity("pve-node-01.example.com", IdentitySource.TlsCertificate, 8006),
            vendor: "Proxmox"));

        Assert.Null(card.Type);
        Assert.Null(card.Os);
        Assert.Null(card.Cores);
        Assert.Null(card.Ram);
        Assert.Equal("192.0.2.13", card.Ip);
    }

    [Fact]
    public void Identity_does_not_move_a_hosts_discovery_id() {
        // Identity is seeded on the MAC; a name learned from a service must not change
        // it, or every card would be recreated the day someone fixes a certificate.
        NetworkHostFact withoutName = Host("192.0.2.13", "bc:24:11:00:1a:01");
        NetworkHostFact withName = Host(
            "192.0.2.13",
            "bc:24:11:00:1a:01",
            identity: new ServiceIdentity("pve-node-01", IdentitySource.TlsCertificate, 8006));

        Assert.Equal(
            Single(withoutName).DiscoveryId,
            Single(withName).DiscoveryId);
    }

    [Fact]
    public void Two_hosts_naming_themselves_the_same_still_get_distinct_names() {
        // Every OPNsense VLAN gateway presents the same certificate CN.
        List<Resource> cards = NetworkScanMapper.ToResources([
            Host("192.0.2.101", identity: new ServiceIdentity("OPNsense.localdomain", IdentitySource.TlsCertificate, 443)),
            Host("192.0.2.141", identity: new ServiceIdentity("OPNsense.localdomain", IdentitySource.TlsCertificate, 443))
        ]);

        Assert.Equal(2, cards.Select(c => c.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
