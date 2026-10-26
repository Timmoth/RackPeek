namespace RackPeek.Domain.Discovery;

/// <summary>
///     Turns a MAC address into the organisation IEEE assigned its OUI to — the only
///     identity a silent device on the wire ever volunteers. A camera that answers no
///     port and has no PTR record is still recognisably an Espressif or a Ubiquiti.
///     Pure: the table is generated (see <see cref="MacVendorTable" />), never fetched.
/// </summary>
public static class MacVendorLookup {
    /// <summary>
    ///     True when the address was made up by the device rather than assigned by a
    ///     manufacturer — the locally-administered bit is set. Modern phones and laptops
    ///     randomise per network for privacy, so these carry no vendor at all, and the
    ///     OUI half is meaningless rather than merely unknown. Saying so is more useful
    ///     than reporting whichever company happens to own the matching block.
    /// </summary>
    public static bool IsLocallyAdministered(string? mac) {
        var octet = FirstOctet(mac);

        return octet >= 0 && (octet & 0x02) != 0;
    }

    /// <summary>
    ///     The assigned vendor, "Randomised (locally administered)" for a self-assigned
    ///     address, or null when the OUI is not in the curated table. Null means "we do
    ///     not know", never "no vendor".
    /// </summary>
    public static string? Lookup(string? mac) {
        var prefix = NormalisePrefix(mac);

        if (prefix == null)
            return null;

        var index = IndexOf(prefix);

        // The table is consulted before the locally-administered check on purpose:
        // hypervisors mint guest addresses out of that range (KVM's 52:54:00), so the
        // bit alone would report a VM as anonymous when its prefix names the emulator.
        if (index < 0)
            return IsLocallyAdministered(mac)
                ? "Randomised (locally administered)"
                : null;

        var vendorIndex = MacVendorTable.Records[index + 6] - MacVendorTable.FirstIndexChar;

        return vendorIndex >= 0 && vendorIndex < MacVendorTable.Vendors.Length
            ? MacVendorTable.Vendors[vendorIndex]
            : null;
    }

    /// <summary>The first six hex digits, upper-cased, or null when that cannot be read.</summary>
    private static string? NormalisePrefix(string? mac) {
        if (string.IsNullOrWhiteSpace(mac))
            return null;

        Span<char> digits = stackalloc char[6];
        var count = 0;

        foreach (var c in mac) {
            if (!Uri.IsHexDigit(c))
                continue;

            digits[count++] = char.ToUpperInvariant(c);

            if (count == 6)
                return new string(digits);
        }

        return null;
    }

    private static int FirstOctet(string? mac) {
        var prefix = NormalisePrefix(mac);

        return prefix == null
            ? -1
            : Convert.ToInt32(prefix[..2], 16);
    }

    /// <summary>
    ///     Binary search over the packed records. Returns the index of the matching
    ///     record's first char, or -1.
    /// </summary>
    private static int IndexOf(string prefix) {
        var records = MacVendorTable.Records;
        var low = 0;
        var high = records.Length / MacVendorTable.RecordLength - 1;

        while (low <= high) {
            var mid = (low + high) / 2;
            var at = mid * MacVendorTable.RecordLength;

            var comparison = string.CompareOrdinal(records, at, prefix, 0, 6);

            if (comparison == 0)
                return at;

            if (comparison < 0)
                low = mid + 1;
            else
                high = mid - 1;
        }

        return -1;
    }
}
