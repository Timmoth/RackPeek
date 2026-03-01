using RackPeek.Domain.Persistence.Yaml;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.Resources.UpsUnits;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RackPeek.Domain.Templates;

/// <summary>
/// Reads hardware templates from YAML files stored in a local directory tree.
/// Expected layout: <c>{basePath}/{kind}/*.yaml</c> where kind is the plural
/// lowercase form (e.g. <c>switches/</c>, <c>routers/</c>).
/// Templates are cached in memory after the first load.
/// </summary>
public sealed class BundledHardwareTemplateStore : IHardwareTemplateStore
{
    private readonly string _basePath;
    private readonly IDeserializer _deserializer;
    private List<HardwareTemplate>? _cache;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private static readonly Dictionary<string, string> PluralToKind = new(StringComparer.OrdinalIgnoreCase)
    {
        ["servers"] = "Server",
        ["switches"] = "Switch",
        ["firewalls"] = "Firewall",
        ["routers"] = "Router",
        ["accesspoints"] = "AccessPoint",
        ["ups"] = "Ups",
    };

    public BundledHardwareTemplateStore(string basePath)
    {
        _basePath = basePath;
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
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
                    { AccessPoint.KindLabel, typeof(AccessPoint) },
                    { Ups.KindLabel, typeof(Ups) },
                });
            })
            .Build();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HardwareTemplate>> GetAllByKindAsync(string kind)
    {
        var all = await LoadAsync();
        return all
            .Where(t => t.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Model, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<HardwareTemplate?> GetByIdAsync(string templateId)
    {
        var all = await LoadAsync();
        return all.FirstOrDefault(t => t.Id.Equals(templateId, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HardwareTemplate>> GetAllAsync()
    {
        var all = await LoadAsync();
        return all
            .OrderBy(t => t.Kind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.Model, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<List<HardwareTemplate>> LoadAsync()
    {
        if (_cache is not null)
            return _cache;

        await _loadLock.WaitAsync();
        try
        {
            if (_cache is not null)
                return _cache;

            _cache = await ScanTemplatesAsync();
            return _cache;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private Task<List<HardwareTemplate>> ScanTemplatesAsync()
    {
        var templates = new List<HardwareTemplate>();

        if (!Directory.Exists(_basePath))
            return Task.FromResult(templates);

        foreach (var kindDir in Directory.GetDirectories(_basePath))
        {
            var dirName = Path.GetFileName(kindDir);
            if (!PluralToKind.TryGetValue(dirName, out var kind))
                continue;

            foreach (var file in Directory.GetFiles(kindDir, "*.yaml"))
            {
                try
                {
                    var yaml = File.ReadAllText(file);
                    var resource = _deserializer.Deserialize<Resource>(yaml);
                    if (resource is null)
                        continue;

                    resource.Kind = kind;

                    var model = GetModel(resource) ?? Path.GetFileNameWithoutExtension(file);
                    var id = $"{kind}/{model}";

                    if (string.IsNullOrWhiteSpace(resource.Name))
                        resource.Name = model;

                    templates.Add(new HardwareTemplate(id, kind, model, resource));
                }
                catch
                {
                    // Skip malformed template files gracefully
                }
            }
        }

        return Task.FromResult(templates);
    }

    private static string? GetModel(Resource resource) => resource switch
    {
        Switch s => s.Model,
        Router r => r.Model,
        Firewall f => f.Model,
        AccessPoint ap => ap.Model,
        Ups u => u.Model,
        _ => null,
    };
}
