using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Tests.Mcp;

internal static class McpTestExtensions {
    /// <summary>Mirrors the server's tool serialization: web defaults plus enums as strings.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Calls a tool that must succeed and deserializes its structured content.</summary>
    public static async Task<T> CallOkAsync<T>(
        this McpClient client,
        string tool,
        Dictionary<string, object?>? args = null) {
        CallToolResult result = await client.CallToolAsync(tool, args);

        AssertOk(tool, result);
        Assert.NotNull(result.StructuredContent);

        return result.StructuredContent.Value.Deserialize<T>(Json)
               ?? throw new InvalidOperationException($"'{tool}' returned unusable structured content.");
    }

    /// <summary>Calls a tool that must succeed and returns its text content.</summary>
    public static async Task<string> CallTextAsync(
        this McpClient client,
        string tool,
        Dictionary<string, object?>? args = null) {
        CallToolResult result = await client.CallToolAsync(tool, args);

        AssertOk(tool, result);

        return Text(result);
    }

    /// <summary>Calls a tool that must fail and returns the error text the agent would see.</summary>
    public static async Task<string> CallErrorAsync(
        this McpClient client,
        string tool,
        Dictionary<string, object?>? args = null) {
        CallToolResult result = await client.CallToolAsync(tool, args);

        Assert.True(result.IsError == true, $"Expected '{tool}' to fail, but it succeeded: {Text(result)}");

        return Text(result);
    }

    public static string Text(CallToolResult result) =>
        string.Join(Environment.NewLine, result.Content.OfType<TextContentBlock>().Select(t => t.Text));

    private static void AssertOk(string tool, CallToolResult result) =>
        Assert.False(result.IsError == true, $"'{tool}' failed: {Text(result)}");
}
