namespace RackPeek.Domain.Resources.Connections;

public class Connection
{
    public PortReference A { get; set; } = null!;

    public PortReference B { get; set; } = null!;

    public string? Label { get; set; }

    public string? Notes { get; set; }
}

public class PortReference
{
    public string Resource { get; set; } = null!;
    public int PortGroup { get; set; }
    public int PortIndex { get; set; }
}
