using RackPeek.Domain.Discovery;

namespace Tests.Discovery;

/// <summary>
///     Proxmox's perl backend quotes numeric fields inconsistently — <c>"vmid":"100"</c>
///     and <c>"maxdisk":"512110190592"</c> both turn up across versions. The
///     <c>TryGetInt32</c> family does not return false for a string, it throws, so a
///     single quoted number used to take the whole run down with a stack trace: the CLI
///     printed "Unexpected error occurred" and MCP forwarded the BCL's
///     "requires an element of type 'Number'" as if it were the user's fault.
/// </summary>
public class ProxmoxQuotedNumberTests {
    [Fact]
    public void A_guest_whose_numbers_are_quoted_is_read_rather_than_throwing() {
        var json = """
                   {"data":[{"vmid":"100","name":"quoted-guest","cpus":"4",
                   "maxmem":"4294967296","maxdisk":"512110190592","status":"running"}]}
                   """;

        ProxmoxGuest guest = Assert.Single(ProxmoxResponseParser.ParseGuests(json, "pve01", "vm"));

        Assert.Equal(100, guest.VmId);
        Assert.Equal("quoted-guest", guest.Name);
        Assert.Equal(4, guest.Cores);
        Assert.Equal(4294967296, guest.MemoryBytes);
        Assert.Equal(512110190592, guest.DiskBytes);
    }

    [Fact]
    public void Plain_numbers_still_read_the_same_way() {
        var json = """
                   {"data":[{"vmid":101,"name":"plain-guest","cpus":2,
                   "maxmem":2147483648,"maxdisk":34359738368,"status":"running"}]}
                   """;

        ProxmoxGuest guest = Assert.Single(ProxmoxResponseParser.ParseGuests(json, "pve01", "vm"));

        Assert.Equal(101, guest.VmId);
        Assert.Equal(2, guest.Cores);
        Assert.Equal(2147483648, guest.MemoryBytes);
    }

    [Fact]
    public void A_field_that_is_neither_a_number_nor_a_numeric_string_is_simply_absent() {
        // Guessing at "N/A" would be worse than leaving the field empty, and it must
        // still not throw.
        var json = """{"data":[{"vmid":102,"name":"odd-guest","cpus":"N/A","maxmem":null}]}""";

        ProxmoxGuest guest = Assert.Single(ProxmoxResponseParser.ParseGuests(json, "pve01", "vm"));

        Assert.Equal(0, guest.Cores);
        Assert.Equal(0, guest.MemoryBytes);
    }

    [Fact]
    public void A_guest_whose_vmid_is_quoted_is_still_identified() {
        // vmid is the guest's identity; dropping it would silently lose the guest.
        var json = """{"data":[{"vmid":"103","name":"id-guest"}]}""";

        Assert.Equal(103, Assert.Single(ProxmoxResponseParser.ParseGuests(json, "pve01", "vm")).VmId);
    }
}
