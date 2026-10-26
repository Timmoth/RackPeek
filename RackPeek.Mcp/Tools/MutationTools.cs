using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using RackPeek.Domain.Api;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Connections;
using RackPeek.Domain.UseCases;
using RackPeek.Domain.UseCases.Labels;
using RackPeek.Domain.UseCases.Tags;

namespace RackPeek.Mcp.Tools;

public sealed record TagsResult(string Name, string[] Tags);

public sealed record LabelsResult(string Name, Dictionary<string, string> Labels);

[McpServerToolType]
public sealed class MutationTools(IResourceCollection repo, IServiceProvider services) {
    [McpServerTool(Name = "upsert_resources", UseStructuredContent = true, Idempotent = true, OpenWorld = false)]
    [Description("Creates or updates resources (and connections) from a RackPeek YAML document — the main " +
                 "way to build and edit the inventory. Call get_schema first to learn the format, and " +
                 "preview with dryRun before writing. Merge mode only adds and updates; it never removes.")]
    public Task<ImportYamlResponse> UpsertResources(
        [Description("A RackPeek YAML document: 'version', 'resources' and optional 'connections'.")]
        string yaml,
        [Description("Merge folds each incoming resource into the stored one, field by field. " +
                     "Replace swaps each incoming resource in wholesale (other resources are untouched).")]
        MergeMode mode = MergeMode.Merge,
        [Description("When true, nothing is written — the response shows what would change.")]
        bool dryRun = false) =>
        RunUpsertAsync(services, new ImportYamlRequest { Yaml = yaml, Mode = mode, DryRun = dryRun });

    [McpServerTool(Name = "delete_resource", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Deletes a resource, detaches everything that ran on it, and removes its connections.")]
    public Task<string> DeleteResource(
        [Description("The resource's name.")] string name) {
        return ToolErrors.RunAsync(async () => {
            await services.GetRequiredService<IDeleteResourceUseCase<Resource>>().ExecuteAsync(name);
            return $"Deleted '{name}'.";
        });
    }

    [McpServerTool(Name = "rename_resource", Idempotent = true, OpenWorld = false)]
    [Description("Renames a resource and rewrites every runsOn reference and connection endpoint that pointed at it.")]
    public Task<string> RenameResource(
        [Description("The resource's current name.")] string name,
        [Description("The new name.")] string newName) {
        return ToolErrors.RunAsync(async () => {
            await services.GetRequiredService<IRenameResourceUseCase<Resource>>().ExecuteAsync(name, newName);
            return $"Renamed '{name}' to '{newName}'.";
        });
    }

    [McpServerTool(Name = "clone_resource", OpenWorld = false)]
    [Description("Copies a resource under a new name. The copy has no discovery id — it describes new gear, not the original machine.")]
    public Task<string> CloneResource(
        [Description("The resource to copy.")] string name,
        [Description("The copy's name.")] string cloneName) {
        return ToolErrors.RunAsync(async () => {
            await ResourceKindDispatch.CloneAsync(services, repo, name, cloneName);
            return $"Cloned '{name}' to '{cloneName}'.";
        });
    }

    [McpServerTool(Name = "edit_tags", UseStructuredContent = true, Idempotent = true, OpenWorld = false)]
    [Description("Adds and/or removes tags on a resource and returns the tags it ends up with.")]
    public Task<TagsResult> EditTags(
        [Description("The resource's name.")] string name,
        [Description("Tags to add.")] string[]? add = null,
        [Description("Tags to remove.")] string[]? remove = null) {
        return ToolErrors.RunAsync(async () => {
            if (add is not { Length: > 0 } && remove is not { Length: > 0 })
                throw new ValidationException("Pass at least one tag to add or remove.");

            foreach (var tag in add ?? [])
                await services.GetRequiredService<IAddTagUseCase<Resource>>().ExecuteAsync(name, tag);

            foreach (var tag in remove ?? [])
                await services.GetRequiredService<IRemoveTagUseCase<Resource>>().ExecuteAsync(name, tag);

            Resource resource = await repo.GetByNameAsync(name)
                                ?? throw new NotFoundException($"Resource '{name}' not found.");

            return new TagsResult(resource.Name, resource.Tags);
        });
    }

    [McpServerTool(Name = "edit_labels", UseStructuredContent = true, Idempotent = true, OpenWorld = false)]
    [Description("Sets and/or removes key-value labels on a resource and returns the labels it ends up with.")]
    public Task<LabelsResult> EditLabels(
        [Description("The resource's name.")] string name,
        [Description("Labels to set — an existing key is overwritten.")]
        Dictionary<string, string>? set = null,
        [Description("Label keys to remove.")] string[]? remove = null) {
        return ToolErrors.RunAsync(async () => {
            if (set is not { Count: > 0 } && remove is not { Length: > 0 })
                throw new ValidationException("Pass at least one label to set or remove.");

            foreach ((var key, var value) in set ?? new Dictionary<string, string>())
                await services.GetRequiredService<IAddLabelUseCase<Resource>>().ExecuteAsync(name, key, value);

            foreach (var key in remove ?? [])
                await services.GetRequiredService<IRemoveLabelUseCase<Resource>>().ExecuteAsync(name, key);

            Resource resource = await repo.GetByNameAsync(name)
                                ?? throw new NotFoundException($"Resource '{name}' not found.");

            return new LabelsResult(resource.Name, resource.Labels);
        });
    }

    [McpServerTool(Name = "add_connection", Idempotent = true, OpenWorld = false)]
    [Description("Connects a port on one hardware resource to a port on another. A port holds one connection — " +
                 "connecting an occupied port replaces what was plugged into it. Port groups and indexes are " +
                 "zero-based against the resource's 'ports' list (a group entry with count 4 has indexes 0-3).")]
    public Task<string> AddConnection(
        [Description("First endpoint's resource name.")] string resourceA,
        [Description("First endpoint's port group index.")] int portGroupA,
        [Description("First endpoint's port index within the group.")] int portIndexA,
        [Description("Second endpoint's resource name.")] string resourceB,
        [Description("Second endpoint's port group index.")] int portGroupB,
        [Description("Second endpoint's port index within the group.")] int portIndexB,
        [Description("Optional label, e.g. 'uplink'.")] string? label = null,
        [Description("Optional notes.")] string? notes = null) {
        return ToolErrors.RunAsync(async () => {
            var a = new PortReference { Resource = resourceA, PortGroup = portGroupA, PortIndex = portIndexA };
            var b = new PortReference { Resource = resourceB, PortGroup = portGroupB, PortIndex = portIndexB };

            await services.GetRequiredService<IAddConnectionUseCase>().ExecuteAsync(a, b, label, notes);

            return $"Connected {ConnectionMerger.Describe(new Connection { A = a, B = b, Label = label })}.";
        });
    }

    [McpServerTool(Name = "remove_connection", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Removes whatever connection is plugged into the given port.")]
    public Task<string> RemoveConnection(
        [Description("The port's resource name.")] string resource,
        [Description("The port's group index.")] int portGroup,
        [Description("The port's index within the group.")] int portIndex) {
        return ToolErrors.RunAsync(async () => {
            var port = new PortReference { Resource = resource, PortGroup = portGroup, PortIndex = portIndex };
            await services.GetRequiredService<IRemoveConnectionUseCase>().ExecuteAsync(port);
            return $"Removed any connection on {resource} port {portGroup}/{portIndex}.";
        });
    }

    /// <summary>
    ///     Upserts get their own error mapping: the use case reports YAML/JSON problems
    ///     through several exception types, and — matching the REST endpoint's contract —
    ///     the message is always safe and useful to show the caller.
    /// </summary>
    internal static async Task<ImportYamlResponse> RunUpsertAsync(
        IServiceProvider services,
        ImportYamlRequest request) {
        try {
            return await services.GetRequiredService<UpsertInventoryUseCase>().ExecuteAsync(request);
        }
        catch (ValidationException ex) {
            throw new McpException($"Invalid input: {ex.Message}");
        }
        catch (Exception ex) when (ex is not McpException) {
            throw new McpException($"Import failed: {ex.Message}");
        }
    }
}
