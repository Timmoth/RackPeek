namespace RackPeek.Domain.Helpers;

public static class Normalize
{
    public static string NicType(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}