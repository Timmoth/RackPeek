using RackPeek.Domain.Discovery;

namespace Tests.Discovery;

/// <summary>
///     The ARP table is where a scanned host's identity comes from, and the two
///     platforms print it differently — most dangerously, macOS drops leading zeros
///     from MAC octets. If normalisation slips, the same machine gets a different
///     discovery id depending on which workstation ran the scan.
/// </summary>
public class ArpTableParserTests {
    [Fact]
    public void The_linux_proc_file_parses_to_normalised_macs() {
        IReadOnlyDictionary<string, string> table = ArpTableParser.Parse(Fixture.Read("linux-arp-table"));

        Assert.Equal("a4:91:b1:4e:3c:20", table["192.168.1.1"]);
        // Uppercase in the fixture, stored lowercase.
        Assert.Equal("dc:a6:32:0f:11:22", table["192.168.1.20"]);
    }

    [Fact]
    public void The_macos_arp_output_parses_to_the_same_macs_as_linux() {
        IReadOnlyDictionary<string, string> linux = ArpTableParser.Parse(Fixture.Read("linux-arp-table"));
        IReadOnlyDictionary<string, string> macos = ArpTableParser.Parse(Fixture.Read("macos-arp-output"));

        // The macOS fixture prints 192.168.1.20 as dc:a6:32:f:11:22 — unpadded. Identity
        // must not depend on which of the two formats happened to report the machine.
        Assert.Equal(linux["192.168.1.1"], macos["192.168.1.1"]);
        Assert.Equal(linux["192.168.1.20"], macos["192.168.1.20"]);
    }

    [Fact]
    public void Unresolved_neighbours_contribute_nothing() {
        IReadOnlyDictionary<string, string> linux = ArpTableParser.Parse(Fixture.Read("linux-arp-table"));
        IReadOnlyDictionary<string, string> macos = ArpTableParser.Parse(Fixture.Read("macos-arp-output"));

        // Linux marks failures with flags 0x0 or an all-zero MAC; macOS prints "(incomplete)".
        Assert.False(linux.ContainsKey("192.168.1.50"));
        Assert.False(linux.ContainsKey("192.168.1.60"));
        Assert.False(macos.ContainsKey("192.168.1.50"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not an arp table at all")]
    [InlineData("IP address       HW type     Flags       HW address            Mask     Device")]
    [InlineData("? (garbage at nothing")]
    public void Garbage_input_is_an_empty_table_not_an_exception(string? text) =>
        Assert.Empty(ArpTableParser.Parse(text));

    [Theory]
    [InlineData("A4:91:B1:4E:3C:20", "a4:91:b1:4e:3c:20")]
    [InlineData("1:0:5e:0:0:fb", "01:00:5e:00:00:fb")]
    [InlineData("dc:a6:32:f:11:22", "dc:a6:32:0f:11:22")]
    public void Macs_normalise_to_lowercase_padded_octets(string raw, string expected) =>
        Assert.Equal(expected, ArpTableParser.NormaliseMac(raw));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("00:00:00:00:00:00")] // the kernel's "never answered"
    [InlineData("a4:91:b1:4e:3c")] // five octets
    [InlineData("a4:91:b1:4e:3c:20:ff")] // seven octets
    [InlineData("zz:91:b1:4e:3c:20")] // not hex
    [InlineData("(incomplete)")]
    public void Anything_that_is_not_a_usable_mac_is_null(string? raw) =>
        Assert.Null(ArpTableParser.NormaliseMac(raw));
}
