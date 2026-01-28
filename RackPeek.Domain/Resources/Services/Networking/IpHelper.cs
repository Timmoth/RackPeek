namespace RackPeek.Domain.Resources.Services.Networking;

public static class IpHelper
{
    public static uint ToUInt32(string ip)
    {
        var parts = ip.Split('.');
        if (parts.Length != 4)
            throw new ArgumentException($"Invalid IPv4 address: {ip}");

        return (uint)(
            (int.Parse(parts[0]) << 24) |
            (int.Parse(parts[1]) << 16) |
            (int.Parse(parts[2]) << 8) |
            int.Parse(parts[3]));
    }

    public static string ToIp(uint ip)
    {
        return string.Join('.',
            (ip >> 24) & 0xFF,
            (ip >> 16) & 0xFF,
            (ip >> 8) & 0xFF,
            ip & 0xFF);
    }

    public static uint MaskFromPrefix(int prefix)
    {
        if (prefix < 0 || prefix > 32)
            throw new ArgumentException($"Invalid CIDR prefix: {prefix}");

        return prefix == 0 ? 0 : uint.MaxValue << (32 - prefix);
    }
}