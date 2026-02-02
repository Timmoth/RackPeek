using System.ComponentModel.DataAnnotations;

namespace RackPeek.Domain.Helpers;

public static class ThrowIfInvalid
{
    public static void ResourceName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Name is required.");

        if (name.Length > 50) throw new ValidationException("Name is too long.");
    }

    public static void RamGb(int? value)
    {
        if (value is null) throw new ValidationException("RAM value must be specified.");

        if (value < 0) throw new ValidationException("RAM value must be a non negative number of gigabytes.");
    }

    #region Nics

    public static readonly string[] ValidNicTypes =
    {
        // Copper Ethernet
        "rj45",

        // SFP family
        "sfp", // 1G
        "sfp+", // 10G
        "sfp28", // 25G
        "sfp56", // 50G

        // QSFP family
        "qsfp+", // 40G
        "qsfp28", // 100G
        "qsfp56", // 200G
        "qsfp-dd", // 400G (QSFP Double Density)

        // OSFP (400G+)
        "osfp",

        // Legacy / niche but still seen
        "xfp", "cx4",

        // Management / special-purpose
        "mgmt" // Dedicated management NIC (IPMI/BMC)
    };

    public static void NicType(string nicType)
    {
        if (string.IsNullOrWhiteSpace(nicType)) throw new ValidationException("NIC type is required.");

        var normalized = nicType.Trim().ToLowerInvariant();

        if (ValidNicTypes.Contains(normalized)) return;

        var suggestions = GetNicTypeSuggestions(normalized).ToList();

        var message = suggestions.Any()
            ? $"NIC type '{nicType}' is not valid. Did you mean: {string.Join(", ", suggestions)}?"
            : $"NIC type '{nicType}' is not valid. Valid NIC types include rj45, sfp, sfp+, qsfp+ etc";

        throw new ValidationException(message);
    }

    private static IEnumerable<string> GetNicTypeSuggestions(string input)
    {
        return ValidNicTypes.Select(type => new { Type = type, Score = SimilarityScore(input, type) })
            .Where(x => x.Score >= 0.5)
            .OrderByDescending(x => x.Score)
            .Take(3)
            .Select(x => x.Type);
    }

    private static double SimilarityScore(string a, string b)
    {
        if (a == b) return 1.0;

        if (b.StartsWith(a) || a.StartsWith(b)) return 0.9;

        var commonChars = a.Intersect(b).Count();
        return (double)commonChars / Math.Max(a.Length, b.Length);
    }

    public static void NicSpeed(int speed)
    {
        if (speed < 0) throw new ValidationException("NIC speed must be a non negative number of gigabits per second.");
    }

    public static void NicPorts(int ports)
    {
        if (ports < 0) throw new ValidationException("NIC port count must be a non negative integer.");
    }

    #endregion

    #region Drives

    public static readonly string[] ValidDriveTypes =
    {
        // Flash storage
        "nvme", "ssd",
        // Traditional spinning disks
        "hdd",
        // Enterprise interfaces
        "sas", "sata",
        // External / removable
        "usb", "sdcard", "micro-sd"
    };

    public static void DriveType(string driveType)
    {
        if (string.IsNullOrWhiteSpace(driveType)) throw new ValidationException("Drive type is required.");

        var normalized = driveType.Trim().ToLowerInvariant();

        if (ValidDriveTypes.Contains(normalized)) return;

        var suggestions = GetDriveTypeSuggestions(normalized).ToList();

        var message = suggestions.Any()
            ? $"Drive type '{driveType}' is not valid. Did you mean: {string.Join(", ", suggestions)}?"
            : $"Drive type '{driveType}' is not valid. Valid Drive types include nvme, ssd, hdd, sata, sas etc.";

        throw new ValidationException(message);
    }

    private static IEnumerable<string> GetDriveTypeSuggestions(string input)
    {
        return ValidDriveTypes.Select(type => new { Type = type, Score = SimilarityScore(input, type) })
            .Where(x => x.Score >= 0.5)
            .OrderByDescending(x => x.Score)
            .Take(3)
            .Select(x => x.Type);
    }

    public static void DriveSize(int size)
    {
        if (size < 0) throw new ValidationException("Drive size value must be a non negative number of gigabytes.");
    }

    #endregion
}