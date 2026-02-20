namespace RackPeek.Domain.Resources.SubResources;

public class Drive
{
    public static readonly string[] ValidDriveTypes =
    {
        // Flash storage
        "nvme", "ssd",
        // Traditional spinning disks
        "hdd",
        // Enterprise interfaces
        "sas", "sata",
        // External / removable
        "usb", "sdcard", "micro-sd"
    };

    public string? Type { get; set; }
    public int? Size { get; set; }
}