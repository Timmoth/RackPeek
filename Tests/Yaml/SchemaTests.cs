using System.Text.Json;
using Json.Schema;
using YamlDotNet.RepresentationModel;

namespace Tests.Yaml;

public class SchemaConformanceTests
{
    private static JsonSchema LoadSchema()
    {
        var schemaText = File.ReadAllText("schemas/schema.v1.json");
        return JsonSchema.FromText(schemaText);
    }
    private static JsonElement ConvertYamlToJsonElement(string yaml)
    {
        // Load YAML into YAML DOM
        var yamlStream = new YamlStream();
        yamlStream.Load(new StringReader(yaml));

        var root = yamlStream.Documents[0].RootNode;

        // Convert YAML node → JSON string
        var json = ConvertYamlNodeToJson(root);

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static string ConvertYamlNodeToJson(YamlNode node)
    {
        if (node is YamlScalarNode scalar)
        {
            // Try numeric
            if (int.TryParse(scalar.Value, out var i))
                return i.ToString();

            if (double.TryParse(scalar.Value, out var d))
                return d.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (bool.TryParse(scalar.Value, out var b))
                return b.ToString().ToLowerInvariant();

            // Otherwise string
            return JsonSerializer.Serialize(scalar.Value);
        }

        if (node is YamlSequenceNode sequence)
        {
            var items = sequence.Children
                .Select(ConvertYamlNodeToJson);

            return "[" + string.Join(",", items) + "]";
        }

        if (node is YamlMappingNode mapping)
        {
            var props = mapping.Children
                .Select(kvp =>
                    JsonSerializer.Serialize(((YamlScalarNode)kvp.Key).Value)
                    + ":"
                    + ConvertYamlNodeToJson(kvp.Value));

            return "{" + string.Join(",", props) + "}";
        }

        return "null";
    }
    [Fact]
    public void All_v1_yaml_files_conform_to_schema()
    {
        // Arrange
        var schema = LoadSchema();
        
        var yamlFolder = Path.Combine(
            AppContext.BaseDirectory,
            "TestConfigs",
            "v1");

        var yamlFiles = Directory
            .EnumerateFiles(yamlFolder, "*.yaml", SearchOption.AllDirectories)
            .ToList();

        Assert.NotEmpty(yamlFiles);

        var failures = new List<string>();

        // Act
        foreach (var file in yamlFiles)
        {
            var yaml = File.ReadAllText(file);
            var jsonNode = ConvertYamlToJsonElement(yaml);

            var options = new EvaluationOptions
            {
                OutputFormat = OutputFormat.Hierarchical            };

            var result = schema.Evaluate(jsonNode, options);
            
            if (!result.IsValid)
            {
                var errors = new List<string>();

                void CollectErrors(EvaluationResults node)
                {
                    if (node.Errors != null)
                    {
                        foreach (var error in node.Errors)
                            errors.Add($"{error.Key}: {error.Value}");
                    }

                    if (node.Details != null)
                    {
                        foreach (var child in node.Details)
                            CollectErrors(child);
                    }
                }

                CollectErrors(result);

                failures.Add(
                    $"File: {file}{Environment.NewLine}" +
                    string.Join(Environment.NewLine, errors));
            }
        }

        // Assert
        if (failures.Any())
        {
            var message = string.Join(
                $"{Environment.NewLine}--------------------{Environment.NewLine}",
                failures);

            Assert.Fail(message);
        }
    }
}