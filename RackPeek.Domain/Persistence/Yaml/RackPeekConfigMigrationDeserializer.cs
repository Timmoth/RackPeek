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
using RackPeek.Domain.Resources.UpsUnits;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RackPeek.Domain.Persistence.Yaml;

public class RackPeekConfigMigrationDeserializer : YamlMigrationDeserializer<YamlRoot>
{
    // List migration functions here
    public static readonly IReadOnlyList<Func<IServiceProvider, Dictionary<object, object>, ValueTask>> ListOfMigrations = new List<Func<IServiceProvider, Dictionary<object,object>, ValueTask>>{
        EnsureSchemaVersionExists,
        ConvertScalarRunsOnToList,
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
                .WithTypeDiscriminatingNodeDeserializer(options =>
                {
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
                )) {}

    #region Migrations

    // Define migration functions here
    public static ValueTask EnsureSchemaVersionExists(IServiceProvider serviceProvider, Dictionary<object, object> obj)
    {
        if (!obj.ContainsKey("version"))
        {
            obj["version"] = 0;
        }
        
        return ValueTask.CompletedTask;
    }
    public static ValueTask ConvertScalarRunsOnToList(
        IServiceProvider serviceProvider,
        Dictionary<object, object> obj)
    {
        const string key = "runsOn";

        if (!obj.TryGetValue("resources", out var resourceListObj))
            return ValueTask.CompletedTask;

        if (resourceListObj is not List<object> resources)
            return ValueTask.CompletedTask;

        foreach (var resourceObj in resources)
        {
            if (resourceObj is not Dictionary<object, object> resourceDict)
                continue;

            if (!resourceDict.TryGetValue(key, out var runsOn))
                continue;

            switch (runsOn)
            {
                case string single:
                    resourceDict[key] = new List<string> { single };
                    break;

                case List<object> list:
                    resourceDict[key] = list
                        .OfType<string>()
                        .ToList();
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
    #endregion
}
