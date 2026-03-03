using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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

namespace RackPeek.Domain.Persistence;

public enum MergeMode {
    Replace,
    Merge
}

public static class ResourceCollectionMerger {
    private static readonly JsonSerializerOptions _cloneJsonOptions = new() {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        TypeInfoResolver = ResourcePolymorphismResolver.Create()
    };

    public static List<Resource> Merge(
        IEnumerable<Resource> original,
        IEnumerable<Resource> incoming,
        MergeMode mode) {
        List<Resource> originalClone = DeepCloneList(original);
        List<Resource> incomingClone = DeepCloneList(incoming);

        var result = originalClone.ToDictionary(r => r.Name, r => r, StringComparer.OrdinalIgnoreCase);

        foreach (Resource newResource in incomingClone) {
            if (!result.TryGetValue(newResource.Name, out Resource? existing)) {
                result[newResource.Name] = newResource;
                continue;
            }

            if (mode == MergeMode.Replace ||
                existing.GetType() != newResource.GetType()) {
                result[newResource.Name] = newResource;
                continue;
            }

            DeepMerge(existing, newResource, mode);
        }

        return result.Values.ToList();
    }

    private static List<Resource> DeepCloneList(IEnumerable<Resource> resources) {
        var json = JsonSerializer.Serialize(resources, _cloneJsonOptions);
        return JsonSerializer.Deserialize<List<Resource>>(json, _cloneJsonOptions) ?? new List<Resource>();
    }

    private static void DeepMerge(object target, object source, MergeMode mode) {
        Type type = target.GetType();

        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (!prop.CanRead || !prop.CanWrite)
                continue;

            var sourceValue = prop.GetValue(source);
            if (sourceValue == null)
                continue;

            var targetValue = prop.GetValue(target);
            Type propType = prop.PropertyType;

            // Simple types → overwrite
            if (IsSimple(propType)) {
                prop.SetValue(target, sourceValue);
                continue;
            }

            // Dictionary
            if (IsDictionary(propType)) {
                if (mode == MergeMode.Merge && IsDictionaryEmpty(sourceValue))
                    continue;

                MergeDictionaries(targetValue, sourceValue);
                continue;
            }

            // List / collection
            if (IsEnumerable(propType)) {
                if (mode == MergeMode.Merge && IsEnumerableEmpty(sourceValue))
                    continue;

                prop.SetValue(target, sourceValue);
                continue;
            }

            // Complex object → recursive merge
            if (targetValue == null)
                prop.SetValue(target, sourceValue);
            else
                DeepMerge(targetValue, sourceValue, mode);
        }
    }

    private static bool IsSimple(Type type) {
        return type.IsPrimitive
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(Guid)
               || Nullable.GetUnderlyingType(type)?.IsPrimitive == true;
    }

    private static bool IsDictionary(Type type) {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
    }

    private static bool IsEnumerable(Type type) {
        return typeof(IEnumerable).IsAssignableFrom(type)
               && type != typeof(string)
               && !IsDictionary(type);
    }

    private static bool IsEnumerableEmpty(object value) {
        var enumerable = (IEnumerable)value;
        return !enumerable.GetEnumerator().MoveNext();
    }

    private static bool IsDictionaryEmpty(object value) {
        var dict = (IDictionary)value;
        return dict.Count == 0;
    }

    private static void MergeDictionaries(object? target, object source) {
        if (target == null) return;

        var targetDict = (IDictionary)target;
        var sourceDict = (IDictionary)source;

        foreach (var key in sourceDict.Keys) targetDict[key] = sourceDict[key];
    }
}

internal static class ResourcePolymorphismResolver {
    public static IJsonTypeInfoResolver Create() {
        var resolver = new DefaultJsonTypeInfoResolver();

        resolver.Modifiers.Add(typeInfo => {
            if (typeInfo.Type == typeof(Resource)) {
                typeInfo.PolymorphismOptions = new JsonPolymorphismOptions {
                    TypeDiscriminatorPropertyName = "kind",
                    IgnoreUnrecognizedTypeDiscriminators = false,
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization
                };

                typeInfo.PolymorphismOptions.DerivedTypes.Add(
                    new JsonDerivedType(typeof(Server), Server.KindLabel));

                typeInfo.PolymorphismOptions.DerivedTypes.Add(
                    new JsonDerivedType(typeof(Switch), Switch.KindLabel));

                typeInfo.PolymorphismOptions.DerivedTypes.Add(
                    new JsonDerivedType(typeof(Firewall), Firewall.KindLabel));

                typeInfo.PolymorphismOptions.DerivedTypes.Add(
                    new JsonDerivedType(typeof(Router), Router.KindLabel));

                typeInfo.PolymorphismOptions.DerivedTypes.Add(
                    new JsonDerivedType(typeof(Desktop), Desktop.KindLabel));

                typeInfo.PolymorphismOptions.DerivedTypes.Add(
                    new JsonDerivedType(typeof(Laptop), Laptop.KindLabel));

                typeInfo.PolymorphismOptions.DerivedTypes.Add(
                    new JsonDerivedType(typeof(AccessPoint), AccessPoint.KindLabel));

                typeInfo.PolymorphismOptions.DerivedTypes.Add(
                    new JsonDerivedType(typeof(Ups), Ups.KindLabel));

                typeInfo.PolymorphismOptions.DerivedTypes.Add(
                    new JsonDerivedType(typeof(SystemResource), SystemResource.KindLabel));

                typeInfo.PolymorphismOptions.DerivedTypes.Add(
                    new JsonDerivedType(typeof(Service), Service.KindLabel));
            }
        });

        return resolver;
    }
}
