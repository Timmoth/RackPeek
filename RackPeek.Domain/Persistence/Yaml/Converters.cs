using System.Globalization;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace RackPeek.Domain.Persistence.Yaml;

public static class StorageSizeParser
{
    private static readonly Regex SizeRegex = new(@"^\s*(\d+(?:\.\d+)?)\s*(gb|tb)?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static double ParseToGbDouble(string input)
    {
        var match = SizeRegex.Match(input);
        if (!match.Success) throw new FormatException($"Invalid storage size: '{input}'");
        var value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var unit = match.Groups[2].Value.ToLowerInvariant();
        return unit switch
        {
            "tb" => value * 1024, "gb" or "" => value, _ => throw new FormatException($"Unknown unit in '{input}'")
        };
    }
}

public class StorageSizeYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(int) ||
               type == typeof(int?) ||
               type == typeof(double) ||
               type == typeof(double?);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();
        var value = scalar.Value;

        if (string.IsNullOrWhiteSpace(value))
            return null;

        // If it's already a number, parse directly
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericDouble))
        {
            if (type == typeof(double) || type == typeof(double?))
                return numericDouble;

            if (type == typeof(int) || type == typeof(int?))
                return (int)numericDouble;
        }

        // Otherwise parse with size parser
        var gb = StorageSizeParser.ParseToGbDouble(value);

        if (type == typeof(double) || type == typeof(double?))
            return gb;

        return (int)Math.Round(gb);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        emitter.Emit(new Scalar(value?.ToString() ?? string.Empty));
    }
}