namespace RackPeek.Domain.Resources.Hardware;

/// <summary>
/// Base class for all physical hardware resources.
/// </summary>
public abstract class Hardware : Resource {
    /// <summary>
    /// The hardware model identifier (e.g. "Dell PowerEdge R730", "Arista DCS-7050TX-64-R").
    /// </summary>
    public string? Model { get; set; }
}
