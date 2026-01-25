using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RackPeek;

public static class StorageSizeParser
{
    private static readonly Regex SizeRegex =
        new(@"^\s*(\d+(?:\.\d+)?)\s*(gb|tb)?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static int ParseToGb(string input)
    {
        var match = SizeRegex.Match(input);

        if (!match.Success)
            throw new FormatException($"Invalid storage size: '{input}'");

        var value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var unit = match.Groups[2].Value.ToLowerInvariant();

        return unit switch
        {
            "tb" => (int)Math.Round(value * 1024),
            "gb" or "" => (int)Math.Round(value),
            _ => throw new FormatException($"Unknown unit in '{input}'")
        };
    }
}

public class StorageSizeYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) =>
        type == typeof(int) || type == typeof(int?);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        var value = scalar.Value;

        if (string.IsNullOrWhiteSpace(value))
            return null;

        // If it's already a number, just parse it
        if (int.TryParse(value, out var numeric))
            return numeric;

        // Otherwise try size parsing
        return StorageSizeParser.ParseToGb(value);
        
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        emitter.Emit(new Scalar(value?.ToString() ?? string.Empty));
    }
}
