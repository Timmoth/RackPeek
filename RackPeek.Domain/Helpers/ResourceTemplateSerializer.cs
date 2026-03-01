using System.Collections.Specialized;
using System.Text.Json;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RackPeek.Domain.Helpers;

/// <summary>
/// Serializes a <see cref="Resource"/> to template-format YAML suitable for
/// contributing as a bundled hardware template. Instance-specific fields
/// (tags, labels, notes, runsOn) are stripped from the output.
/// </summary>
public static class ResourceTemplateSerializer
{
    private static readonly HashSet<string> ExcludedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "tags", "labels", "notes", "runsOn", "kind"
    };

    /// <summary>
    /// Produces a template-format YAML string for the given resource.
    /// The output has <c>kind</c> first, then <c>name</c>, followed by
    /// type-specific hardware properties.
    /// </summary>
    /// <param name="resource">The resource to serialize.</param>
    /// <param name="templateName">
    /// Optional official hardware name to use in the template instead of
    /// the resource's current <see cref="Resource.Name"/>.
    /// </param>
    public static string Serialize(Resource resource, string? templateName = null)
    {
        var concreteType = resource.GetType();
        var json = JsonSerializer.Serialize(resource, concreteType);
        var clone = (Resource)JsonSerializer.Deserialize(json, concreteType)!;
        clone.Tags = [];
        clone.Labels = new Dictionary<string, string>();
        clone.Notes = null;
        clone.RunsOn = [];

        var kind = Resource.GetKind(clone);

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeConverter(new NotesStringYamlConverter())
            .ConfigureDefaultValuesHandling(
                DefaultValuesHandling.OmitNull |
                DefaultValuesHandling.OmitEmptyCollections
            )
            .Build();

        var yaml = serializer.Serialize(clone);

        var props = new DeserializerBuilder()
            .Build()
            .Deserialize<Dictionary<string, object?>>(yaml);

        var map = new OrderedDictionary
        {
            ["kind"] = kind,
            ["name"] = templateName ?? clone.Name
        };

        if (props is not null)
        {
            foreach (var (key, value) in props)
            {
                if (ExcludedKeys.Contains(key) ||
                    string.Equals(key, "name", StringComparison.OrdinalIgnoreCase))
                    continue;

                map[key] = value;
            }
        }

        return serializer.Serialize(map);
    }
}
