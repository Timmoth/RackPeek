using System.ComponentModel.DataAnnotations;
using RackPeek.Domain.Helpers;
using RackPeek.Domain.Persistence;
using RackPeek.Domain.Resources;
using RackPeek.Domain.Resources.Desktops;
using RackPeek.Domain.Resources.Laptops;
using RackPeek.Domain.Resources.Servers;
using RackPeek.Domain.Resources.SubResources;
using RackPeek.Domain.Resources.SystemResources;

namespace RackPeek.Domain.Api;

public class UpsertInventoryUseCase(IResourceCollection repo) : IUseCase
{
    public async Task<InventoryResponse> ExecuteAsync(InventoryRequest request)
    {
        await repo.LoadAsync();

        var response = new InventoryResponse();

        var hostname = Normalize.HardwareName(request.Hostname);
        ThrowIfInvalid.ResourceName(hostname);

        // Upsert hardware resource
        var existing = await repo.GetByNameAsync(hostname);
        var hardwareAction = existing != null ? "updated" : "created";

        var hardware = BuildHardware(request, hostname);

        if (existing != null)
            await repo.UpdateAsync(hardware);
        else
            await repo.AddAsync(hardware);

        response.Hardware = new ResourceResult
        {
            Name = hostname,
            Kind = GetKind(hardware.GetType()),
            Action = hardwareAction
        };

        // Upsert system resource if system fields are provided
        if (HasSystemFields(request))
        {
            var systemName = Normalize.SystemName(request.SystemName ?? $"{hostname}-system");
            ThrowIfInvalid.ResourceName(systemName);

            var existingSystem = await repo.GetByNameAsync(systemName);
            var systemAction = existingSystem != null ? "updated" : "created";

            var system = BuildSystem(request, systemName, hostname);

            if (existingSystem != null)
                await repo.UpdateAsync(system);
            else
                await repo.AddAsync(system);

            response.System = new ResourceResult
            {
                Name = systemName,
                Kind = SystemResource.KindLabel,
                Action = systemAction
            };
        }

        return response;
    }

    private static Resource BuildHardware(InventoryRequest request, string hostname)
    {
        var type = request.HardwareType.Trim().ToLowerInvariant();

        var ram = (request.RamGb != null || request.RamMts != null)
            ? new Ram { Size = request.RamGb, Mts = request.RamMts }
            : null;

        var cpus = MapCpus(request.Cpus);
        var drives = MapDrives(request.Drives);
        var gpus = MapGpus(request.Gpus);
        var nics = MapNics(request.Nics);

        var tags = request.Tags ?? [];
        var labels = request.Labels ?? new Dictionary<string, string>();

        return type switch
        {
            "server" => new Server
            {
                Name = hostname,
                Ram = ram,
                Ipmi = request.Ipmi,
                Cpus = cpus,
                Drives = drives,
                Gpus = gpus,
                Nics = nics,
                Tags = tags,
                Labels = labels,
                Notes = request.Notes
            },
            "desktop" => new Desktop
            {
                Name = hostname,
                Ram = ram,
                Model = request.Model
                    ?? throw new ValidationException("Model is required for desktop hardware type."),
                Cpus = cpus,
                Drives = drives,
                Gpus = gpus,
                Nics = nics,
                Tags = tags,
                Labels = labels,
                Notes = request.Notes
            },
            "laptop" => new Laptop
            {
                Name = hostname,
                Ram = ram,
                Model = request.Model,
                Cpus = cpus,
                Drives = drives,
                Gpus = gpus,
                Tags = tags,
                Labels = labels,
                Notes = request.Notes
            },
            _ => throw new ValidationException(
                $"Hardware type '{request.HardwareType}' is not valid. Valid types: server, desktop, laptop.")
        };
    }

    private static SystemResource BuildSystem(InventoryRequest request, string systemName, string hostname)
    {
        if (request.SystemType != null)
        {
            var normalizedType = Normalize.SystemType(request.SystemType);
            ThrowIfInvalid.SystemType(normalizedType);
        }

        return new SystemResource
        {
            Name = systemName,
            Type = request.SystemType != null ? Normalize.SystemType(request.SystemType) : null,
            Os = request.Os,
            Cores = request.Cores,
            Ram = request.SystemRam,
            Drives = MapDrives(request.SystemDrives),
            RunsOn = [hostname],
            Tags = request.Tags ?? [],
            Labels = request.Labels ?? new Dictionary<string, string>(),
            Notes = request.Notes
        };
    }

    private static bool HasSystemFields(InventoryRequest request)
    {
        return request.Os != null
            || request.SystemType != null
            || request.Cores != null
            || request.SystemRam != null
            || request.SystemDrives is { Count: > 0 };
    }

    private static List<Cpu>? MapCpus(List<InventoryCpu>? cpus)
    {
        return cpus?.Select(c => new Cpu
        {
            Model = c.Model,
            Cores = c.Cores,
            Threads = c.Threads
        }).ToList();
    }

    private static List<Drive>? MapDrives(List<InventoryDrive>? drives)
    {
        if (drives == null) return null;

        foreach (var d in drives)
        {
            if (d.Type != null)
            {
                d.Type = Normalize.DriveType(d.Type);
                ThrowIfInvalid.DriveType(d.Type);
            }

            if (d.Size.HasValue)
                ThrowIfInvalid.DriveSize(d.Size.Value);
        }

        return drives.Select(d => new Drive
        {
            Type = d.Type,
            Size = d.Size
        }).ToList();
    }

    private static List<Gpu>? MapGpus(List<InventoryGpu>? gpus)
    {
        return gpus?.Select(g => new Gpu
        {
            Model = g.Model,
            Vram = g.Vram
        }).ToList();
    }

    private static List<Nic>? MapNics(List<InventoryNic>? nics)
    {
        if (nics == null) return null;

        foreach (var n in nics)
        {
            if (n.Type != null)
            {
                n.Type = Normalize.NicType(n.Type);
                ThrowIfInvalid.NicType(n.Type);
            }

            if (n.Speed.HasValue)
                ThrowIfInvalid.NicSpeed(n.Speed.Value);

            if (n.Ports.HasValue)
                ThrowIfInvalid.NicPorts(n.Ports.Value);
        }

        return nics.Select(n => new Nic
        {
            Type = n.Type,
            Speed = n.Speed,
            Ports = n.Ports
        }).ToList();
    }

    private static string GetKind(Type resourceType)
    {
        if (resourceType == typeof(Server)) return Server.KindLabel;
        if (resourceType == typeof(Desktop)) return Desktop.KindLabel;
        if (resourceType == typeof(Laptop)) return Laptop.KindLabel;
        if (resourceType == typeof(SystemResource)) return SystemResource.KindLabel;
        throw new InvalidOperationException($"Unknown resource type: {resourceType.Name}");
    }
}
