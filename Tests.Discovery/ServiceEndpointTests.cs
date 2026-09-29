using RackPeek.Domain.Resources.Services;

namespace Tests.Discovery;

/// <summary>
///     Where a service answers, and whether a browser can do anything with it.
///     <para>
///         Both of these were wrong in the UI. The endpoint was rendered as
///         <c>Ip: 10.0.50.105:3000</c> and then used as a link target, so clicking it did
///         nothing. And the scheme fell back to <c>http</c> for every port, so an SSH
///         service showed <c>http://10.0.50.105:22/</c> — a link that looks real, invites
///         a click and cannot load.
///     </para>
/// </summary>
public class ServiceEndpointTests {
    private static Network Net(int? port, string? protocol = "TCP", string? ip = "10.0.50.105", string? url = null) =>
        new() { Ip = ip, Port = port, Protocol = protocol, Url = url };

    private static Service Svc(Network? network) =>
        new() { Kind = Service.KindLabel, Name = "svc", Network = network };

    [Fact]
    public void An_endpoint_reads_as_an_address_and_nothing_else() =>
        // It goes on screen next to the service name; a label belongs in the markup.
        Assert.Equal("10.0.50.105:3000", Svc(Net(3000)).NetworkString());

    [Fact]
    public void An_endpoint_with_no_port_is_just_the_address() =>
        Assert.Equal("10.0.50.105", Svc(Net(null)).NetworkString());

    [Fact]
    public void A_service_with_no_network_has_no_endpoint() =>
        Assert.Equal(string.Empty, Svc(null).NetworkString());

    [Theory]
    [InlineData(22)] // ssh
    [InlineData(445)] // smb
    [InlineData(1883)] // mqtt
    [InlineData(53)] // dns
    [InlineData(3306)] // mysql
    [InlineData(9987)] // nothing curated
    public void A_port_a_browser_cannot_open_gets_no_link(int port) =>
        Assert.Null(Svc(Net(port)).BrowsableUrl());

    [Theory]
    [InlineData(80, "http://10.0.50.105/")]
    [InlineData(3000, "http://10.0.50.105:3000/")]
    [InlineData(8123, "http://10.0.50.105:8123/")]
    [InlineData(9000, "http://10.0.50.105:9000/")]
    public void A_web_port_gets_a_link(int port, string expected) =>
        Assert.Equal(expected, Svc(Net(port)).BrowsableUrl());

    [Theory]
    [InlineData(443, "https://10.0.50.105/")]
    [InlineData(8006, "https://10.0.50.105:8006/")]
    [InlineData(8443, "https://10.0.50.105:8443/")]
    public void A_tls_port_gets_an_https_link(int port, string expected) =>
        // Proxmox on 8006 redirects plain HTTP, so guessing http costs a round trip.
        Assert.Equal(expected, Svc(Net(port)).BrowsableUrl());

    [Fact]
    public void The_transport_is_not_mistaken_for_a_scheme() {
        // Discovery writes "TCP" into protocol, which says nothing about what rides on
        // top of it. Reading that as a scheme is what produced http:// on port 22.
        Assert.Null(Svc(Net(22, "TCP")).BrowsableUrl());
        Assert.Equal("http://10.0.50.105:3000/", Svc(Net(3000, "TCP")).BrowsableUrl());
    }

    [Theory]
    [InlineData("https", "https://10.0.50.105:9999/")]
    [InlineData("HTTP", "http://10.0.50.105:9999/")]
    public void A_protocol_that_names_a_scheme_is_believed(string protocol, string expected) =>
        // On an uncurated port this is the only thing that can answer.
        Assert.Equal(expected, Svc(Net(9999, protocol)).BrowsableUrl());

    [Fact]
    public void A_url_someone_typed_always_wins() {
        Service service = Svc(Net(22, "TCP", url: "https://git.example.com/"));

        Assert.Equal("https://git.example.com/", service.BrowsableUrl());
        Assert.Equal("https://git.example.com/", service.NetworkString());
    }

    [Fact]
    public void A_service_with_no_address_of_its_own_can_borrow_its_hosts() {
        // The card resolves the host's address when the service carries none.
        Service service = Svc(Net(8080, ip: null));

        Assert.Null(service.BrowsableUrl());
        Assert.Equal("http://10.0.20.7:8080/", service.BrowsableUrl("10.0.20.7"));
    }

    [Fact]
    public void A_service_with_no_port_is_not_guessed_at() =>
        Assert.Null(Svc(Net(null)).BrowsableUrl());

    [Theory]
    [InlineData("host name with spaces")]
    [InlineData("...")]
    [InlineData("a/b")]
    [InlineData("under_score.local")]
    [InlineData("fe80::1")]
    [InlineData("[::1]")]
    public void An_address_no_url_can_be_built_from_yields_no_link_rather_than_throwing(string ip) {
        // The address field holds whatever a person typed or a device reported, and
        // neither is obliged to produce something a URL can be built from. This is
        // rendered inside the hardware and system trees, so an exception here would
        // blank the whole page rather than spoil one link.
        Service service = Svc(Net(8080, ip: ip));

        Exception? thrown = Record.Exception(() => service.BrowsableUrl());

        Assert.Null(thrown);
    }
}
