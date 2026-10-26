using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.Resources.Connections;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.Resources.OtherHardware;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.Resources.UpsUnits;

namespace RackPeek.Domain.Api;

/// <summary>
///     Reads the inventory as a JSON document in the same shape <c>POST /api/inventory</c>
///     accepts: camelCase with a per-resource "kind" discriminator. The discriminator is
///     written from the CLR type, so what this produces can be fed straight back to the
///     import endpoint — including the AccessPoint/Accesspoint casing difference that
///     trips hand-written documents.
/// </summary>
public static class InventoryDocumentJson {
    public static readonly JsonSerializerOptions Options = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public static string KindLabel(Resource resource) => resource switch {
        Server => Server.KindLabel,
        Switch => Switch.KindLabel,
        Firewall => Firewall.KindLabel,
        Router => Router.KindLabel,
        Desktop => Desktop.KindLabel,
        Laptop => Laptop.KindLabel,
        AccessPoint => AccessPoint.KindLabel,
        Ups => Ups.KindLabel,
        Other => Other.KindLabel,
        SystemResource => SystemResource.KindLabel,
        Service => Service.KindLabel,
        _ => throw new InvalidOperationException($"Unknown resource type: {resource.GetType().Name}")
    };

    public static JsonNode ToNode(Resource resource) {
        // Serialize against the concrete type: no polymorphism resolver, so the
        // Resource.Kind property does not collide with the discriminator name.
        // The discriminator must come first — the import endpoint's reader cannot
        // buffer derived-type fields (ports, network, …) until the type is resolved.
        JsonObject body = JsonSerializer.SerializeToNode(resource, resource.GetType(), Options)!.AsObject();
        body.Remove("kind");

        var node = new JsonObject { ["kind"] = KindLabel(resource) };
        foreach (KeyValuePair<string, JsonNode?> property in body)
            node[property.Key] = property.Value?.DeepClone();

        return node;
    }

    public static async Task<JsonObject> BuildDocumentAsync(IResourceCollection repo) {
        (IReadOnlyList<Resource> resources, IReadOnlyList<Connection> connections) =
            await repo.GetInventorySnapshotAsync();

        return new JsonObject {
            ["version"] = RackPeekConfigMigrationDeserializer.ListOfMigrations.Count,
            ["resources"] = new JsonArray([.. resources.Select(ToNode)]),
            ["connections"] = new JsonArray([.. connections.Select(c => JsonSerializer.SerializeToNode(c, Options))])
        };
    }
}
