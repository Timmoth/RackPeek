using RackPeek.Domain.Discovery;

namespace Tests.Discovery;

/// <summary>
///     Reading a host's name out of what its services volunteer. The fixtures follow the
///     shape of what real homelab gear emits — a Proxmox node, an OPNsense firewall, a
///     network printer — because the value of this parser is entirely in what it makes of
///     awkward real-world output rather than well-formed examples. The names and
///     addresses throughout are invented.
/// </summary>
public class ServiceIdentityParserTests {
    [Theory]
    // Proxmox ships a per-node certificate naming the node. This single line is what
    // turns "host-1a2b3c4d" into "pve-node-01".
    [InlineData("OU=PVE Cluster Node, O=Proxmox Virtual Environment, CN=pve-node-01.example.com", "pve-node-01.example.com")]
    [InlineData("OU=PVE Cluster Node, O=Proxmox Virtual Environment, CN=pve-node-02.example", "pve-node-02.example")]
    [InlineData("CN=OPNsense.localdomain, O=OPNsense self-signed", "OPNsense.localdomain")]
    [InlineData("CN=printer-lobby.local", "printer-lobby.local")]
    [InlineData("CN = spaced.example.lan, O = Org", "spaced.example.lan")]
    public void A_certificate_common_name_is_read_from_its_subject(string subject, string expected) =>
        Assert.Equal(expected, ServiceIdentityParser.ParseTlsSubject(subject));

    [Theory]
    [InlineData("CN=*.example.com, O=Org")] // names a domain, not this machine
    [InlineData("CN=localhost")] // the installer's placeholder
    [InlineData("CN=-6268337161588699610")] // an appliance serial: no letters, names nothing
    [InlineData("O=Org Only, OU=No Common Name")]
    [InlineData("")]
    [InlineData(null)]
    public void A_subject_that_names_nothing_useful_is_rejected(string? subject) =>
        Assert.Null(ServiceIdentityParser.ParseTlsSubject(subject));

    [Theory]
    [InlineData("SSH-2.0-OpenSSH_9.6p1 Ubuntu-3ubuntu13.5", "OpenSSH")]
    [InlineData("SSH-2.0-dropbear", "dropbear")]
    [InlineData("SSH-2.0-dropbear_2022.83", "dropbear")]
    [InlineData("SSH-1.99-Cisco-1.25", "Cisco")]
    // The version is deliberately dropped: it goes stale on the next patch, and a
    // resource named after a point release is worse than one named after the daemon.
    public void An_ssh_greeting_yields_the_software_without_its_version(string banner, string expected) =>
        Assert.Equal(expected, ServiceIdentityParser.ParseSshBanner(banner));

    [Theory]
    [InlineData("220 mail.example.com ESMTP Postfix")] // not SSH
    [InlineData("HTTP/1.1 200 OK")]
    [InlineData("")]
    [InlineData(null)]
    public void Something_that_is_not_an_ssh_greeting_yields_nothing(string? banner) =>
        Assert.Null(ServiceIdentityParser.ParseSshBanner(banner));

    [Fact]
    public void A_page_title_is_preferred_over_the_server_header() {
        // "Home Assistant" identifies the box; "nginx" identifies half the internet.
        var head = "HTTP/1.0 200 OK\r\nServer: nginx/1.24.0\r\n\r\n<html><head><title>Home Assistant</title>";

        Assert.Equal("Home Assistant", ServiceIdentityParser.ParseHttpIdentity(head));
    }

    [Fact]
    public void The_server_header_is_used_when_there_is_no_title() {
        var head = "HTTP/1.0 200 OK\r\nServer: llama.cpp\r\nContent-Type: text/html\r\n\r\n<html><body>";

        Assert.Equal("llama.cpp", ServiceIdentityParser.ParseHttpIdentity(head));
    }

    [Fact]
    public void A_server_headers_version_is_trimmed() {
        var head = "HTTP/1.0 200 OK\r\nServer: nginx/1.24.0 (Ubuntu)\r\n\r\n";

        Assert.Equal("nginx", ServiceIdentityParser.ParseHttpIdentity(head));
    }

    [Theory]
    // Observed on a live sweep: the tagline became the card's name.
    [InlineData("<title>Forgejo: Beyond coding. We Forge.</title>", "Forgejo")]
    [InlineData("<title>Grafana | Dashboards</title>", "Grafana")]
    [InlineData("<title>Jellyfin – Media</title>", "Jellyfin")]
    [InlineData("<title>Home Assistant</title>", "Home Assistant")]
    public void A_title_is_trimmed_to_the_phrase_before_its_separator(string body, string expected) =>
        Assert.Equal(expected, ServiceIdentityParser.ParseHttpIdentity("HTTP/1.0 200 OK\r\n\r\n" + body));

    [Fact]
    public void A_title_whose_lead_is_too_short_to_stand_alone_is_kept_whole() {
        // A stray separator near the start is not a product name.
        var head = "HTTP/1.0 200 OK\r\n\r\n<title>my: little server</title>";

        Assert.Equal("my: little server", ServiceIdentityParser.ParseHttpIdentity(head));
    }

    [Fact]
    public void Whitespace_in_a_title_is_collapsed() {
        var head = "HTTP/1.0 200 OK\r\n\r\n<title>\n   Home    Assistant\n</title>";

        Assert.Equal("Home Assistant", ServiceIdentityParser.ParseHttpIdentity(head));
    }

    [Theory]
    [InlineData("HTTP/1.0 401 Unauthorized\r\nWWW-Authenticate: Basic\r\n\r\n")]
    [InlineData("HTTP/1.0 200 OK\r\n\r\n<title>   </title>")]
    [InlineData("HTTP/1.0 200 OK\r\n\r\n<title>404</title>")] // digits name nothing
    [InlineData("")]
    [InlineData(null)]
    public void An_http_response_that_names_nothing_yields_nothing(string? head) =>
        Assert.Null(ServiceIdentityParser.ParseHttpIdentity(head));

    [Fact]
    public void An_error_pages_title_is_not_a_name() {
        // Observed on a live sweep: a host answering 400 produced a card called
        // "http-status-400-bad-request".
        var head = "HTTP/1.1 400 Bad Request\r\nServer: Apache\r\n\r\n"
                   + "<title>HTTP Status 400 – Bad Request</title>";

        // The Server header is still accurate on an error response, so it is used.
        Assert.Equal("Apache", ServiceIdentityParser.ParseHttpIdentity(head));
    }

    [Fact]
    public void An_error_response_with_no_server_header_names_nothing() {
        var head = "HTTP/1.1 500 Internal Server Error\r\n\r\n<title>Something broke</title>";

        Assert.Null(ServiceIdentityParser.ParseHttpIdentity(head));
    }

    [Fact]
    public void A_redirect_still_counts_as_a_working_page() {
        var head = "HTTP/1.1 302 Found\r\nLocation: /ui\r\n\r\n<title>Home Assistant</title>";

        Assert.Equal("Home Assistant", ServiceIdentityParser.ParseHttpIdentity(head));
    }

    [Fact]
    public void An_absurdly_long_title_is_rejected_rather_than_becoming_a_name() {
        var head = $"HTTP/1.0 200 OK\r\n\r\n<title>{new string('x', 300)}</title>";

        Assert.Null(ServiceIdentityParser.ParseHttpIdentity(head));
    }
}
