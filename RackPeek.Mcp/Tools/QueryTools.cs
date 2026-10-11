using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Connections;
using RackPeek.Domain.Resources.Hardware;
using RackPeek.Domain.Resources.Services.UseCases;
using RackPeek.Domain.Resources.SystemResources.UseCases;
using RackPeek.Domain.Search;

namespace RackPeek.Mcp.Tools;

public sealed record ResourceList(int Count, List<ResourceRow> Resources);

public sealed record SearchResults(IReadOnlyList<SearchResult> Matches);

public sealed record TreeResult(List<HardwareTree> Hardware);

public sealed record ConnectionList(int Count, IReadOnlyList<Connection> Connections);

public sealed record ResourceRow(
    string Name,
    string Kind,
    string? Ip,
    List<string> RunsOn,
    string[] Tags,
    Dictionary<string, string> Labels);

public sealed record ResourceDetail(
    [property: Description("The resource and its connections as a RackPeek YAML document. " +
                           "Edit it and pass it back to upsert_resources to change the resource.")]
    string Yaml,
    [property: Description("Names of resources that run on this one.")]
    List<string> Dependants);

public sealed record InfrastructureSummary(
    HardwareSummary Hardware,
    SystemSummary Systems,
    AllServicesSummary Services,
    [property: Description("Every tag in use and how many resources carry it.")]
    Dictionary<string, int> Tags,
    [property: Description("Every label key in use and how many resources carry it.")]
    Dictionary<string, int> Labels);

public sealed record SchemaInfo(
    [property: Description("The current config schema version.")]
    int Version,
    [property: Description("The JSON schema every inventory YAML document must conform to.")]
    string JsonSchema,
    string Guidance);

[McpServerToolType]
public sealed class QueryTools(IResourceCollection repo, IServiceProvider services) {
    internal const string UpsertGuidance =
        "A RackPeek document is YAML with 'version', 'resources' and optional 'connections'. " +
        "Every resource has 'kind' (Server, Switch, Firewall, Router, Accesspoint, Desktop, " +
        "Laptop, Ups, Other, System, Service), a unique 'name' (max 50 chars), and optional " +
        "'tags', 'labels', 'notes' and 'runsOn'. Containment rules: a Service runs on a " +
        "System; a System runs on hardware or on another System. Pass documents to " +
        "upsert_resources — merge mode only adds and updates, it never removes fields, " +
        "so use the dedicated tools (delete_resource, edit_tags, edit_labels, " +
        "remove_connection) to take things away.";

    [McpServerTool(Name = "list_resources", UseStructuredContent = true, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists inventory resources with their key facts. All filters are optional and combine.")]
    public Task<ResourceList> ListResources(
        [Description("Only this kind: Server, Switch, Firewall, Router, Accesspoint, Desktop, Laptop, Ups, Other, System or Service.")]
        string? kind = null,
        [Description("Only resources carrying this tag.")]
        string? tag = null,
        [Description("Only resources carrying this label key.")]
        string? labelKey = null) {
        return ToolErrors.RunAsync(async () => {
            IReadOnlyList<Resource> all = await repo.GetAllOfTypeAsync<Resource>();
            IReadOnlyList<(Resource, string)> ips = await repo.GetResourceIpsAsync();

            var ipByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach ((Resource resource, var ip) in ips) ipByName[resource.Name] = ip;

            var rows = all
                .Where(r => kind is null || r.Kind.Equals(kind.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(r => tag is null || r.Tags.Contains(tag.Trim(), StringComparer.OrdinalIgnoreCase))
                .Where(r => labelKey is null || r.Labels.Keys.Contains(labelKey.Trim(), StringComparer.OrdinalIgnoreCase))
                .OrderBy(r => r.Kind, StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .Select(r => new ResourceRow(
                    r.Name,
                    r.Kind,
                    ipByName.GetValueOrDefault(r.Name),
                    r.RunsOn,
                    r.Tags,
                    r.Labels))
                .ToList();

            return new ResourceList(rows.Count, rows);
        });
    }

    [McpServerTool(Name = "get_resource", UseStructuredContent = true, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Gets one resource in full, as a YAML document that can be edited and passed back to upsert_resources.")]
    public Task<ResourceDetail> GetResource(
        [Description("The resource's name.")] string name) {
        return ToolErrors.RunAsync(async () => {
            Resource resource = await repo.GetByNameAsync(name)
                                ?? throw new NotFoundException($"Resource '{name}' not found.");

            IReadOnlyList<Connection> connections = await repo.GetConnectionsForResourceAsync(resource.Name);
            IReadOnlyList<Resource> dependants = await repo.GetDependantsAsync(resource.Name);

            var yaml = YamlResourceCollection.SerializeRootAsync(new YamlRoot {
                Version = RackPeekConfigMigrationDeserializer.ListOfMigrations.Count,
                Resources = [resource],
                Connections = connections.ToList()
            });

            return new ResourceDetail(yaml, dependants.Select(d => d.Name).ToList());
        });
    }

    [McpServerTool(Name = "search_resources", UseStructuredContent = true, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Free-text search over resource names, IPs, tags and labels; returns the best matches first.")]
    public Task<SearchResults> SearchResources(
        [Description("The search text.")] string query,
        [Description("Maximum number of matches to return.")] int max = 8) {
        return ToolErrors.RunAsync(async () => {
            IReadOnlyList<Resource> all = await repo.GetAllOfTypeAsync<Resource>();
            return new SearchResults(GlobalSearchService.Search(all, query, max));
        });
    }

    [McpServerTool(Name = "get_summary", UseStructuredContent = true, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Counts of everything in the inventory: hardware by kind, systems by type and OS, services, tags and labels.")]
    public Task<InfrastructureSummary> GetSummary() {
        return ToolErrors.RunAsync(async () => {
            HardwareSummary hardware = await services.GetRequiredService<GetHardwareUseCaseSummary>().ExecuteAsync();
            SystemSummary systems = await services.GetRequiredService<GetSystemSummaryUseCase>().ExecuteAsync();
            AllServicesSummary allServices = await services.GetRequiredService<GetServiceSummaryUseCase>().ExecuteAsync();
            Dictionary<string, int> tags = await repo.GetTagsAsync();
            Dictionary<string, int> labels = await repo.GetLabelsAsync();

            return new InfrastructureSummary(hardware, systems, allServices, tags, labels);
        });
    }

    [McpServerTool(Name = "get_tree", UseStructuredContent = true, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("The containment forest: each hardware resource, the systems running on it, and the services running on those.")]
    public Task<TreeResult> GetTree(
        [Description("Only the tree under this hardware resource. Omit for the whole forest.")]
        string? hardwareName = null) {
        return ToolErrors.RunAsync(async () => {
            List<HardwareTree> forest = await services.GetRequiredService<IHardwareRepository>().GetTreeAsync();

            if (hardwareName is null) return new TreeResult(forest);

            var match = forest
                .Where(t => t.HardwareName.Equals(hardwareName.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (match.Count == 0)
                throw new NotFoundException($"Hardware '{hardwareName}' not found.");

            return new TreeResult(match);
        });
    }

    [McpServerTool(Name = "list_connections", UseStructuredContent = true, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Physical port-to-port connections between hardware resources.")]
    public Task<ConnectionList> ListConnections(
        [Description("Only connections touching this resource. Omit for all connections.")]
        string? resource = null) {
        return ToolErrors.RunAsync(async () => {
            IReadOnlyList<Connection> connections = resource is null
                ? await repo.GetConnectionsAsync()
                : await repo.GetConnectionsForResourceAsync(resource);

            return new ConnectionList(connections.Count, connections);
        });
    }

    [McpServerTool(Name = "get_subnets", UseStructuredContent = true, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Groups service IPs into subnets, or lists the services inside one CIDR block.")]
    public Task<ServiceSubnetsResult> GetSubnets(
        [Description("A CIDR block like 192.168.1.0/24 to list the services inside it. Omit to group all service IPs into subnets instead.")]
        string? cidr = null,
        [Description("Prefix length used to group when no CIDR is given (default 24).")]
        int? prefix = null,
        CancellationToken cancellationToken = default) {
        return ToolErrors.RunAsync(async () => {
            ServiceSubnetsResult result = await services.GetRequiredService<ServiceSubnetsUseCase>()
                .ExecuteAsync(cidr, prefix, cancellationToken);

            if (result.IsInvalidCidr)
                throw new McpException($"'{result.InvalidCidrValue}' is not a valid CIDR block. Use e.g. 192.168.1.0/24.");

            return result;
        });
    }

    [McpServerTool(Name = "get_schema", UseStructuredContent = true, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("The JSON schema and authoring rules for RackPeek inventory YAML — read this before writing documents for upsert_resources.")]
    public Task<SchemaInfo> GetSchema() {
        return ToolErrors.RunAsync(() => {
            var version = RackPeekConfigMigrationDeserializer.ListOfMigrations.Count;
            var resourceName = $"schema.v{version}.json";

            using Stream? stream = typeof(QueryTools).Assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                throw new InvalidOperationException($"Embedded schema '{resourceName}' is missing from the build.");

            using var reader = new StreamReader(stream);
            var schema = reader.ReadToEnd();

            return Task.FromResult(new SchemaInfo(version, schema, UpsertGuidance));
        });
    }
}
