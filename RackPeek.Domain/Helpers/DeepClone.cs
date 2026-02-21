using System.Text.Json;

namespace RackPeek.Domain.Helpers;

public static class Clone
{
    public static T DeepClone<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<T>(json)!;
    }
}