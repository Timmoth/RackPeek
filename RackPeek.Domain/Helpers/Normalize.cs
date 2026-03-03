namespace RackPeek.Domain.Helpers;

public static class Normalize {
    public static string DriveType(string value) => value.Trim().ToLowerInvariant();

    public static string NicType(string value) => value.Trim().ToLowerInvariant();

    public static string SystemType(string value) => value.Trim().ToLowerInvariant();

    public static string SystemName(string name) => name.Trim();

    public static string ServiceName(string name) => name.Trim();

    public static string HardwareName(string name) => name.Trim();

    public static string ResourceName(string name) => name.Trim();

    public static string Tag(string name) => name.Trim();

    public static string LabelKey(string key) => key.Trim();

    public static string LabelValue(string value) => value.Trim();
}
