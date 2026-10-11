using RackPeek.Domain.Discovery;

namespace Tests.Discovery;

/// <summary>
///     The vendor a MAC implies. Every expectation here is a real IEEE assignment, and
///     several are addresses observed on a live homelab — a lookup that silently drifted
///     would be worse than no lookup, because a confident wrong vendor is unfalsifiable
///     to whoever reads the card.
/// </summary>
public class MacVendorLookupTests {
    [Theory]
    [InlineData("bc:24:11:00:1a:01", "Proxmox")]
    [InlineData("a0:36:9f:00:1a:02", "Intel")]
    [InlineData("1c:6a:1b:00:1a:03", "Ubiquiti")]
    [InlineData("48:b0:2d:00:1a:04", "NVIDIA")]
    [InlineData("80:f3:da:00:1a:06", "Espressif")]
    [InlineData("b8:27:eb:11:22:33", "Raspberry Pi")]
    [InlineData("00:0c:29:aa:bb:cc", "VMware")]
    public void Assigned_prefixes_resolve_to_their_owner(string mac, string expected) =>
        Assert.Equal(expected, MacVendorLookup.Lookup(mac));

    [Theory]
    [InlineData("BC:24:11:14:59:DF")]
    [InlineData("bc-24-11-14-59-df")]
    [InlineData("bc2411145 9df")]
    [InlineData("bc24.1114.59df")]
    public void Separators_and_case_do_not_matter(string mac) =>
        Assert.Equal("Proxmox", MacVendorLookup.Lookup(mac));

    [Fact]
    // KVM mints guest addresses from the locally-administered range rather than buying
    // an OUI. Reporting "randomised" here would hide the most useful fact about the
    // host — that it is a virtual machine.
    public void A_hypervisors_own_prefix_beats_the_locally_administered_bit() =>
        Assert.Equal("QEMU/KVM", MacVendorLookup.Lookup("52:54:00:12:34:56"));

    [Theory]
    [InlineData("92:16:01:00:1a:07")]
    [InlineData("6a:34:ce:00:1a:08")]
    public void Self_assigned_addresses_are_reported_as_randomised(string mac) {
        // A phone or laptop randomising per network. The OUI half names no one, so
        // saying so beats reporting whichever company owns the matching block.
        Assert.Equal("Randomised (locally administered)", MacVendorLookup.Lookup(mac));
        Assert.True(MacVendorLookup.IsLocallyAdministered(mac));
    }

    [Fact]
    public void A_universally_administered_address_is_not_flagged_as_randomised() =>
        Assert.False(MacVendorLookup.IsLocallyAdministered("bc:24:11:00:1a:01"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-mac")]
    [InlineData("bc:24")]
    public void Unreadable_input_yields_no_vendor(string? mac) {
        Assert.Null(MacVendorLookup.Lookup(mac));
        Assert.False(MacVendorLookup.IsLocallyAdministered(mac));
    }

    [Fact]
    // 00:00:00 is assigned to Xerox and deliberately not in the curated table. Null
    // means "not known", which is honest; a nearest-match would not be.
    public void An_unlisted_but_assigned_prefix_is_null_rather_than_a_guess() =>
        Assert.Null(MacVendorLookup.Lookup("00:00:00:11:22:33"));

    [Fact]
    public void Every_packed_record_resolves_to_a_real_vendor_name() {
        // Guards the generated table's index encoding: an off-by-one in the index char
        // would map prefixes to the wrong vendor, or off the end of the array. Also
        // proves the records are sorted, which the binary search depends on.
        var records = MacVendorTable.Records;
        var count = records.Length / MacVendorTable.RecordLength;

        Assert.True(count > 1000, $"The table holds only {count} records — did generation fail?");

        string? previous = null;

        for (var i = 0; i < count; i++) {
            var prefix = records.Substring(i * MacVendorTable.RecordLength, 6);

            if (previous != null)
                Assert.True(
                    string.CompareOrdinal(previous, prefix) < 0,
                    $"Records are not sorted ascending: {previous} precedes {prefix}.");

            previous = prefix;

            Assert.False(
                string.IsNullOrWhiteSpace(MacVendorLookup.Lookup(prefix + "000000")),
                $"{prefix} resolved to nothing.");
        }
    }
}
