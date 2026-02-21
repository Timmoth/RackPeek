using Microsoft.JSInterop;
using RackPeek.Domain.Persistence.Yaml;

namespace RackPeek.Web.Viewer;

public sealed class WasmTextFileStore(
    IJSRuntime js,
    HttpClient http) : ITextFileStore
{
    private const string Prefix = "rackpeek:file:";

    public async Task<bool> ExistsAsync(string path)
    {
        var value = await js.InvokeAsync<string?>(
            "rackpeekStorage.get",
            Key(path));

        return !string.IsNullOrWhiteSpace(value);
    }


    public async Task<string> ReadAllTextAsync(string path)
    {
        Console.WriteLine($"WASM store read: {path}");

        var value = await js.InvokeAsync<string?>(
            "rackpeekStorage.get",
            Key(path));

        if (!string.IsNullOrWhiteSpace(value))
        {
            Console.WriteLine("Loaded from browser storage");
            return value;
        }

        Console.WriteLine("Storage empty — loading from wwwroot via HTTP");

        try
        {
            var result = await http.GetStringAsync(path);
            Console.WriteLine($"Loaded {result.Length} chars from HTTP");

            // optional auto-persist bootstrap
            if (!string.IsNullOrWhiteSpace(result))
                await WriteAllTextAsync(path, result);

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine("HTTP load failed: " + ex.Message);
            throw; // TEMP — don't swallow during debug
        }
    }


    public async Task WriteAllTextAsync(string path, string contents)
    {
        await js.InvokeVoidAsync(
            "rackpeekStorage.set",
            Key(path),
            contents);
    }

    private static string Key(string path)
    {
        return Prefix + path;
    }
}