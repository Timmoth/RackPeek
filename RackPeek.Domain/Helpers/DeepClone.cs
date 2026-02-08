namespace RackPeek.Domain.Helpers;

using System.Text.Json;

public static class Clone
{
    public static T DeepClone<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<T>(json)!;
    }

}
