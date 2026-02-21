using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Resources.SubResources;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Helpers;

public static class ThrowIfInvalid
{
    public static void ResourceName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Name is required.");

        if (name.Length > 50) throw new ValidationException("Name is too long.");
    }

    public static void AccessPointModelName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Model name is required.");

        if (name.Length > 50)
            throw new ValidationException("Model name is too long.");
    }

    public static void RamGb(double? value)
    {
        if (value is null) throw new ValidationException("RAM value must be specified.");

        if (value < 0) throw new ValidationException("RAM value must be a non negative number of gigabytes.");
    }

    public static void SystemType(string systemType)
    {
        if (string.IsNullOrWhiteSpace(systemType)) throw new ValidationException("System type is required.");

        var normalized = systemType.Trim().ToLowerInvariant();

        if (SystemResource.ValidSystemTypes.Contains(normalized)) return;

        var suggestions = GetSystemTypeSuggestions(normalized).ToList();

        var message = suggestions.Any()
            ? $"System type '{systemType}' is not valid. Did you mean: {string.Join(", ", suggestions)}?"
            : $"System type '{systemType}' is not valid. Valid System types include hypervisor, baremetal vm, container, other etc";

        throw new ValidationException(message);
    }

    private static IEnumerable<string> GetSystemTypeSuggestions(string input)
    {
        return SystemResource.ValidSystemTypes.Select(type => new { Type = type, Score = SimilarityScore(input, type) })
            .Where(x => x.Score >= 0.5)
            .OrderByDescending(x => x.Score)
            .Take(3)
            .Select(x => x.Type);
    }

    #region Nics

    public static void NicType(string nicType)
    {
        if (string.IsNullOrWhiteSpace(nicType)) throw new ValidationException("NIC type is required.");

        var normalized = nicType.Trim().ToLowerInvariant();

        if (Nic.ValidNicTypes.Contains(normalized)) return;

        var suggestions = GetNicTypeSuggestions(normalized).ToList();

        var message = suggestions.Any()
            ? $"NIC type '{nicType}' is not valid. Did you mean: {string.Join(", ", suggestions)}?"
            : $"NIC type '{nicType}' is not valid. Valid NIC types include rj45, sfp, sfp+, qsfp+ etc";

        throw new ValidationException(message);
    }

    private static IEnumerable<string> GetNicTypeSuggestions(string input)
    {
        return Nic.ValidNicTypes.Select(type => new { Type = type, Score = SimilarityScore(input, type) })
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

    public static void NicSpeed(double speed)
    {
        if (speed < 0) throw new ValidationException("NIC speed must be a non negative number of gigabits per second.");
    }

    public static void NetworkSpeed(double speed)
    {
        if (speed < 0)
            throw new ValidationException(
                "Network speed must be a non negative number of gigabits per second.");
    }


    public static void NicPorts(int ports)
    {
        if (ports < 0) throw new ValidationException("NIC port count must be a non negative integer.");
    }

    #endregion

    #region Drives

    public static void DriveType(string driveType)
    {
        if (string.IsNullOrWhiteSpace(driveType)) throw new ValidationException("Drive type is required.");

        var normalized = driveType.Trim().ToLowerInvariant();

        if (Drive.ValidDriveTypes.Contains(normalized)) return;

        var suggestions = GetDriveTypeSuggestions(normalized).ToList();

        var message = suggestions.Any()
            ? $"Drive type '{driveType}' is not valid. Did you mean: {string.Join(", ", suggestions)}?"
            : $"Drive type '{driveType}' is not valid. Valid Drive types include nvme, ssd, hdd, sata, sas etc.";

        throw new ValidationException(message);
    }

    private static IEnumerable<string> GetDriveTypeSuggestions(string input)
    {
        return Drive.ValidDriveTypes.Select(type => new { Type = type, Score = SimilarityScore(input, type) })
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