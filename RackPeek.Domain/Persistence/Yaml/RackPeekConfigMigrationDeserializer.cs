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

    public static ValueTask ConvertScalarRunsOnToList(IServiceProvider serviceProvider, Dictionary<object, object> obj)
    {
        const string key = "runsOn";
        var resourceList = obj["resources"];
        if (resourceList is List<object> resources)
        {
            foreach(var resourceObj in resources)
            {
                if (resourceObj is Dictionary<object,object> resourceDict)
                {
                    if (resourceDict.ContainsKey(key))
                    {
                        var runsOn = resourceDict[key];
                        Type t = runsOn.GetType();
                        switch (runsOn)
                        {
                            case string r:
                                resourceDict[key] = new List<string>{r};
                                break;
                            case List<string> r:
                                // Nothing to do
                                break;
                            default:
                                throw new InvalidCastException($"Cannot convert from {t} to List<string> in {resourceDict}[{key}]");
                        }
                    }
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    #endregion
}
