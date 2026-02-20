namespace RackPeek.Domain.Helpers;

public static class Normalize
{
    public static string DriveType(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    public static string NicType(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    public static string SystemType(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    public static string SystemName(string name)
    {
        return name.Trim();
    }

    public static string ServiceName(string name)
    {
        return name.Trim();
    }

    public static string HardwareName(string name)
    {
        return name.Trim();
    }

    public static string ResourceName(string name)
    {
        return name.Trim();
    }

    public static string Tag(string name)
    {
        return name.Trim();
    }
}