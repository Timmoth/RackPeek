using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using YamlDotNet.RepresentationModel;

namespace Tests.Yaml;

public class SchemaConformanceTests {
    private static JsonSchema LoadSchema(int version) {
        var schemaText = File.ReadAllText($"schemas/schema.v{version}.json");
        return JsonSchema.FromText(schemaText);
    }

    private static JsonElement ConvertYamlToJsonElement(string yaml) {
        // Load YAML into YAML DOM
        var yamlStream = new YamlStream();
        yamlStream.Load(new StringReader(yaml));

        YamlNode root = yamlStream.Documents[0].RootNode;

        // Convert YAML node → JSON string
        var json = ConvertYamlNodeToJson(root);

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static string ConvertYamlNodeToJson(YamlNode node) {
        if (node is YamlScalarNode scalar) {
            // Try numeric
            if (int.TryParse(scalar.Value, out var i))
                return i.ToString();

            if (double.TryParse(scalar.Value, out var d))
                return d.ToString(CultureInfo.InvariantCulture);

            if (bool.TryParse(scalar.Value, out var b))
                return b.ToString().ToLowerInvariant();

            // Otherwise string
            return JsonSerializer.Serialize(scalar.Value);
        }

        if (node is YamlSequenceNode sequence) {
            IEnumerable<string> items = sequence.Children
                .Select(ConvertYamlNodeToJson);

            return "[" + string.Join(",", items) + "]";
        }

        if (node is YamlMappingNode mapping) {
            IEnumerable<string> props = mapping.Children
                .Select(kvp =>
                    JsonSerializer.Serialize(((YamlScalarNode)kvp.Key).Value)
                    + ":"
                    + ConvertYamlNodeToJson(kvp.Value));

            return "{" + string.Join(",", props) + "}";
        }

        return "null";
    }

    /// <summary>
    ///     The schema is published three times: the repo root copy tests validate
    ///     against, and the copies the web app and the viewer serve at
    ///     /schemas/v{n}/schema.v{n}.json. They are hand-synced, and #310/#311 were
    ///     what happens when a sync is missed — this pins them together for good.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void The_served_schema_copies_never_drift_from_the_published_one(int version) {
        var published = JsonNode.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "schemas", $"schema.v{version}.json")));

        foreach (var host in new[] { "web", "viewer" }) {
            var served = JsonNode.Parse(File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "wwwroot-schemas", host, $"schema.v{version}.json")));

            Assert.True(
                JsonNode.DeepEquals(published, served),
                $"The {host} wwwroot copy of schema.v{version}.json differs from schemas/ — " +
                "update both together, or documents RackPeek writes will fail the schema it serves.");
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void All_yaml_files_conform_to_schema(int version) {
        // Arrange
        JsonSchema schema = LoadSchema(version);

        var yamlFolder = Path.Combine(
            AppContext.BaseDirectory,
            "TestConfigs",
            $"v{version}");

        var yamlFiles = Directory
            .EnumerateFiles(yamlFolder, "*.yaml", SearchOption.AllDirectories)
            .ToList();

        Assert.NotEmpty(yamlFiles);

        var failures = new List<string>();

        // Act
        foreach (var file in yamlFiles) {
            var yaml = File.ReadAllText(file);
            JsonElement jsonNode = ConvertYamlToJsonElement(yaml);

            var options = new EvaluationOptions {
                OutputFormat = OutputFormat.Hierarchical
            };

            EvaluationResults result = schema.Evaluate(jsonNode, options);

            if (!result.IsValid) {
                var errors = new List<string>();

                void CollectErrors(EvaluationResults node) {
                    if (node.Errors != null)
                        foreach (KeyValuePair<string, string> error in node.Errors)
                            errors.Add($"{error.Key}: {error.Value}");

                    if (node.Details != null)
                        foreach (EvaluationResults child in node.Details)
                            CollectErrors(child);
                }

                CollectErrors(result);

                failures.Add(
                    $"File: {file}{Environment.NewLine}" +
                    string.Join(Environment.NewLine, errors));
            }
        }

        // Assert
        if (failures.Any()) {
            var message = string.Join(
                $"{Environment.NewLine}--------------------{Environment.NewLine}",
                failures);

            Assert.Fail(message);
        }
    }
}
