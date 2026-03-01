using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.AccessPoints;
using RackPeek.Domain.Resources.Firewalls;
using RackPeek.Domain.Resources.Routers;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;
using RackPeek.Domain.Resources.Switches;
using RackPeek.Domain.Resources.UpsUnits;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using RackPeek.Domain.Persistence.Yaml;

namespace RackPeek.Domain.Templates;

/// <summary>
/// Validates a hardware template YAML file against the resource schema for its <c>kind</c>.
/// Returns a list of human-readable validation errors (empty list means valid).
/// </summary>
public sealed class TemplateValidator
{
    private static readonly HashSet<string> ValidKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "Server", "Switch", "Router", "Firewall", "AccessPoint", "Ups"
    };

    private static readonly HashSet<string> ValidNicTypes =
        new(Nic.ValidNicTypes, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ValidDriveTypes =
        new(Drive.ValidDriveTypes, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ValidPortTypes =
        new(Nic.ValidNicTypes, StringComparer.OrdinalIgnoreCase);

    private readonly IDeserializer _deserializer;
    private readonly IDeserializer _plainDeserializer;

    public TemplateValidator()
    {
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

        _plainDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Validates raw YAML content as a hardware template.
    /// </summary>
    /// <param name="yaml">The YAML content to validate.</param>
    /// <param name="fileName">Filename used for error messages and model fallback.</param>
    /// <returns>A list of validation errors. Empty means the template is valid.</returns>
    public IReadOnlyList<string> Validate(string yaml, string fileName)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(yaml))
        {
            errors.Add("File is empty.");
            return errors;
        }

        // Pre-parse to extract kind before the type-discriminating deserializer runs,
        // because the discriminator throws on unknown/missing kind values.
        string? kind;
        try
        {
            var raw = _plainDeserializer.Deserialize<Dictionary<string, object>>(yaml);
            if (raw is null)
            {
                errors.Add("YAML deserialized to null.");
                return errors;
            }

            kind = raw.TryGetValue("kind", out var k) ? k?.ToString() : null;
        }
        catch (Exception ex)
        {
            errors.Add($"YAML parsing failed: {ex.Message}");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            errors.Add("Missing required field: 'kind'.");
            return errors;
        }

        if (!ValidKinds.Contains(kind))
        {
            errors.Add($"Invalid kind '{kind}'. Must be one of: {string.Join(", ", ValidKinds.Order())}.");
            return errors;
        }

        Resource resource;
        try
        {
            resource = _deserializer.Deserialize<Resource>(yaml);
        }
        catch (Exception ex)
        {
            errors.Add($"YAML parsing failed: {ex.Message}");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(resource.Name))
            errors.Add("Missing required field: 'name'.");

        switch (resource)
        {
            case Server server:
                ValidateServer(server, errors);
                break;
            case Switch sw:
                ValidatePortDevice(sw.Model, sw.Ports, "Switch", errors);
                break;
            case Router rt:
                ValidatePortDevice(rt.Model, rt.Ports, "Router", errors);
                break;
            case Firewall fw:
                ValidatePortDevice(fw.Model, fw.Ports, "Firewall", errors);
                break;
            case AccessPoint ap:
                ValidateAccessPoint(ap, errors);
                break;
            case Ups ups:
                ValidateUps(ups, errors);
                break;
        }

        return errors;
    }

    private static void ValidateServer(Server server, List<string> errors)
    {
        if (server.Cpus is { Count: > 0 })
        {
            for (var i = 0; i < server.Cpus.Count; i++)
            {
                var cpu = server.Cpus[i];
                if (string.IsNullOrWhiteSpace(cpu.Model))
                    errors.Add($"cpus[{i}]: 'model' is required.");
                if (cpu.Cores is < 0)
                    errors.Add($"cpus[{i}]: 'cores' must be non-negative.");
                if (cpu.Threads is < 0)
                    errors.Add($"cpus[{i}]: 'threads' must be non-negative.");
            }
        }

        if (server.Ram is not null)
        {
            if (server.Ram.Size is < 0)
                errors.Add("ram: 'size' must be non-negative.");
            if (server.Ram.Mts is < 0)
                errors.Add("ram: 'mts' must be non-negative.");
        }

        if (server.Drives is { Count: > 0 })
        {
            for (var i = 0; i < server.Drives.Count; i++)
            {
                var drive = server.Drives[i];
                if (string.IsNullOrWhiteSpace(drive.Type))
                    errors.Add($"drives[{i}]: 'type' is required.");
                else if (!ValidDriveTypes.Contains(drive.Type))
                    errors.Add($"drives[{i}]: invalid type '{drive.Type}'. Valid: {string.Join(", ", Drive.ValidDriveTypes)}.");
                if (drive.Size is < 0)
                    errors.Add($"drives[{i}]: 'size' must be non-negative.");
            }
        }

        if (server.Nics is { Count: > 0 })
            ValidateNics(server.Nics, errors);

        if (server.Gpus is { Count: > 0 })
        {
            for (var i = 0; i < server.Gpus.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(server.Gpus[i].Model))
                    errors.Add($"gpus[{i}]: 'model' is required.");
            }
        }
    }

    private static void ValidateNics(List<Nic> nics, List<string> errors)
    {
        for (var i = 0; i < nics.Count; i++)
        {
            var nic = nics[i];
            if (string.IsNullOrWhiteSpace(nic.Type))
                errors.Add($"nics[{i}]: 'type' is required.");
            else if (!ValidNicTypes.Contains(nic.Type))
                errors.Add($"nics[{i}]: invalid type '{nic.Type}'. Valid: {string.Join(", ", Nic.ValidNicTypes)}.");
            if (nic.Speed is < 0)
                errors.Add($"nics[{i}]: 'speed' must be non-negative.");
            if (nic.Ports is < 0)
                errors.Add($"nics[{i}]: 'ports' must be non-negative.");
        }
    }

    private static void ValidatePortDevice(string? model, List<Port>? ports, string kindLabel, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(model))
            errors.Add($"'{kindLabel}' templates require the 'model' field.");

        if (ports is { Count: > 0 })
        {
            for (var i = 0; i < ports.Count; i++)
            {
                var port = ports[i];
                if (string.IsNullOrWhiteSpace(port.Type))
                    errors.Add($"ports[{i}]: 'type' is required.");
                else if (!ValidPortTypes.Contains(port.Type))
                    errors.Add($"ports[{i}]: invalid type '{port.Type}'. Valid: {string.Join(", ", Nic.ValidNicTypes)}.");
                if (port.Speed is < 0)
                    errors.Add($"ports[{i}]: 'speed' must be non-negative.");
                if (port.Count is < 0)
                    errors.Add($"ports[{i}]: 'count' must be non-negative.");
            }
        }
    }

    private static void ValidateAccessPoint(AccessPoint ap, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(ap.Model))
            errors.Add("'AccessPoint' templates require the 'model' field.");
        if (ap.Speed is < 0)
            errors.Add("'speed' must be non-negative.");
    }

    private static void ValidateUps(Ups ups, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(ups.Model))
            errors.Add("'Ups' templates require the 'model' field.");
        if (ups.Va is < 0)
            errors.Add("'va' must be non-negative.");
    }
}
