using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Persistence.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RackPeek.Domain.Api;

public class UpsertInventoryUseCase(
    IResourceCollection repo,
    IResourceYamlMigrationService migrationService)
    : IUseCase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        TypeInfoResolver = ResourcePolymorphismResolver.Create()
    };

    public async Task<ImportYamlResponse> ExecuteAsync(ImportYamlRequest request)
    {
        if (request == null)
            throw new ValidationException("Invalid request.");

        if (string.IsNullOrWhiteSpace(request.Yaml) && request.Json == null)
            throw new ValidationException("Either 'yaml' or 'json' must be provided.");
        
        if (!string.IsNullOrWhiteSpace(request.Yaml) && request.Json != null)
            throw new ValidationException("Provide either 'yaml' or 'json', not both.");
        
        
        YamlRoot incomingRoot;
        string yamlInput;

        if (!string.IsNullOrWhiteSpace(request.Yaml))
        {
            yamlInput = request.Yaml!;
            incomingRoot = await migrationService.DeserializeAsync(yamlInput)
                           ?? throw new ValidationException("Invalid YAML structure.");
        }
        else
        {
            if (request.Json is not JsonElement element)
                throw new ValidationException("Invalid JSON payload.");
            
            var rawJson = element.GetRawText();
            incomingRoot = JsonSerializer.Deserialize<YamlRoot>(
                               rawJson,
                               JsonOptions)
                           ?? throw new ValidationException("Invalid JSON structure.");
            // Generate YAML only for persistence layer
            var yamlSerializer = new SerializerBuilder()
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

        var incomingResources = incomingRoot.Resources;
        var currentResources = await repo.GetAllOfTypeAsync<Resource>();

        var duplicate = incomingResources
            .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
            throw new ValidationException($"Duplicate resource name: {duplicate.Key}");
        
        var currentDict = currentResources
            .ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        var serializerDiff = new SerializerBuilder()
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

        var mergedResources = ResourceCollectionMerger.Merge(
            currentResources,
            incomingResources,
            request.Mode);

        var mergedDict = mergedResources
            .ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        var response = new ImportYamlResponse();

        foreach (var incoming in incomingResources)
        {
            if (!mergedDict.TryGetValue(incoming.Name, out var merged))
                continue;

            var newYaml = serializerDiff.Serialize(merged);
            response.NewYaml[incoming.Name] = newYaml;

            if (!currentDict.ContainsKey(incoming.Name))
            {
                response.Added.Add(incoming.Name);
                continue;
            }

            var oldYaml = oldSnapshots[incoming.Name];
            response.OldYaml[incoming.Name] = oldYaml;

            var existing = currentDict[incoming.Name];

            if (request.Mode == MergeMode.Replace ||
                existing.GetType() != incoming.GetType())
            {
                response.Replaced.Add(incoming.Name);
            }
            else if (oldYaml != newYaml)
            {
                response.Updated.Add(incoming.Name);
            }
        }

        if (!request.DryRun)
        {
            await repo.Merge(yamlInput, request.Mode);
        }

        return response;
    }
}