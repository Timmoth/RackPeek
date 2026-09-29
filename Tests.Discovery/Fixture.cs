using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Json.Schema;
using YamlDotNet.RepresentationModel;

namespace Tests.Discovery;

/// <summary>
///     Captured output from real machines. Reading these rather than the host is what
///     lets one set of tests run unchanged on Linux, macOS and Windows.
/// </summary>
public static class Fixture {
    // JsonSchema.Net keeps a process-wide registry keyed on $id, so loading the same
    // schema from two test classes at once races. Load each one exactly once.
    private static readonly ConcurrentDictionary<int, Lazy<JsonSchema>> _schemas = new();

    public static string Read(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    /// <summary>
    ///     Asserts a discovery document satisfies the published RackPeek schema, so the
    ///     collectors cannot drift away from the contract the rest of the world imports.
    /// </summary>
    public static void AssertConformsToSchema(string yaml, int version = 4) {
        JsonSchema schema = _schemas.GetOrAdd(version, v => new Lazy<JsonSchema>(() =>
            JsonSchema.FromText(
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "schemas", $"schema.v{v}.json"))),
            LazyThreadSafetyMode.ExecutionAndPublication)).Value;

        EvaluationResults results = schema.Evaluate(
            ToJson(yaml),
            new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });

        if (results.IsValid)
            return;

        var errors = new List<string>();
        Collect(results, errors);

        Assert.Fail($"Discovery output does not match schema v{version}:{Environment.NewLine}"
                    + string.Join(Environment.NewLine, errors.Distinct())
                    + Environment.NewLine + Environment.NewLine + yaml);
    }

    private static void Collect(EvaluationResults node, List<string> errors) {
        if (node.Errors != null)
            foreach (KeyValuePair<string, string> error in node.Errors)
                errors.Add($"{node.InstanceLocation}: {error.Value}");

        if (node.Details != null)
            foreach (EvaluationResults child in node.Details)
                Collect(child, errors);
    }

    private static JsonElement ToJson(string yaml) {
        var stream = new YamlStream();
        stream.Load(new StringReader(yaml));

        using var document = JsonDocument.Parse(Convert(stream.Documents[0].RootNode));

        return document.RootElement.Clone();
    }

    private static string Convert(YamlNode node) {
        switch (node) {
            case YamlScalarNode scalar:
                if (scalar.Style is YamlDotNet.Core.ScalarStyle.SingleQuoted
                    or YamlDotNet.Core.ScalarStyle.DoubleQuoted)
                    return JsonSerializer.Serialize(scalar.Value);

                if (int.TryParse(scalar.Value, out var i))
                    return i.ToString();

                if (double.TryParse(scalar.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                    return d.ToString(CultureInfo.InvariantCulture);

                if (bool.TryParse(scalar.Value, out var b))
                    return b.ToString().ToLowerInvariant();

                return JsonSerializer.Serialize(scalar.Value);

            case YamlSequenceNode sequence:
                return "[" + string.Join(",", sequence.Children.Select(Convert)) + "]";

            case YamlMappingNode mapping:
                return "{" + string.Join(",", mapping.Children.Select(kvp =>
                    JsonSerializer.Serialize(((YamlScalarNode)kvp.Key).Value) + ":" + Convert(kvp.Value))) + "}";

            default:
                return "null";
        }
    }
}
