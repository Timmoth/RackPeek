namespace RackPeek.Domain.Api;

public class InventoryRequest
{
    public required string Hostname { get; set; }

    public string HardwareType { get; set; } = "server";

    // Hardware fields
    public double? RamGb { get; set; }
    public int? RamMts { get; set; }
    public bool? Ipmi { get; set; }
    public string? Model { get; set; }

    public List<InventoryCpu>? Cpus { get; set; }
    public List<InventoryDrive>? Drives { get; set; }
    public List<InventoryGpu>? Gpus { get; set; }
    public List<InventoryNic>? Nics { get; set; }

    // System fields (optional - if set, a SystemResource is also upserted)
    public string? SystemName { get; set; }
    public string? SystemType { get; set; }
    public string? Os { get; set; }
    public int? Cores { get; set; }
    public double? SystemRam { get; set; }
    public List<InventoryDrive>? SystemDrives { get; set; }

    // Metadata
    public string[]? Tags { get; set; }
    public Dictionary<string, string>? Labels { get; set; }
    public string? Notes { get; set; }
}

public class InventoryCpu
{
    public string? Model { get; set; }
    public int? Cores { get; set; }
    public int? Threads { get; set; }
}

public class InventoryDrive
{
    public string? Type { get; set; }
    public int? Size { get; set; }
}

public class InventoryGpu
{
    public string? Model { get; set; }
    public int? Vram { get; set; }
}

public class InventoryNic
{
    public string? Type { get; set; }
    public double? Speed { get; set; }
    public int? Ports { get; set; }
}
