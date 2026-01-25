namespace RackPeek.Domain.Resources.Hardware.Parsing;

public class UnitParser
{
    public static double ParseGbValue(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0;
        raw = raw.ToLower().Trim();
        if (raw.EndsWith("gb")) raw = raw.Replace("gb", "");
        return double.TryParse(raw, out var value) ? value : 0;
    }
}