namespace RackPeek.Domain.Resources.Services.Networking;

public static class IpHelper {
    public static uint ToUInt32(string ip) {
        var parts = ip.Split('.');
        if (parts.Length != 4)
            throw new ArgumentException($"Invalid IPv4 address: {ip}");

        uint result = 0;

        foreach (var part in parts) {
            // Range-checked: unchecked shifts would fold 192.168.256.0 into
            // 192.169.0.0 and quietly point a caller at the wrong network.
            if (!int.TryParse(part, out var octet) || octet is < 0 or > 255)
                throw new ArgumentException($"Invalid IPv4 address: {ip}");

            result = (result << 8) | (uint)octet;
        }

        return result;
    }

    public static string ToIp(uint ip) {
        return string.Join('.',
            (ip >> 24) & 0xFF,
            (ip >> 16) & 0xFF,
            (ip >> 8) & 0xFF,
            ip & 0xFF);
    }

    public static uint MaskFromPrefix(int prefix) {
        if (prefix < 0 || prefix > 32)
            throw new ArgumentException($"Invalid CIDR prefix: {prefix}");

        return prefix == 0 ? 0 : uint.MaxValue << (32 - prefix);
    }
}
