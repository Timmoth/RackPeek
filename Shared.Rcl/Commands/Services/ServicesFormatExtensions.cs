namespace Shared.Rcl.Commands.Services;

public static class ServicesFormatExtensions
{
    public static string FormatRunsOn(string? runsOn, string? runsOnHost)
    {
        if (string.IsNullOrEmpty(runsOn) && string.IsNullOrEmpty(runsOnHost)) return "";

        if (string.IsNullOrEmpty(runsOn)) return runsOnHost!;

        if (string.IsNullOrEmpty(runsOnHost)) return runsOn!;

        return $"{runsOnHost}/{runsOn}";
    }
}