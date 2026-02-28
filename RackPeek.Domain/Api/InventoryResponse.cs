namespace RackPeek.Domain.Api;

public class InventoryResponse
{
    public ResourceResult Hardware { get; set; } = new();
    public ResourceResult? System { get; set; }
}

public class ResourceResult
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}
