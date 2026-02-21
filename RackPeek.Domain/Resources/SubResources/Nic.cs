namespace RackPeek.Domain.Resources.SubResources;

public class Nic
{
    public static readonly string[] ValidNicTypes =
    {
        // Copper Ethernet
        "rj45",

        // SFP family
        "sfp", // 1G
        "sfp+", // 10G
        "sfp28", // 25G
        "sfp56", // 50G

        // QSFP family
        "qsfp+", // 40G
        "qsfp28", // 100G
        "qsfp56", // 200G
        "qsfp-dd", // 400G (QSFP Double Density)

        // OSFP (400G+)
        "osfp",

        // Legacy / niche but still seen
        "xfp", "cx4",

        // Management / special-purpose
        "mgmt" // Dedicated management NIC (IPMI/BMC)
    };

    public string? Type { get; set; }
    public double? Speed { get; set; }
    public int? Ports { get; set; }
}