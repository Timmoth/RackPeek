using DocMigrator.Yaml;
using Microsoft.Extensions.Logging;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.Services;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.Resources.SystemResources;
using RackPeek.Domain.Resources.OtherHardware;
using RackPeek.Domain.Resources.UpsUnits;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RackPeek.Domain.Persistence.Yaml;

public class RackPeekConfigMigrationDeserializer : YamlMigrationDeserializer<YamlRoot> {
    // List migration functions here
    public static readonly IReadOnlyList<Func<IServiceProvider, Dictionary<object, object>, ValueTask>>
        ListOfMigrations = new List<Func<IServiceProvider, Dictionary<object, object>, ValueTask>>
        {
            EnsureSchemaVersionExists,
            ConvertScalarRunsOnToList,
            ConvertNicsToPortsV3,
            AllowDiscoveryIdsV4,
            AddServiceEndpointsV5
        };

    public RackPeekConfigMigrationDeserializer(IServiceProvider serviceProvider,
        ILogger<YamlMigrationDeserializer<YamlRoot>> logger) :
        base(serviceProvider, logger,
            ListOfMigrations,
            "version",
            new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithCaseInsensitivePropertyMatching()
                .WithTypeConverter(new StorageSizeYamlConverter())
                .WithTypeConverter(new NotesStringYamlConverter())
                .WithTypeDiscriminatingNodeDeserializer(options => {
                    options.AddKeyValueTypeDiscriminator<Resource>("kind", new Dictionary<string, Type>
                    {
                        { Server.KindLabel, typeof(Server) },
                        { Switch.KindLabel, typeof(Switch) },
                        { Firewall.KindLabel, typeof(Firewall) },
                        { Router.KindLabel, typeof(Router) },
                        { Desktop.KindLabel, typeof(Desktop) },
                        { Laptop.KindLabel, typeof(Laptop) },
                        { AccessPoint.KindLabel, typeof(AccessPoint) },
                        { Ups.KindLabel, typeof(Ups) },
                        { Other.KindLabel, typeof(Other) },
                        { SystemResource.KindLabel, typeof(SystemResource) },
                        { Service.KindLabel, typeof(Service) }
                    });
                }),
            new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeConverter(new StorageSizeYamlConverter())
                .WithTypeConverter(new NotesStringYamlConverter())
                .ConfigureDefaultValuesHandling(
                    DefaultValuesHandling.OmitNull |
                    DefaultValuesHandling.OmitEmptyCollections
                )) {
    }

    #region Migrations

    // Define migration functions here
    public static ValueTask EnsureSchemaVersionExists(IServiceProvider serviceProvider, Dictionary<object, object> obj) {
        if (!obj.ContainsKey("version")) obj["version"] = 0;

        return ValueTask.CompletedTask;
    }

    public static ValueTask ConvertScalarRunsOnToList(
        IServiceProvider serviceProvider,
        Dictionary<object, object> obj) {
        const string key = "runsOn";

        if (!obj.TryGetValue("resources", out var resourceListObj))
            return ValueTask.CompletedTask;

        if (resourceListObj is not List<object> resources)
            return ValueTask.CompletedTask;

        foreach (var resourceObj in resources) {
            if (resourceObj is not Dictionary<object, object> resourceDict)
                continue;

            if (!resourceDict.TryGetValue(key, out var runsOn))
                continue;

            switch (runsOn) {
                case string single:
                    resourceDict[key] = new List<string> { single };
                    break;

                case List<object> list:
                    if (list.Any(item => item is not string))
                        throw new InvalidCastException(
                            $"Cannot convert every item in '{key}' to a string for resource " +
                            $"'{resourceDict.GetValueOrDefault("name", "unknown")}'.");

                    resourceDict[key] = list.Cast<string>().ToList();
                    break;

                case List<string>:
                    // Already correct
                    break;

                default:
                    throw new InvalidCastException(
                        $"Cannot convert {runsOn.GetType()} to List<string> for resource '{resourceDict}'.");
            }
        }

        return ValueTask.CompletedTask;
    }

    public static ValueTask ConvertNicsToPortsV3(
        IServiceProvider serviceProvider,
        Dictionary<object, object> obj) {
        if (!obj.TryGetValue("resources", out var resourcesObj))
            return ValueTask.CompletedTask;

        if (resourcesObj is not List<object> resources)
            return ValueTask.CompletedTask;

        foreach (var resourceObj in resources) {
            if (resourceObj is not Dictionary<object, object> resourceDict)
                continue;

            if (!resourceDict.TryGetValue("nics", out var nicsObj))
                continue;

            if (nicsObj is not List<object> nics)
                continue;

            var ports = new List<Dictionary<object, object>>();

            foreach (var nicObj in nics) {
                if (nicObj is not Dictionary<object, object> nicDict)
                    throw new InvalidCastException(
                        $"Cannot convert a non-object NIC to a port for resource " +
                        $"'{resourceDict.GetValueOrDefault("name", "unknown")}'.");

                var unsupportedKeys = nicDict.Keys
                    .Where(key => key is not string text || text is not ("type" or "speed" or "ports"))
                    .ToList();
                if (unsupportedKeys.Count > 0)
                    throw new InvalidCastException(
                        $"Cannot migrate NIC with unsupported fields for resource " +
                        $"'{resourceDict.GetValueOrDefault("name", "unknown")}'.");

                var port = new Dictionary<object, object>();

                if (nicDict.TryGetValue("type", out var type))
                    port["type"] = type;

                if (nicDict.TryGetValue("speed", out var speed))
                    port["speed"] = speed;

                if (nicDict.TryGetValue("ports", out var portCount))
                    port["count"] = portCount;

                ports.Add(port);
            }

            resourceDict.Remove("nics");

            if (resourceDict.TryGetValue("ports", out var existingPortsObj)
                && existingPortsObj is List<object> existingPorts)
                foreach (Dictionary<object, object> p in ports)
                    existingPorts.Add(p);
            else
                resourceDict["ports"] = ports.Cast<object>().ToList();
        }

        obj["version"] = 3;

        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///     v4 adds the optional <c>discoveryId</c> field for <c>rpk discover</c>. Purely
    ///     additive, so a v3 document only needs its version stamped — but it still gets
    ///     a version of its own so that a v3-era binary refuses a discovery-written file
    ///     cleanly ("version 4 is newer than this application supports") instead of
    ///     failing schema validation on a field it has never heard of.
    /// </summary>
    public static ValueTask AllowDiscoveryIdsV4(IServiceProvider serviceProvider, Dictionary<object, object> obj) {
        obj["version"] = 4;

        return ValueTask.CompletedTask;
    }

    public static ValueTask AddServiceEndpointsV5(IServiceProvider serviceProvider, Dictionary<object, object> obj) {
        if (obj.TryGetValue("resources", out var resourcesObj) && resourcesObj is List<object> resources) {
            foreach (var resourceObj in resources) {
                if (resourceObj is not Dictionary<object, object> resource
                    || !resource.TryGetValue("kind", out var kind)
                    || !Equals(kind, Service.KindLabel)
                    || !resource.TryGetValue("network", out var network)
                    || network is not Dictionary<object, object> networkDict
                    || resource.ContainsKey("endpoints"))
                    continue;

                resource["endpoints"] = new List<object> {
                    new Dictionary<object, object>(networkDict)
                };
            }
        }

        obj["version"] = 5;

        return ValueTask.CompletedTask;
    }

    #endregion
}
