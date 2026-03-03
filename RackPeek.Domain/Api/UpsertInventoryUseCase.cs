using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RackPeek.Domain.Api;

public class UpsertInventoryUseCase(
    IResourceCollection repo,
    IResourceYamlMigrationService migrationService)
    : IUseCase {
    private static readonly JsonSerializerOptions _jsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        TypeInfoResolver = ResourcePolymorphismResolver.Create()
    };

    public async Task<ImportYamlResponse> ExecuteAsync(ImportYamlRequest request) {
        if (request == null)
            throw new ValidationException("Invalid request.");

        if (string.IsNullOrWhiteSpace(request.Yaml) && request.Json == null)
            throw new ValidationException("Either 'yaml' or 'json' must be provided.");

        if (!string.IsNullOrWhiteSpace(request.Yaml) && request.Json != null)
            throw new ValidationException("Provide either 'yaml' or 'json', not both.");


        YamlRoot incomingRoot;
        string yamlInput;

        if (!string.IsNullOrWhiteSpace(request.Yaml)) {
            yamlInput = request.Yaml!;
            incomingRoot = await migrationService.DeserializeAsync(yamlInput)
                           ?? throw new ValidationException("Invalid YAML structure.");
        }
        else {
            if (request.Json is not JsonElement element)
                throw new ValidationException("Invalid JSON payload.");

            var rawJson = element.GetRawText();
            incomingRoot = JsonSerializer.Deserialize<YamlRoot>(
                               rawJson,
                               _jsonOptions)
                           ?? throw new ValidationException("Invalid JSON structure.");
            // Generate YAML only for persistence layer
            ISerializer yamlSerializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeConverter(new StorageSizeYamlConverter())
                .WithTypeConverter(new NotesStringYamlConverter())
                .ConfigureDefaultValuesHandling(
                    DefaultValuesHandling.OmitNull |
                    DefaultValuesHandling.OmitEmptyCollections)
                .Build();

            yamlInput = yamlSerializer.Serialize(incomingRoot);
        }

        if (incomingRoot.Resources == null)
            throw new ValidationException("Missing 'resources' section.");

        // 2️Compute Diff

        List<Resource>? incomingResources = incomingRoot.Resources;
        IReadOnlyList<Resource> currentResources = await repo.GetAllOfTypeAsync<Resource>();

        IGrouping<string, Resource>? duplicate = incomingResources
            .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
            throw new ValidationException($"Duplicate resource name: {duplicate.Key}");

        var currentDict = currentResources
            .ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        ISerializer serializerDiff = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(
                DefaultValuesHandling.OmitNull |
                DefaultValuesHandling.OmitEmptyCollections)
            .Build();

        var oldSnapshots = currentResources
            .ToDictionary(
                r => r.Name,
                r => serializerDiff.Serialize(r),
                StringComparer.OrdinalIgnoreCase);

        List<Resource> mergedResources = ResourceCollectionMerger.Merge(
            currentResources,
            incomingResources,
            request.Mode);

        var mergedDict = mergedResources
            .ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        var response = new ImportYamlResponse();

        foreach (Resource incoming in incomingResources) {
            if (!mergedDict.TryGetValue(incoming.Name, out Resource? merged))
                continue;

            var newYaml = serializerDiff.Serialize(merged);
            response.NewYaml[incoming.Name] = newYaml;

            if (!currentDict.ContainsKey(incoming.Name)) {
                response.Added.Add(incoming.Name);
                continue;
            }

            var oldYaml = oldSnapshots[incoming.Name];
            response.OldYaml[incoming.Name] = oldYaml;

            Resource existing = currentDict[incoming.Name];

            if (request.Mode == MergeMode.Replace ||
                existing.GetType() != incoming.GetType())
                response.Replaced.Add(incoming.Name);
            else if (oldYaml != newYaml) response.Updated.Add(incoming.Name);
        }

        if (!request.DryRun) await repo.Merge(yamlInput, request.Mode);

        return response;
    }
}
