using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using RackPeek.Domain.Discovery;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Connections;
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

            // The JSON "kind" is consumed as the polymorphism discriminator, so the
            // Kind property stays empty. Stamp it from the resolved type — the same
            // label the discriminator carried — so diff snapshots and the re-serialized
            // YAML match what a YAML upload would produce.
            foreach (Resource resource in incomingRoot.Resources ?? [])
                resource.Kind = InventoryDocumentJson.KindLabel(resource);

            yamlInput = YamlResourceCollection.SerializeRootAsync(incomingRoot);
        }

        if (incomingRoot.Resources == null)
            throw new ValidationException("Missing 'resources' section.");

        // 2️Compute Diff
        List<Resource>? incomingResources = incomingRoot.Resources;
        IReadOnlyList<Resource> storedResources = await repo.GetAllOfTypeAsync<Resource>();
        List<Resource> currentResources = ResourceCollectionMerger.DeepCloneResources(storedResources);

        IReadOnlyList<Connection> storedConnections = await repo.GetConnectionsAsync();
        var currentConnections = storedConnections.Select(CloneConnection).ToList();

        ISerializer serializerDiff = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(
                DefaultValuesHandling.OmitNull |
                DefaultValuesHandling.OmitEmptyCollections)
            .Build();

        var oldSnapshots = storedResources
            .ToDictionary(
                r => r.Name,
                r => serializerDiff.Serialize(r),
                StringComparer.OrdinalIgnoreCase);

        // Line discovered resources up with what they already map to before anything
        // else looks at names, so the diff below reports against the right resources.
        // Reconcile a snapshot so validation and dry-run previews cannot mutate the
        // repository's live objects. The real merge repeats this under its file lock.
        Dictionary<string, string> storedRenames = DiscoveryIdResolver.ResolveNames(
            currentResources,
            incomingResources,
            incomingRoot.Connections,
            currentConnections,
            true);

        IGrouping<string, Resource>? duplicate = incomingResources
            .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
            throw new ValidationException($"Duplicate resource name: {duplicate.Key}");

        var currentDict = currentResources
            .ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        List<Resource> mergedResources = ResourceCollectionMerger.Merge(
            currentResources,
            incomingResources,
            request.Mode);

        var mergedDict = mergedResources
            .ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        var response = new ImportYamlResponse();
        response.Renamed = storedRenames;

        foreach ((var oldName, var newName) in storedRenames) {
            if (!oldSnapshots.TryGetValue(oldName, out var oldYaml)
                || !mergedDict.TryGetValue(newName, out Resource? renamed))
                continue;

            response.OldYaml[newName] = oldYaml;
            response.NewYaml[newName] = serializerDiff.Serialize(renamed);
            if (request.Mode != MergeMode.Replace
                || !incomingResources.Any(r => r.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                response.Updated.Add(newName);
        }

        foreach (Resource incoming in incomingResources) {
            if (!mergedDict.TryGetValue(incoming.Name, out Resource? merged))
                continue;

            var newYaml = serializerDiff.Serialize(merged);
            response.NewYaml[incoming.Name] = newYaml;

            if (!currentDict.ContainsKey(incoming.Name)) {
                response.Added.Add(incoming.Name);
                continue;
            }

            var oldName = incoming.Name;
            foreach ((var storedName, var renamedName) in storedRenames) {
                if (renamedName.Equals(incoming.Name, StringComparison.OrdinalIgnoreCase)) {
                    oldName = storedName;
                    break;
                }
            }

            if (!oldSnapshots.TryGetValue(oldName, out var oldYaml))
                continue;

            response.OldYaml[incoming.Name] = oldYaml;

            Resource existing = currentDict[incoming.Name];

            if (request.Mode == MergeMode.Replace ||
                existing.GetType() != incoming.GetType())
                response.Replaced.Add(incoming.Name);
            else if (oldYaml != newYaml && !response.Updated.Contains(incoming.Name, StringComparer.OrdinalIgnoreCase))
                response.Updated.Add(incoming.Name);
        }

        List<Connection>? mergedConnections = ConnectionMerger.Merge(
            currentConnections,
            incomingRoot.Connections,
            request.Mode);

        IReadOnlyList<Connection> finalConnections = mergedConnections ?? currentConnections;
        response.ConnectionsAdded = finalConnections
            .Select(ConnectionMerger.Describe)
            .Except(storedConnections.Select(ConnectionMerger.Describe))
            .ToList();

        response.ConnectionsRemoved = storedConnections
            .Select(ConnectionMerger.Describe)
            .Except(finalConnections.Select(ConnectionMerger.Describe))
            .ToList();

        if (!request.DryRun) await repo.Merge(yamlInput, request.Mode);

        return response;
    }

    private static Connection CloneConnection(Connection connection) => new() {
        A = new PortReference {
            Resource = connection.A.Resource,
            PortGroup = connection.A.PortGroup,
            PortIndex = connection.A.PortIndex
        },
        B = new PortReference {
            Resource = connection.B.Resource,
            PortGroup = connection.B.PortGroup,
            PortIndex = connection.B.PortIndex
        },
        Label = connection.Label,
        Notes = connection.Notes
    };
}
